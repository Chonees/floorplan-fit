using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls.Preview;

namespace FloorplanFit.Desktop.Controls;

public sealed class RegistrationOverlayPreviewControl : Control
{
    private const double PreviewPadding = 36d;
    private static readonly Pen CanonicalPen = new(
        new SolidColorBrush(Color.FromArgb(220, 226, 232, 240)),
        1.8d);
    private static readonly Pen ElectricalPen = new(
        new SolidColorBrush(Color.FromArgb(185, 34, 211, 238)),
        2.4d);

    static RegistrationOverlayPreviewControl()
    {
        AffectsRender<RegistrationOverlayPreviewControl>(CanonicalGeometryProperty, ElectricalGeometryProperty);
    }

    public static readonly StyledProperty<IReadOnlyList<GeometryPathDto>?> CanonicalGeometryProperty =
        AvaloniaProperty.Register<RegistrationOverlayPreviewControl, IReadOnlyList<GeometryPathDto>?>(nameof(CanonicalGeometry));

    public static readonly StyledProperty<IReadOnlyList<GeometryPathDto>?> ElectricalGeometryProperty =
        AvaloniaProperty.Register<RegistrationOverlayPreviewControl, IReadOnlyList<GeometryPathDto>?>(nameof(ElectricalGeometry));

    public IReadOnlyList<GeometryPathDto>? CanonicalGeometry
    {
        get => GetValue(CanonicalGeometryProperty);
        set => SetValue(CanonicalGeometryProperty, value);
    }

    public IReadOnlyList<GeometryPathDto>? ElectricalGeometry
    {
        get => GetValue(ElectricalGeometryProperty);
        set => SetValue(ElectricalGeometryProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = new Rect(0d, 0d, Math.Max(0d, Bounds.Width), Math.Max(0d, Bounds.Height));
        var allGeometry = (CanonicalGeometry ?? [])
            .Concat(ElectricalGeometry ?? [])
            .ToArray();
        var viewport = FloorPlanPreviewGeometry.CalculateViewport(allGeometry, bounds, PreviewPadding);

        using var clip = context.PushClip(bounds);
        PreviewWorkspaceRenderer.Render(context, bounds, viewport);
        context.DrawRectangle(new Pen(Brushes.Gainsboro, 1d), bounds.Deflate(0.5d));

        if (viewport is not { } fittedViewport)
        {
            return;
        }

        DrawGeometry(context, bounds, fittedViewport, CanonicalGeometry, CanonicalPen);
        DrawGeometry(context, bounds, fittedViewport, ElectricalGeometry, ElectricalPen);
    }

    private static void DrawGeometry(
        DrawingContext context,
        Rect bounds,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        IReadOnlyList<GeometryPathDto>? geometry,
        Pen pen)
    {
        foreach (var segment in (geometry ?? []).SelectMany(path => path.Segments))
        {
            PreviewLineClipper.DrawLine(
                context,
                bounds,
                pen,
                viewport.Project(segment.StartX, segment.StartY),
                viewport.Project(segment.EndX, segment.EndY));
        }
    }

}
