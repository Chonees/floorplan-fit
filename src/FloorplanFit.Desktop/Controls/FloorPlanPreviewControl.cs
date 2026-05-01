using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls;

public sealed class FloorPlanPreviewControl : Control
{
    public static readonly StyledProperty<IReadOnlyList<GeometryPathDto>?> GeometryPathsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<GeometryPathDto>?>(nameof(GeometryPaths));

    public static readonly StyledProperty<Guid?> HighlightGeometryPathIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(HighlightGeometryPathId));

    public IReadOnlyList<GeometryPathDto>? GeometryPaths
    {
        get => GetValue(GeometryPathsProperty);
        set => SetValue(GeometryPathsProperty, value);
    }

    public Guid? HighlightGeometryPathId
    {
        get => GetValue(HighlightGeometryPathIdProperty);
        set => SetValue(HighlightGeometryPathIdProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        context.FillRectangle(Brushes.White, bounds);
        context.DrawRectangle(new Pen(Brushes.Gainsboro, 1), bounds.Deflate(0.5));

        if (GeometryPaths is not { Count: > 0 })
        {
            return;
        }

        var segments = GeometryPaths
            .SelectMany(path => path.Segments.Select(segment => (Path: path, Segment: segment)))
            .ToArray();

        if (segments.Length == 0)
        {
            return;
        }

        var minX = segments.Min(item => Math.Min((double)item.Segment.StartX, (double)item.Segment.EndX));
        var minY = segments.Min(item => Math.Min((double)item.Segment.StartY, (double)item.Segment.EndY));
        var maxX = segments.Max(item => Math.Max((double)item.Segment.StartX, (double)item.Segment.EndX));
        var maxY = segments.Max(item => Math.Max((double)item.Segment.StartY, (double)item.Segment.EndY));

        const double padding = 16d;
        var width = Math.Max(maxX - minX, 1d);
        var height = Math.Max(maxY - minY, 1d);
        var availableWidth = Math.Max(bounds.Width - (padding * 2d), 1d);
        var availableHeight = Math.Max(bounds.Height - (padding * 2d), 1d);
        var scale = Math.Min(availableWidth / width, availableHeight / height);
        var offsetX = padding + ((availableWidth - (width * scale)) / 2d);
        var offsetY = padding + ((availableHeight - (height * scale)) / 2d);

        Point Project(decimal x, decimal y)
        {
            var projectedX = offsetX + (((double)x - minX) * scale);
            var projectedY = bounds.Height - (offsetY + (((double)y - minY) * scale));
            return new Point(projectedX, projectedY);
        }

        foreach (var path in GeometryPaths)
        {
            var isHighlighted = HighlightGeometryPathId == path.Id;
            var pen = new Pen(
                isHighlighted ? Brushes.OrangeRed : Brushes.SlateGray,
                isHighlighted ? 2.5d : 1.25d);

            foreach (var segment in path.Segments)
            {
                context.DrawLine(
                    pen,
                    Project(segment.StartX, segment.StartY),
                    Project(segment.EndX, segment.EndY));
            }
        }
    }
}
