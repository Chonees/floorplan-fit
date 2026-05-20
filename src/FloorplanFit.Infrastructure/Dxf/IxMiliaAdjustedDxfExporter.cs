using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using IxMilia.Dxf;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed class IxMiliaAdjustedDxfExporter : IAdjustedDxfExporter
{
    private static readonly string[] HandleFieldNames =
    [
        "<IxMilia.Dxf.IDxfItemInternal.Handle>k__BackingField",
        "<Handle>k__BackingField"
    ];

    public Task ExportAsync(
        string sourceFilePath,
        string outputFilePath,
        IReadOnlyList<DimensionDto> dimensions,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFilePath);

        var dxf = DxfFile.Load(sourceFilePath);
        var dimensionLookup = dxf.Entities
            .OfType<DxfDimensionBase>()
            .Select(entity => (Entity: entity, Handle: GetSourceHandle(entity)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Handle))
            .ToDictionary(item => item.Handle!, item => item.Entity, StringComparer.OrdinalIgnoreCase);

        foreach (var dimension in dimensions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sourceHandle = ResolveSourceHandle(dimension);
            if (string.IsNullOrWhiteSpace(sourceHandle) || !dimensionLookup.TryGetValue(sourceHandle, out var dxfDimension))
            {
                continue;
            }

            ApplyDimensionMetadata(dxfDimension, dimension);
            RewriteGeometryBlock(dxf, dxfDimension.BlockName ?? dimension.GeometryBlockName, dimension);
        }

        var outputDirectory = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        dxf.Save(outputFilePath, true);
        return Task.CompletedTask;
    }

    private static string? ResolveSourceHandle(DimensionDto dimension)
    {
        if (!string.IsNullOrWhiteSpace(dimension.SourceHandle))
        {
            return dimension.SourceHandle;
        }

        const string prefix = "DIMENSION:";
        return dimension.SourceEntityRef.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? dimension.SourceEntityRef[prefix.Length..]
            : null;
    }

    private static void ApplyDimensionMetadata(DxfDimensionBase dxfDimension, DimensionDto dimension)
    {
        dxfDimension.Text = string.IsNullOrWhiteSpace(dimension.RawTextOverride)
            ? "<>"
            : dimension.RawTextOverride;

        SetPointProperty(dxfDimension, "DefinitionPoint1", dimension.DefPointX, dimension.DefPointY, dimension.DefPointZ);
        SetPointProperty(dxfDimension, "DefinitionPoint2", dimension.DefPoint2X, dimension.DefPoint2Y, dimension.DefPoint2Z);
        SetPointProperty(dxfDimension, "DefinitionPoint3", dimension.DefPoint3X, dimension.DefPoint3Y, dimension.DefPoint3Z);

        if (dimension.RenderTextX is not null && dimension.RenderTextY is not null)
        {
            SetPointProperty(dxfDimension, "TextMidPoint", dimension.RenderTextX.Value, dimension.RenderTextY.Value, 0m);
        }

        if (dimension.RenderTextRotationDegrees is not null)
        {
            SetDecimalProperty(dxfDimension, "TextRotationAngle", dimension.RenderTextRotationDegrees.Value);
            SetDecimalProperty(dxfDimension, "RotationAngle", dimension.RenderTextRotationDegrees.Value);
        }
    }

    private static void RewriteGeometryBlock(DxfFile dxf, string? blockName, DimensionDto dimension)
    {
        if (string.IsNullOrWhiteSpace(blockName))
        {
            return;
        }

        var block = dxf.Blocks.FirstOrDefault(item => string.Equals(item.Name, blockName, StringComparison.OrdinalIgnoreCase));
        if (block is null)
        {
            block = new DxfBlock
            {
                Name = blockName
            };
            dxf.Blocks.Add(block);
        }

        block.Entities.Clear();

        foreach (var primitive in dimension.LinePrimitives.OrderBy(item => item.SortOrder))
        {
            block.Entities.Add(new DxfLine(
                new DxfPoint((double)primitive.StartX, (double)primitive.StartY, 0d),
                new DxfPoint((double)primitive.EndX, (double)primitive.EndY, 0d))
            {
                Layer = primitive.SourceLayer ?? dimension.SourceLayer
            });
        }

        foreach (var primitive in dimension.TextPrimitives.OrderBy(item => item.SortOrder))
        {
            if (!string.IsNullOrWhiteSpace(primitive.AttachmentPoint))
            {
                block.Entities.Add(new DxfMText
                {
                    Text = primitive.Text,
                    InsertionPoint = new DxfPoint((double)primitive.X, (double)primitive.Y, 0d),
                    InitialTextHeight = (double)primitive.Height,
                    RotationAngle = (double)primitive.RotationDegrees,
                    TextStyleName = primitive.StyleName,
                    Layer = primitive.SourceLayer ?? dimension.SourceLayer,
                    AttachmentPoint = ParseAttachmentPoint(primitive.AttachmentPoint)
                });
                continue;
            }

            block.Entities.Add(new DxfText
            {
                Value = primitive.Text,
                Location = new DxfPoint((double)primitive.X, (double)primitive.Y, 0d),
                TextHeight = (double)primitive.Height,
                Rotation = (double)primitive.RotationDegrees,
                TextStyleName = primitive.StyleName,
                Layer = primitive.SourceLayer ?? dimension.SourceLayer
            });
        }

        foreach (var primitive in dimension.InsertPrimitives.OrderBy(item => item.SortOrder))
        {
            block.Entities.Add(new DxfInsert
            {
                Name = primitive.Name,
                Location = new DxfPoint((double)primitive.X, (double)primitive.Y, (double)primitive.Z),
                Rotation = (double)primitive.RotationDegrees,
                XScaleFactor = (double)primitive.ScaleX,
                YScaleFactor = (double)primitive.ScaleY,
                ZScaleFactor = (double)primitive.ScaleZ,
                Layer = primitive.SourceLayer ?? dimension.SourceLayer
            });
        }

        foreach (var primitive in dimension.CirclePrimitives.OrderBy(item => item.SortOrder))
        {
            block.Entities.Add(new DxfCircle(new DxfPoint((double)primitive.CenterX, (double)primitive.CenterY, 0d), (double)primitive.Radius)
            {
                Layer = primitive.SourceLayer ?? dimension.SourceLayer
            });
        }

        foreach (var primitive in dimension.ArcPrimitives.OrderBy(item => item.SortOrder))
        {
            block.Entities.Add(new DxfArc(
                new DxfPoint((double)primitive.CenterX, (double)primitive.CenterY, 0d),
                (double)primitive.Radius,
                (double)primitive.StartAngleDegrees,
                (double)primitive.EndAngleDegrees)
            {
                Layer = primitive.SourceLayer ?? dimension.SourceLayer
            });
        }

        foreach (var primitive in dimension.SolidPrimitives.OrderBy(item => item.SortOrder))
        {
            block.Entities.Add(new DxfSolid
            {
                FirstCorner = new DxfPoint((double)primitive.Point1X, (double)primitive.Point1Y, 0d),
                SecondCorner = new DxfPoint((double)primitive.Point2X, (double)primitive.Point2Y, 0d),
                ThirdCorner = new DxfPoint((double)primitive.Point3X, (double)primitive.Point3Y, 0d),
                FourthCorner = new DxfPoint((double)primitive.Point4X, (double)primitive.Point4Y, 0d),
                Layer = primitive.SourceLayer ?? dimension.SourceLayer
            });
        }
    }

    private static string? GetSourceHandle(DxfEntity entity)
    {
        for (var currentType = entity.GetType(); currentType is not null; currentType = currentType.BaseType)
        {
            foreach (var fieldName in HandleFieldNames)
            {
                var field = currentType.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field?.GetValue(entity) is { } value)
                {
                    var handle = value.ToString();
                    if (!string.IsNullOrWhiteSpace(handle))
                    {
                        return handle;
                    }
                }
            }
        }

        return null;
    }

    private static void SetPointProperty(object instance, string propertyName, decimal x, decimal y, decimal z)
    {
        var property = instance.GetType().GetProperty(propertyName);
        if (property is null || !property.CanWrite || property.PropertyType != typeof(DxfPoint))
        {
            return;
        }

        property.SetValue(instance, new DxfPoint((double)x, (double)y, (double)z));
    }

    private static void SetDecimalProperty(object instance, string propertyName, decimal value)
    {
        var property = instance.GetType().GetProperty(propertyName);
        if (property is null || !property.CanWrite || property.PropertyType != typeof(double))
        {
            return;
        }

        property.SetValue(instance, (double)value);
    }

    private static DxfAttachmentPoint ParseAttachmentPoint(string? value)
    {
        return Enum.TryParse<DxfAttachmentPoint>(value, ignoreCase: true, out var attachmentPoint)
            ? attachmentPoint
            : DxfAttachmentPoint.MiddleCenter;
    }
}
