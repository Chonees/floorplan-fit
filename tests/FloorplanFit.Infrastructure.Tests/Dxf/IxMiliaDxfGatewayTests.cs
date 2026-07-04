using FloorplanFit.Domain.Measurement;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Tests.TestSupport;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Tests.Dxf;

public sealed class IxMiliaDxfGatewayTests
{
    [Fact]
    public async Task ReadFloorPlanAsync_reads_real_santa_barbara_metadata()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var fixturePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var gateway = new IxMiliaDxfGateway();

        var document = await gateway.ReadFloorPlanAsync(fixturePath, CancellationToken.None);

        Assert.Equal("SANTA-BARBARA.dxf", document.OriginalFileName);
        Assert.Equal("SANTA-BARBARA", document.SuggestedName);
        Assert.Equal(LengthUnit.Inch, document.SourceUnit);
        Assert.Equal(25.4m, document.ToMillimetersFactor);
        Assert.Equal("AC1032", document.DxfVersion);
        Assert.StartsWith("bbox:", document.GeometryFingerprint, StringComparison.Ordinal);

        var coordinates = document.GeometryFingerprint["bbox:".Length..]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();

        Assert.Equal(4, coordinates.Length);
        Assert.True(coordinates[2] > coordinates[0]);
        Assert.True(coordinates[3] > coordinates[1]);
    }

    [Fact]
    public async Task ReadFloorPlanAsync_returns_layer_names_from_table_and_entities()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf");
        var dxf = new DxfFile();
        dxf.Header.Version = DxfAcadVersion.R2013;
        dxf.Header["$INSUNITS"] = 1;
        dxf.Layers.Add(new DxfLayer("E-LIGHTING", DxfColor.FromIndex(2)));
        dxf.Entities.Add(new DxfLine(new DxfPoint(0, 0, 0), new DxfPoint(10, 0, 0))
        {
            Layer = "E-POWER"
        });
        dxf.Save(path, asText: true);

        try
        {
            var document = await new IxMiliaDxfGateway().ReadFloorPlanAsync(path, CancellationToken.None);

            Assert.NotNull(document.LayerNames);
            Assert.Contains("E-LIGHTING", document.LayerNames!);
            Assert.Contains("E-POWER", document.LayerNames!);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
