using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls.Preview;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Desktop.Controls;

public sealed class FloorPlanPreviewControl : Control
{
    private const double PreviewPadding = 48d;
    private const double HitTestTolerance = 8d;
    private const double UserZoomStep = 1.12d;
    internal const double MinimumUserZoomFactor = 0.35d;
    internal const double MaximumUserZoomFactor = 6d;
    private INotifyCollectionChanged? observedGeometryPaths;
    private INotifyCollectionChanged? observedPinchMarkers;
    private INotifyCollectionChanged? observedRoomLabels;
    private INotifyCollectionChanged? observedOpeningCandidates;
    private INotifyCollectionChanged? observedOpeningLabels;
    private INotifyCollectionChanged? observedFixedPlanComponents;
    private INotifyCollectionChanged? observedProtectedDetailAssemblies;
    private FloorPlanPreviewGeometry.PreviewCompressionEdge? activeDragEdge;
    private Point dragStartPoint;
    private decimal activePreviewTrimMm;
    private PreviewZoomState previewZoomState = PreviewZoomState.Default;
    private bool isPanningPreview;
    private Point panStartPoint;
    private PreviewZoomState panStartZoomState;

    static FloorPlanPreviewControl()
    {
        AffectsRender<FloorPlanPreviewControl>(
            GeometryPathsProperty,
            HighlightGeometryPathIdProperty,
            PinchMarkersProperty,
            RoomLabelsProperty,
            OpeningCandidatesProperty,
            OpeningLabelsProperty,
            FixedPlanComponentsProperty,
            ProtectedDetailAssembliesProperty,
            PreviewPinchGroupIdProperty,
            PreviewAxisTagProperty,
            IsPinchPlacementArmedProperty);
        GeometryPathsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnGeometryPathsChanged(
                args.GetOldValue<IReadOnlyList<GeometryPathDto>?>(),
                args.GetNewValue<IReadOnlyList<GeometryPathDto>?>()));
        PinchMarkersProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnPinchMarkersChanged(
                args.GetOldValue<IReadOnlyList<PinchMarkerDto>?>(),
                args.GetNewValue<IReadOnlyList<PinchMarkerDto>?>()));
        RoomLabelsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnRoomLabelsChanged(
                args.GetOldValue<IReadOnlyList<RoomLabelDto>?>(),
                args.GetNewValue<IReadOnlyList<RoomLabelDto>?>()));
        OpeningCandidatesProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnOpeningCandidatesChanged(
                args.GetOldValue<IReadOnlyList<OpeningCandidateDto>?>(),
                args.GetNewValue<IReadOnlyList<OpeningCandidateDto>?>()));
        OpeningLabelsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnOpeningLabelsChanged(
                args.GetOldValue<IReadOnlyList<OpeningLabelDto>?>(),
                args.GetNewValue<IReadOnlyList<OpeningLabelDto>?>()));
        FixedPlanComponentsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnFixedPlanComponentsChanged(
                args.GetOldValue<IReadOnlyList<FixedPlanComponentDto>?>(),
                args.GetNewValue<IReadOnlyList<FixedPlanComponentDto>?>()));
        ProtectedDetailAssembliesProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnProtectedDetailAssembliesChanged(
                args.GetOldValue<IReadOnlyList<ProtectedDetailAssemblyDto>?>(),
                args.GetNewValue<IReadOnlyList<ProtectedDetailAssemblyDto>?>()));
    }

    public static readonly StyledProperty<IReadOnlyList<GeometryPathDto>?> GeometryPathsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<GeometryPathDto>?>(nameof(GeometryPaths));

    public static readonly StyledProperty<Guid?> HighlightGeometryPathIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(HighlightGeometryPathId));

    public static readonly StyledProperty<IReadOnlyList<PinchMarkerDto>?> PinchMarkersProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<PinchMarkerDto>?>(nameof(PinchMarkers));

    public static readonly StyledProperty<IReadOnlyList<RoomLabelDto>?> RoomLabelsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<RoomLabelDto>?>(nameof(RoomLabels));

    public static readonly StyledProperty<IReadOnlyList<OpeningCandidateDto>?> OpeningCandidatesProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<OpeningCandidateDto>?>(nameof(OpeningCandidates));

    public static readonly StyledProperty<IReadOnlyList<OpeningLabelDto>?> OpeningLabelsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<OpeningLabelDto>?>(nameof(OpeningLabels));

    public static readonly StyledProperty<IReadOnlyList<FixedPlanComponentDto>?> FixedPlanComponentsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<FixedPlanComponentDto>?>(nameof(FixedPlanComponents));

    public static readonly StyledProperty<IReadOnlyList<ProtectedDetailAssemblyDto>?> ProtectedDetailAssembliesProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<ProtectedDetailAssemblyDto>?>(nameof(ProtectedDetailAssemblies));

    public static readonly StyledProperty<Guid?> PreviewPinchGroupIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(PreviewPinchGroupId));

    public static readonly StyledProperty<string?> PreviewAxisTagProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, string?>(nameof(PreviewAxisTag));

    public static readonly StyledProperty<bool> IsPinchPlacementArmedProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, bool>(nameof(IsPinchPlacementArmed));

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

    public IReadOnlyList<PinchMarkerDto>? PinchMarkers
    {
        get => GetValue(PinchMarkersProperty);
        set => SetValue(PinchMarkersProperty, value);
    }

    public IReadOnlyList<RoomLabelDto>? RoomLabels
    {
        get => GetValue(RoomLabelsProperty);
        set => SetValue(RoomLabelsProperty, value);
    }

    public IReadOnlyList<OpeningCandidateDto>? OpeningCandidates
    {
        get => GetValue(OpeningCandidatesProperty);
        set => SetValue(OpeningCandidatesProperty, value);
    }

    public IReadOnlyList<OpeningLabelDto>? OpeningLabels
    {
        get => GetValue(OpeningLabelsProperty);
        set => SetValue(OpeningLabelsProperty, value);
    }

    public IReadOnlyList<FixedPlanComponentDto>? FixedPlanComponents
    {
        get => GetValue(FixedPlanComponentsProperty);
        set => SetValue(FixedPlanComponentsProperty, value);
    }

    public IReadOnlyList<ProtectedDetailAssemblyDto>? ProtectedDetailAssemblies
    {
        get => GetValue(ProtectedDetailAssembliesProperty);
        set => SetValue(ProtectedDetailAssembliesProperty, value);
    }

    public Guid? PreviewPinchGroupId
    {
        get => GetValue(PreviewPinchGroupIdProperty);
        set => SetValue(PreviewPinchGroupIdProperty, value);
    }

    public string? PreviewAxisTag
    {
        get => GetValue(PreviewAxisTagProperty);
        set => SetValue(PreviewAxisTagProperty, value);
    }

    public bool IsPinchPlacementArmed
    {
        get => GetValue(IsPinchPlacementArmedProperty);
        set => SetValue(IsPinchPlacementArmedProperty, value);
    }

    public event EventHandler<GeometryPathClickedEventArgs>? GeometryPathClicked;

    internal static Rect GetLocalRenderBounds(Rect layoutBounds)
    {
        return new Rect(0d, 0d, Math.Max(0d, layoutBounds.Width), Math.Max(0d, layoutBounds.Height));
    }

    internal static IReadOnlyList<GeometryPathDto> BuildHitTestGeometry(
        IReadOnlyList<GeometryPathDto>? geometryPaths,
        IReadOnlyList<OpeningCandidateDto>? openings = null,
        IReadOnlyList<FixedPlanComponentDto>? fixedPlanComponents = null,
        IReadOnlyList<ProtectedDetailAssemblyDto>? protectedDetailAssemblies = null)
    {
        if (geometryPaths is not { Count: > 0 })
        {
            return [];
        }

        return PreviewArtifactGeometryIndex
            .Create(openings, fixedPlanComponents, protectedDetailAssemblies)
            .OrderForHitTesting(geometryPaths);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachGeometryPathsCollectionObserver(GeometryPaths);
        AttachPinchMarkersCollectionObserver(PinchMarkers);
        AttachRoomLabelsCollectionObserver(RoomLabels);
        AttachOpeningCandidatesCollectionObserver(OpeningCandidates);
        AttachOpeningLabelsCollectionObserver(OpeningLabels);
        AttachFixedPlanComponentsCollectionObserver(FixedPlanComponents);
        AttachProtectedDetailAssembliesCollectionObserver(ProtectedDetailAssemblies);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachRoomLabelsCollectionObserver();
        DetachOpeningLabelsCollectionObserver();
        DetachOpeningCandidatesCollectionObserver();
        DetachFixedPlanComponentsCollectionObserver();
        DetachProtectedDetailAssembliesCollectionObserver();
        DetachPinchMarkersCollectionObserver();
        DetachGeometryPathsCollectionObserver();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var pointerProperties = e.GetCurrentPoint(this).Properties;
        if (pointerProperties.IsMiddleButtonPressed)
        {
            isPanningPreview = true;
            panStartPoint = e.GetPosition(this);
            panStartZoomState = previewZoomState;
            activeDragEdge = null;
            activePreviewTrimMm = 0m;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!pointerProperties.IsLeftButtonPressed)
        {
            return;
        }

        var pointerPosition = e.GetPosition(this);
        var axisTag = ParseAxisTag();
        if (!IsPinchPlacementArmed && axisTag is not null)
        {
            var edge = ResolveEdgeDrag(pointerPosition, axisTag.Value);
            if (edge is not null)
            {
                activeDragEdge = edge;
                dragStartPoint = pointerPosition;
                activePreviewTrimMm = 0m;
                e.Pointer.Capture(this);
                e.Handled = true;
                return;
            }
        }

        var viewport = GetPreviewViewport(axisTag);
        if (viewport is null)
        {
            return;
        }

        var hitTestGeometry = BuildHitTestGeometry(
            GeometryPaths,
            OpeningCandidates,
            FixedPlanComponents,
            ProtectedDetailAssemblies);
        var hit = FloorPlanPreviewGeometry.HitTestPathDetail(hitTestGeometry, viewport.Value, pointerPosition, HitTestTolerance);

        if (hit is null)
        {
            return;
        }

        GeometryPathClicked?.Invoke(this, new GeometryPathClickedEventArgs(hit.Value.GeometryPathId, hit.Value.PositionRatio));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (isPanningPreview)
        {
            previewZoomState = ResolvePanStateForDrag(panStartZoomState, panStartPoint, e.GetPosition(this));
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (activeDragEdge is null || GeometryPaths is not { Count: > 0 })
        {
            return;
        }

        var viewport = GetPreviewViewport(ParseAxisTag());
        if (viewport is null || viewport.Value.Scale <= double.Epsilon)
        {
            return;
        }

        var current = e.GetPosition(this);
        var pixelDelta = activeDragEdge switch
        {
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Right => Math.Max(0d, dragStartPoint.X - current.X),
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Left => Math.Max(0d, current.X - dragStartPoint.X),
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Top => Math.Max(0d, current.Y - dragStartPoint.Y),
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Bottom => Math.Max(0d, dragStartPoint.Y - current.Y),
            _ => 0d
        };

        activePreviewTrimMm = (decimal)(pixelDelta / viewport.Value.Scale);
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (GeometryPaths is not { Count: > 0 })
        {
            return;
        }

        var axisTag = ParseAxisTag();
        var baseViewport = FloorPlanPreviewGeometry.CalculateViewport(GeometryPaths, GetGeometryViewportBounds(axisTag), PreviewPadding);
        if (baseViewport is null)
        {
            return;
        }

        previewZoomState = ResolveZoomStateForWheel(
            baseViewport.Value,
            previewZoomState,
            e.GetPosition(this),
            e.Delta.Y);
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (isPanningPreview)
        {
            isPanningPreview = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (activeDragEdge is null)
        {
            return;
        }

        activeDragEdge = null;
        activePreviewTrimMm = 0m;
        e.Pointer.Capture(null);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = GetLocalRenderBounds(Bounds);
        PreviewWorkspaceRenderer.Render(context, bounds);
        context.DrawRectangle(new Pen(Brushes.Gainsboro, 1), bounds.Deflate(0.5));

        var axisTag = ParseAxisTag();
        if (axisTag is not null)
        {
            CompressionHandlePreviewLayerRenderer.Render(context, bounds, axisTag.Value, IsPinchPlacementArmed);
        }

        if (GeometryPaths is not { Count: > 0 })
        {
            return;
        }

        var viewport = GetPreviewViewport(axisTag);
        if (viewport is null)
        {
            return;
        }

        var previewGeometry = BuildPreviewGeometry(axisTag);
        var artifactIndex = PreviewArtifactGeometryIndex.Create(OpeningCandidates, FixedPlanComponents, ProtectedDetailAssemblies);
        var orderedPaths = previewGeometry
            .Where(path => !artifactIndex.OpeningGeometryPathIds.Contains(path.Id))
            .Where(path => !artifactIndex.FixedPlanComponentGeometryPathIds.Contains(path.Id))
            .Where(path => !artifactIndex.ProtectedDetailGeometryPathIds.Contains(path.Id))
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

        OpeningPreviewLayerRenderer.Render(
            context,
            viewport.Value,
            previewGeometry,
            OpeningCandidates,
            artifactIndex.OpeningGeometryPathIds,
            HighlightGeometryPathId);
        FixedPlanComponentPreviewLayerRenderer.Render(
            context,
            viewport.Value,
            previewGeometry,
            FixedPlanComponents,
            artifactIndex.FixedPlanComponentGeometryPathIds,
            HighlightGeometryPathId);
        ProtectedDetailPreviewLayerRenderer.Render(
            context,
            viewport.Value,
            previewGeometry,
            ProtectedDetailAssemblies,
            artifactIndex.ProtectedDetailGeometryPathIds,
            HighlightGeometryPathId);
        CadTextPreviewLayerRenderer.RenderRoomLabels(context, viewport.Value, RoomLabels);
        CadTextPreviewLayerRenderer.RenderOpeningLabels(context, viewport.Value, OpeningLabels);
        PinchMarkerPreviewLayerRenderer.Render(
            context,
            viewport.Value,
            previewGeometry,
            PinchMarkers,
            PreviewPinchGroupId,
            PreviewAxisTag);
    }

    internal static double CalculateWheelZoomFactor(double currentZoomFactor, double wheelDeltaY)
    {
        var safeCurrentZoom = double.IsFinite(currentZoomFactor)
            ? currentZoomFactor
            : 1d;
        var requestedZoom = safeCurrentZoom * Math.Pow(UserZoomStep, wheelDeltaY);
        return Math.Clamp(requestedZoom, MinimumUserZoomFactor, MaximumUserZoomFactor);
    }

    internal static PreviewZoomState ResolveZoomStateForWheel(
        FloorPlanPreviewGeometry.PreviewViewport baseViewport,
        PreviewZoomState currentState,
        Point pointerPosition,
        double wheelDeltaY)
    {
        var currentZoom = Math.Clamp(
            double.IsFinite(currentState.ZoomFactor) ? currentState.ZoomFactor : 1d,
            MinimumUserZoomFactor,
            MaximumUserZoomFactor);
        var currentViewport = baseViewport.WithUserTransform(currentZoom, currentState.PanOffset);
        var worldPoint = currentViewport.Unproject(pointerPosition);
        var nextZoom = CalculateWheelZoomFactor(currentZoom, wheelDeltaY);
        var nextViewportWithoutPan = baseViewport.WithUserTransform(nextZoom, default);
        var projectedWithoutPan = nextViewportWithoutPan.Project(worldPoint.X, worldPoint.Y);
        var nextPanOffset = new Vector(
            pointerPosition.X - projectedWithoutPan.X,
            pointerPosition.Y - projectedWithoutPan.Y);

        return new PreviewZoomState(nextZoom, nextPanOffset);
    }

    internal static PreviewZoomState ResolvePanStateForDrag(
        PreviewZoomState startState,
        Point startPointerPosition,
        Point currentPointerPosition)
    {
        var pointerDelta = currentPointerPosition - startPointerPosition;
        return new PreviewZoomState(
            startState.ZoomFactor,
            startState.PanOffset + pointerDelta);
    }

    private IReadOnlyList<GeometryPathDto> BuildPreviewGeometry(PinchAxisTag? axisTag)
    {
        if (axisTag is null || activeDragEdge is null)
        {
            return GeometryPaths ?? [];
        }

        return FloorPlanPreviewGeometry.CreatePreviewGeometry(
            GeometryPaths,
            axisTag.Value,
            PinchMarkerPreviewLayerRenderer.FilterForPreviewGroup(PinchMarkers, PreviewPinchGroupId),
            activePreviewTrimMm,
            activeDragEdge.Value);
    }

    private FloorPlanPreviewGeometry.PreviewCompressionEdge? ResolveEdgeDrag(Point pointerPosition, PinchAxisTag axisTag)
    {
        return FloorPlanPreviewGeometry.TryResolveCompressionHandle(pointerPosition, GetLocalRenderBounds(Bounds), axisTag);
    }

    private Rect GetGeometryViewportBounds(PinchAxisTag? axisTag)
    {
        var bounds = GetLocalRenderBounds(Bounds);

        return axisTag is null
            ? bounds
            : FloorPlanPreviewGeometry.GetGeometryViewportBounds(bounds, axisTag.Value);
    }

    private FloorPlanPreviewGeometry.PreviewViewport? GetPreviewViewport(PinchAxisTag? axisTag)
    {
        var baseViewport = FloorPlanPreviewGeometry.CalculateViewport(GeometryPaths, GetGeometryViewportBounds(axisTag), PreviewPadding);
        return baseViewport?.WithUserTransform(previewZoomState.ZoomFactor, previewZoomState.PanOffset);
    }

    private PinchAxisTag? ParseAxisTag()
    {
        return Enum.TryParse<PinchAxisTag>(PreviewAxisTag, out var axisTag)
            ? axisTag
            : null;
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

    private void OnPinchMarkersChanged(IReadOnlyList<PinchMarkerDto>? oldValue, IReadOnlyList<PinchMarkerDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachPinchMarkersCollectionObserver();
            AttachPinchMarkersCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnRoomLabelsChanged(IReadOnlyList<RoomLabelDto>? oldValue, IReadOnlyList<RoomLabelDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachRoomLabelsCollectionObserver();
            AttachRoomLabelsCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnOpeningCandidatesChanged(IReadOnlyList<OpeningCandidateDto>? oldValue, IReadOnlyList<OpeningCandidateDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachOpeningCandidatesCollectionObserver();
            AttachOpeningCandidatesCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnOpeningLabelsChanged(IReadOnlyList<OpeningLabelDto>? oldValue, IReadOnlyList<OpeningLabelDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachOpeningLabelsCollectionObserver();
            AttachOpeningLabelsCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnFixedPlanComponentsChanged(IReadOnlyList<FixedPlanComponentDto>? oldValue, IReadOnlyList<FixedPlanComponentDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachFixedPlanComponentsCollectionObserver();
            AttachFixedPlanComponentsCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnProtectedDetailAssembliesChanged(
        IReadOnlyList<ProtectedDetailAssemblyDto>? oldValue,
        IReadOnlyList<ProtectedDetailAssemblyDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachProtectedDetailAssembliesCollectionObserver();
            AttachProtectedDetailAssembliesCollectionObserver(newValue);
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

    private void AttachPinchMarkersCollectionObserver(IReadOnlyList<PinchMarkerDto>? value)
    {
        if (ReferenceEquals(observedPinchMarkers, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedPinchMarkers = notifyCollectionChanged;
        observedPinchMarkers.CollectionChanged += PinchMarkersCollectionChanged;
    }

    private void DetachPinchMarkersCollectionObserver()
    {
        if (observedPinchMarkers is null)
        {
            return;
        }

        observedPinchMarkers.CollectionChanged -= PinchMarkersCollectionChanged;
        observedPinchMarkers = null;
    }

    private void AttachRoomLabelsCollectionObserver(IReadOnlyList<RoomLabelDto>? value)
    {
        if (ReferenceEquals(observedRoomLabels, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedRoomLabels = notifyCollectionChanged;
        observedRoomLabels.CollectionChanged += RoomLabelsCollectionChanged;
    }

    private void DetachRoomLabelsCollectionObserver()
    {
        if (observedRoomLabels is null)
        {
            return;
        }

        observedRoomLabels.CollectionChanged -= RoomLabelsCollectionChanged;
        observedRoomLabels = null;
    }

    private void AttachOpeningCandidatesCollectionObserver(IReadOnlyList<OpeningCandidateDto>? value)
    {
        if (ReferenceEquals(observedOpeningCandidates, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedOpeningCandidates = notifyCollectionChanged;
        observedOpeningCandidates.CollectionChanged += OpeningCandidatesCollectionChanged;
    }

    private void DetachOpeningCandidatesCollectionObserver()
    {
        if (observedOpeningCandidates is null)
        {
            return;
        }

        observedOpeningCandidates.CollectionChanged -= OpeningCandidatesCollectionChanged;
        observedOpeningCandidates = null;
    }

    private void AttachOpeningLabelsCollectionObserver(IReadOnlyList<OpeningLabelDto>? value)
    {
        if (ReferenceEquals(observedOpeningLabels, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedOpeningLabels = notifyCollectionChanged;
        observedOpeningLabels.CollectionChanged += OpeningLabelsCollectionChanged;
    }

    private void DetachOpeningLabelsCollectionObserver()
    {
        if (observedOpeningLabels is null)
        {
            return;
        }

        observedOpeningLabels.CollectionChanged -= OpeningLabelsCollectionChanged;
        observedOpeningLabels = null;
    }

    private void AttachFixedPlanComponentsCollectionObserver(IReadOnlyList<FixedPlanComponentDto>? value)
    {
        if (ReferenceEquals(observedFixedPlanComponents, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedFixedPlanComponents = notifyCollectionChanged;
        observedFixedPlanComponents.CollectionChanged += FixedPlanComponentsCollectionChanged;
    }

    private void AttachProtectedDetailAssembliesCollectionObserver(IReadOnlyList<ProtectedDetailAssemblyDto>? value)
    {
        if (ReferenceEquals(observedProtectedDetailAssemblies, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedProtectedDetailAssemblies = notifyCollectionChanged;
        observedProtectedDetailAssemblies.CollectionChanged += ProtectedDetailAssembliesCollectionChanged;
    }

    private void DetachFixedPlanComponentsCollectionObserver()
    {
        if (observedFixedPlanComponents is null)
        {
            return;
        }

        observedFixedPlanComponents.CollectionChanged -= FixedPlanComponentsCollectionChanged;
        observedFixedPlanComponents = null;
    }

    private void DetachProtectedDetailAssembliesCollectionObserver()
    {
        if (observedProtectedDetailAssemblies is null)
        {
            return;
        }

        observedProtectedDetailAssemblies.CollectionChanged -= ProtectedDetailAssembliesCollectionChanged;
        observedProtectedDetailAssemblies = null;
    }

    private void GeometryPathsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void PinchMarkersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void RoomLabelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void OpeningCandidatesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void OpeningLabelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void FixedPlanComponentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void ProtectedDetailAssembliesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    public sealed class GeometryPathClickedEventArgs : EventArgs
    {
        public GeometryPathClickedEventArgs(Guid geometryPathId, decimal positionRatio)
        {
            GeometryPathId = geometryPathId;
            PositionRatio = positionRatio;
        }

        public Guid GeometryPathId { get; }

        public decimal PositionRatio { get; }
    }

    internal readonly record struct PreviewZoomState(double ZoomFactor, Vector PanOffset)
    {
        public static PreviewZoomState Default { get; } = new(1d, default);
    }
}
