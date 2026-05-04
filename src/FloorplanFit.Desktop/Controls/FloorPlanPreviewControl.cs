using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls;

public sealed class FloorPlanPreviewControl : Control
{
    private const double PreviewPadding = 16d;
    private const double HitTestTolerance = 8d;
    private INotifyCollectionChanged? observedGeometryPaths;

    static FloorPlanPreviewControl()
    {
        AffectsRender<FloorPlanPreviewControl>(GeometryPathsProperty, HighlightGeometryPathIdProperty);
        GeometryPathsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnGeometryPathsChanged(
                args.GetOldValue<IReadOnlyList<GeometryPathDto>?>(),
                args.GetNewValue<IReadOnlyList<GeometryPathDto>?>()));
    }

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

    public event EventHandler<GeometryPathClickedEventArgs>? GeometryPathClicked;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachGeometryPathsCollectionObserver(GeometryPaths);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachGeometryPathsCollectionObserver();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var geometryPathId = FloorPlanPreviewGeometry.HitTestPath(
            GeometryPaths,
            Bounds,
            e.GetPosition(this),
            PreviewPadding,
            HitTestTolerance);

        if (geometryPathId is null)
        {
            return;
        }

        GeometryPathClicked?.Invoke(this, new GeometryPathClickedEventArgs(geometryPathId.Value));
        e.Handled = true;
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

        var viewport = FloorPlanPreviewGeometry.CalculateViewport(GeometryPaths, bounds, PreviewPadding);
        if (viewport is null)
        {
            return;
        }

        var orderedPaths = GeometryPaths
            .OrderBy(path => FloorPlanPreviewGeometry.GetPathStyle(path.Id, HighlightGeometryPathId).IsHighlighted)
            .ToArray();

        foreach (var path in orderedPaths)
        {
            var style = FloorPlanPreviewGeometry.GetPathStyle(path.Id, HighlightGeometryPathId);
            var pen = new Pen(new SolidColorBrush(style.Color), style.Thickness);

            foreach (var segment in path.Segments)
            {
                context.DrawLine(
                    pen,
                    viewport.Value.Project(segment.StartX, segment.StartY),
                    viewport.Value.Project(segment.EndX, segment.EndY));
            }
        }
    }

    private void OnGeometryPathsChanged(IReadOnlyList<GeometryPathDto>? oldValue, IReadOnlyList<GeometryPathDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachGeometryPathsCollectionObserver();
            AttachGeometryPathsCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void AttachGeometryPathsCollectionObserver(IReadOnlyList<GeometryPathDto>? value)
    {
        if (ReferenceEquals(observedGeometryPaths, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedGeometryPaths = notifyCollectionChanged;
        observedGeometryPaths.CollectionChanged += GeometryPathsCollectionChanged;
    }

    private void DetachGeometryPathsCollectionObserver()
    {
        if (observedGeometryPaths is null)
        {
            return;
        }

        observedGeometryPaths.CollectionChanged -= GeometryPathsCollectionChanged;
        observedGeometryPaths = null;
    }

    private void GeometryPathsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    public sealed class GeometryPathClickedEventArgs : EventArgs
    {
        public GeometryPathClickedEventArgs(Guid geometryPathId)
        {
            GeometryPathId = geometryPathId;
        }

        public Guid GeometryPathId { get; }
    }
}
