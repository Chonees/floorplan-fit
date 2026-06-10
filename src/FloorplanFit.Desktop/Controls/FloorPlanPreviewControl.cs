using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls.Preview;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Desktop.Controls;

public sealed class FloorPlanPreviewControl : Control
{
    private const double PreviewPadding = 48d;
    private const double HitTestTolerance = 8d;
    private const double UserZoomStep = 1.12d;
    private static readonly TimeSpan ChangePreviewAnimationDuration = TimeSpan.FromMilliseconds(260);
    internal const decimal MovementPersistenceEpsilon = 0.001m;
    private const double DimensionHandleHitTolerance = 10d;
    internal const double MinimumUserZoomFactor = 0.35d;
    internal const double MaximumUserZoomFactor = 80d;
    private readonly PreviewCollectionObserverHub collectionObserverHub;
    private FloorPlanPreviewGeometry.PreviewCompressionEdge? activeDragEdge;
    private PreviewArtifactMoveState? activeArtifactMove;
    private Point dragStartPoint;
    private decimal activePreviewTrimSourceUnits;
    private PreviewZoomState previewZoomState = PreviewZoomState.Default;
    private bool isPanningPreview;
    private Point panStartPoint;
    private PreviewZoomState panStartZoomState;
    private FloorPlanPreviewGeometry.PreviewViewport? lastBaseViewport;
    private PinchAxisTag? lastBaseViewportAxisTag;
    private IReadOnlyList<GeometryPathDto> lastRenderedPreviewGeometry = [];
    private IReadOnlyList<GeometryPathDto> changePreviewGhostGeometry = [];
    private DateTimeOffset? changePreviewAnimationStartedAt;
    private readonly DispatcherTimer changePreviewAnimationTimer;
    private bool preserveViewportOnNextRender;
    private FloorPlanMoveDragState? activeFloorPlanMove;
    private PreviewDimensionEditState? activeDimensionEdit;
    private PreviewPendingDimensionEditState? pendingDimensionEdit;

    static FloorPlanPreviewControl()
    {
        AffectsRender<FloorPlanPreviewControl>(
            SitePlanGeometryPathsProperty,
            SitePlanRenderPathsProperty,
            SitePlanTextsProperty,
            GeometryPathsProperty,
            HighlightGeometryPathIdProperty,
            PinchMarkersProperty,
            RoomLabelsProperty,
            HighlightRoomLabelIdProperty,
            WallCandidatesProperty,
            OpeningCandidatesProperty,
            OpeningLabelsProperty,
            HighlightOpeningLabelIdProperty,
            DimensionsProperty,
            ChangedNumberDimensionIdsProperty,
            MeasurementContextProperty,
            DimensionBindingsProperty,
            DimensionAssociationsProperty,
            MeasurementCorridorsProperty,
            MeasurementNodesProperty,
            DimensionIntervalBindingsProperty,
            ArticulationBandsProperty,
            HighlightDimensionIdProperty,
            FixedPlanComponentsProperty,
            ProtectedDetailAssembliesProperty,
            CuratedPlanArtifactsProperty,
            PreviewPinchGroupIdProperty,
            PreviewAxisTagProperty,
            IsPinchPlacementArmedProperty,
            AreDimensionsVisibleProperty);
        SitePlanGeometryPathsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnSitePlanGeometryPathsChanged(
                args.GetOldValue<IReadOnlyList<GeometryPathDto>?>(),
                args.GetNewValue<IReadOnlyList<GeometryPathDto>?>()));
        SitePlanRenderPathsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnSitePlanRenderPathsChanged(
                args.GetOldValue<IReadOnlyList<SitePlanRenderPathDto>?>(),
                args.GetNewValue<IReadOnlyList<SitePlanRenderPathDto>?>()));
        SitePlanTextsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnSitePlanTextsChanged(
                args.GetOldValue<IReadOnlyList<SitePlanTextDto>?>(),
                args.GetNewValue<IReadOnlyList<SitePlanTextDto>?>()));
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
        WallCandidatesProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnWallCandidatesChanged(
                args.GetOldValue<IReadOnlyList<WallCandidateDto>?>(),
                args.GetNewValue<IReadOnlyList<WallCandidateDto>?>()));
        OpeningCandidatesProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnOpeningCandidatesChanged(
                args.GetOldValue<IReadOnlyList<OpeningCandidateDto>?>(),
                args.GetNewValue<IReadOnlyList<OpeningCandidateDto>?>()));
        OpeningLabelsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnOpeningLabelsChanged(
                args.GetOldValue<IReadOnlyList<OpeningLabelDto>?>(),
                args.GetNewValue<IReadOnlyList<OpeningLabelDto>?>()));
        DimensionsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnDimensionsChanged(
                args.GetOldValue<IReadOnlyList<DimensionDto>?>(),
                args.GetNewValue<IReadOnlyList<DimensionDto>?>()));
        DimensionBindingsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnDimensionBindingsChanged(
                args.GetOldValue<IReadOnlyList<DimensionBindingDto>?>(),
                args.GetNewValue<IReadOnlyList<DimensionBindingDto>?>()));
        DimensionAssociationsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnDimensionAssociationsChanged(
                args.GetOldValue<IReadOnlyList<DimensionAssociationDto>?>(),
                args.GetNewValue<IReadOnlyList<DimensionAssociationDto>?>()));
        MeasurementCorridorsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnMeasurementCorridorsChanged(
                args.GetOldValue<IReadOnlyList<MeasurementCorridorDto>?>(),
                args.GetNewValue<IReadOnlyList<MeasurementCorridorDto>?>()));
        MeasurementNodesProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnMeasurementNodesChanged(
                args.GetOldValue<IReadOnlyList<MeasurementNodeDto>?>(),
                args.GetNewValue<IReadOnlyList<MeasurementNodeDto>?>()));
        DimensionIntervalBindingsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnDimensionIntervalBindingsChanged(
                args.GetOldValue<IReadOnlyList<DimensionIntervalBindingDto>?>(),
                args.GetNewValue<IReadOnlyList<DimensionIntervalBindingDto>?>()));
        ArticulationBandsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnArticulationBandsChanged(
                args.GetOldValue<IReadOnlyList<ArticulationBandDto>?>(),
                args.GetNewValue<IReadOnlyList<ArticulationBandDto>?>()));
        SelectedMeasurementCorridorIdProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, _) => control.InvalidateVisual());
        SelectedMeasurementNodeIdProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, _) => control.InvalidateVisual());
        SelectedMeasurementStartNodeIdProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, _) => control.InvalidateVisual());
        SelectedMeasurementEndNodeIdProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, _) => control.InvalidateVisual());
        FixedPlanComponentsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnFixedPlanComponentsChanged(
                args.GetOldValue<IReadOnlyList<FixedPlanComponentDto>?>(),
                args.GetNewValue<IReadOnlyList<FixedPlanComponentDto>?>()));
        ProtectedDetailAssembliesProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnProtectedDetailAssembliesChanged(
                args.GetOldValue<IReadOnlyList<ProtectedDetailAssemblyDto>?>(),
                args.GetNewValue<IReadOnlyList<ProtectedDetailAssemblyDto>?>()));
        CuratedPlanArtifactsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnCuratedPlanArtifactsChanged(
                args.GetOldValue<IReadOnlyList<CuratedPlanArtifactDto>?>(),
                args.GetNewValue<IReadOnlyList<CuratedPlanArtifactDto>?>()));
    }

    public FloorPlanPreviewControl()
    {
        collectionObserverHub = new PreviewCollectionObserverHub(OnObservedCollectionChanged);
        changePreviewAnimationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        changePreviewAnimationTimer.Tick += (_, _) => TickChangePreviewAnimation();
    }

    public static readonly StyledProperty<IReadOnlyList<GeometryPathDto>?> SitePlanGeometryPathsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<GeometryPathDto>?>(nameof(SitePlanGeometryPaths));

    public static readonly StyledProperty<IReadOnlyList<SitePlanRenderPathDto>?> SitePlanRenderPathsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<SitePlanRenderPathDto>?>(nameof(SitePlanRenderPaths));

    public static readonly StyledProperty<IReadOnlyList<SitePlanTextDto>?> SitePlanTextsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<SitePlanTextDto>?>(nameof(SitePlanTexts));

    public static readonly StyledProperty<IReadOnlyList<GeometryPathDto>?> GeometryPathsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<GeometryPathDto>?>(nameof(GeometryPaths));

    public static readonly StyledProperty<Guid?> HighlightGeometryPathIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(HighlightGeometryPathId));

    public static readonly StyledProperty<IReadOnlyList<PinchMarkerDto>?> PinchMarkersProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<PinchMarkerDto>?>(nameof(PinchMarkers));

    public static readonly StyledProperty<IReadOnlyList<RoomLabelDto>?> RoomLabelsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<RoomLabelDto>?>(nameof(RoomLabels));

    public static readonly StyledProperty<Guid?> HighlightRoomLabelIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(HighlightRoomLabelId));

    public static readonly StyledProperty<IReadOnlyList<WallCandidateDto>?> WallCandidatesProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<WallCandidateDto>?>(nameof(WallCandidates));

    public static readonly StyledProperty<IReadOnlyList<OpeningCandidateDto>?> OpeningCandidatesProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<OpeningCandidateDto>?>(nameof(OpeningCandidates));

    public static readonly StyledProperty<IReadOnlyList<OpeningLabelDto>?> OpeningLabelsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<OpeningLabelDto>?>(nameof(OpeningLabels));

    public static readonly StyledProperty<Guid?> HighlightOpeningLabelIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(HighlightOpeningLabelId));

    public static readonly StyledProperty<IReadOnlyList<DimensionDto>?> DimensionsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<DimensionDto>?>(nameof(Dimensions));

    public static readonly StyledProperty<IReadOnlyList<Guid>?> ChangedNumberDimensionIdsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<Guid>?>(nameof(ChangedNumberDimensionIds));

    public static readonly StyledProperty<MeasurementContextDto?> MeasurementContextProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, MeasurementContextDto?>(nameof(MeasurementContext));

    public static readonly StyledProperty<IReadOnlyList<DimensionBindingDto>?> DimensionBindingsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<DimensionBindingDto>?>(nameof(DimensionBindings));

    public static readonly StyledProperty<IReadOnlyList<DimensionAssociationDto>?> DimensionAssociationsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<DimensionAssociationDto>?>(nameof(DimensionAssociations));

    public static readonly StyledProperty<IReadOnlyList<MeasurementCorridorDto>?> MeasurementCorridorsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<MeasurementCorridorDto>?>(nameof(MeasurementCorridors));

    public static readonly StyledProperty<IReadOnlyList<MeasurementNodeDto>?> MeasurementNodesProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<MeasurementNodeDto>?>(nameof(MeasurementNodes));

    public static readonly StyledProperty<IReadOnlyList<DimensionIntervalBindingDto>?> DimensionIntervalBindingsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<DimensionIntervalBindingDto>?>(nameof(DimensionIntervalBindings));

    public static readonly StyledProperty<IReadOnlyList<ArticulationBandDto>?> ArticulationBandsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<ArticulationBandDto>?>(nameof(ArticulationBands));

    public static readonly StyledProperty<Guid?> SelectedMeasurementCorridorIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(SelectedMeasurementCorridorId));

    public static readonly StyledProperty<Guid?> SelectedMeasurementNodeIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(SelectedMeasurementNodeId));

    public static readonly StyledProperty<Guid?> SelectedMeasurementStartNodeIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(SelectedMeasurementStartNodeId));

    public static readonly StyledProperty<Guid?> SelectedMeasurementEndNodeIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(SelectedMeasurementEndNodeId));

    public static readonly StyledProperty<Guid?> HighlightDimensionIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(HighlightDimensionId));

    public static readonly StyledProperty<bool> AreDimensionsVisibleProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, bool>(nameof(AreDimensionsVisible), defaultValue: true);

    public static readonly StyledProperty<IReadOnlyList<FixedPlanComponentDto>?> FixedPlanComponentsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<FixedPlanComponentDto>?>(nameof(FixedPlanComponents));

    public static readonly StyledProperty<IReadOnlyList<ProtectedDetailAssemblyDto>?> ProtectedDetailAssembliesProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<ProtectedDetailAssemblyDto>?>(nameof(ProtectedDetailAssemblies));

    public static readonly StyledProperty<IReadOnlyList<CuratedPlanArtifactDto>?> CuratedPlanArtifactsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<CuratedPlanArtifactDto>?>(nameof(CuratedPlanArtifacts));

    public static readonly StyledProperty<Guid?> PreviewPinchGroupIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(PreviewPinchGroupId));

    public static readonly StyledProperty<string?> PreviewAxisTagProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, string?>(nameof(PreviewAxisTag));

    public static readonly StyledProperty<bool> IsPinchPlacementArmedProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, bool>(nameof(IsPinchPlacementArmed));

    public static readonly StyledProperty<bool> IsFloorPlanMoveToolActiveProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, bool>(nameof(IsFloorPlanMoveToolActive));

    public IReadOnlyList<GeometryPathDto>? SitePlanGeometryPaths
    {
        get => GetValue(SitePlanGeometryPathsProperty);
        set => SetValue(SitePlanGeometryPathsProperty, value);
    }

    public IReadOnlyList<SitePlanRenderPathDto>? SitePlanRenderPaths
    {
        get => GetValue(SitePlanRenderPathsProperty);
        set => SetValue(SitePlanRenderPathsProperty, value);
    }

    public IReadOnlyList<SitePlanTextDto>? SitePlanTexts
    {
        get => GetValue(SitePlanTextsProperty);
        set => SetValue(SitePlanTextsProperty, value);
    }

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

    public Guid? HighlightRoomLabelId
    {
        get => GetValue(HighlightRoomLabelIdProperty);
        set => SetValue(HighlightRoomLabelIdProperty, value);
    }

    public IReadOnlyList<WallCandidateDto>? WallCandidates
    {
        get => GetValue(WallCandidatesProperty);
        set => SetValue(WallCandidatesProperty, value);
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

    public Guid? HighlightOpeningLabelId
    {
        get => GetValue(HighlightOpeningLabelIdProperty);
        set => SetValue(HighlightOpeningLabelIdProperty, value);
    }

    public IReadOnlyList<DimensionDto>? Dimensions
    {
        get => GetValue(DimensionsProperty);
        set => SetValue(DimensionsProperty, value);
    }

    public IReadOnlyList<Guid>? ChangedNumberDimensionIds
    {
        get => GetValue(ChangedNumberDimensionIdsProperty);
        set => SetValue(ChangedNumberDimensionIdsProperty, value);
    }

    public MeasurementContextDto? MeasurementContext
    {
        get => GetValue(MeasurementContextProperty);
        set => SetValue(MeasurementContextProperty, value);
    }

    public IReadOnlyList<DimensionBindingDto>? DimensionBindings
    {
        get => GetValue(DimensionBindingsProperty);
        set => SetValue(DimensionBindingsProperty, value);
    }

    public IReadOnlyList<DimensionAssociationDto>? DimensionAssociations
    {
        get => GetValue(DimensionAssociationsProperty);
        set => SetValue(DimensionAssociationsProperty, value);
    }

    public IReadOnlyList<MeasurementCorridorDto>? MeasurementCorridors
    {
        get => GetValue(MeasurementCorridorsProperty);
        set => SetValue(MeasurementCorridorsProperty, value);
    }

    public IReadOnlyList<MeasurementNodeDto>? MeasurementNodes
    {
        get => GetValue(MeasurementNodesProperty);
        set => SetValue(MeasurementNodesProperty, value);
    }

    public IReadOnlyList<DimensionIntervalBindingDto>? DimensionIntervalBindings
    {
        get => GetValue(DimensionIntervalBindingsProperty);
        set => SetValue(DimensionIntervalBindingsProperty, value);
    }

    public IReadOnlyList<ArticulationBandDto>? ArticulationBands
    {
        get => GetValue(ArticulationBandsProperty);
        set => SetValue(ArticulationBandsProperty, value);
    }

    public Guid? SelectedMeasurementCorridorId
    {
        get => GetValue(SelectedMeasurementCorridorIdProperty);
        set => SetValue(SelectedMeasurementCorridorIdProperty, value);
    }

    public Guid? SelectedMeasurementNodeId
    {
        get => GetValue(SelectedMeasurementNodeIdProperty);
        set => SetValue(SelectedMeasurementNodeIdProperty, value);
    }

    public Guid? SelectedMeasurementStartNodeId
    {
        get => GetValue(SelectedMeasurementStartNodeIdProperty);
        set => SetValue(SelectedMeasurementStartNodeIdProperty, value);
    }

    public Guid? SelectedMeasurementEndNodeId
    {
        get => GetValue(SelectedMeasurementEndNodeIdProperty);
        set => SetValue(SelectedMeasurementEndNodeIdProperty, value);
    }

    public Guid? HighlightDimensionId
    {
        get => GetValue(HighlightDimensionIdProperty);
        set => SetValue(HighlightDimensionIdProperty, value);
    }

    public bool AreDimensionsVisible
    {
        get => GetValue(AreDimensionsVisibleProperty);
        set => SetValue(AreDimensionsVisibleProperty, value);
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

    public IReadOnlyList<CuratedPlanArtifactDto>? CuratedPlanArtifacts
    {
        get => GetValue(CuratedPlanArtifactsProperty);
        set => SetValue(CuratedPlanArtifactsProperty, value);
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

    public bool IsFloorPlanMoveToolActive
    {
        get => GetValue(IsFloorPlanMoveToolActiveProperty);
        set => SetValue(IsFloorPlanMoveToolActiveProperty, value);
    }

    public event EventHandler<GeometryPathClickedEventArgs>? GeometryPathClicked;
    public event EventHandler<RoomLabelClickedEventArgs>? RoomLabelClicked;
    public event EventHandler<OpeningLabelClickedEventArgs>? OpeningLabelClicked;
    public event EventHandler<MovableArtifactMovedEventArgs>? MovableArtifactMoved;
    public event EventHandler<DimensionClickedEventArgs>? DimensionClicked;
    public event EventHandler<DimensionEditedEventArgs>? DimensionEdited;
    public event EventHandler<FloorPlanMoveDeltaEventArgs>? FloorPlanMoveDeltaRequested;

    internal static Rect GetLocalRenderBounds(Rect layoutBounds)
    {
        return new Rect(0d, 0d, Math.Max(0d, layoutBounds.Width), Math.Max(0d, layoutBounds.Height));
    }

    internal static (decimal X, decimal Y) ApplyAbsolutePointDelta(decimal baseX, decimal baseY, decimal deltaX, decimal deltaY)
    {
        return (RoundModelValue(baseX + deltaX), RoundModelValue(baseY + deltaY));
    }

    internal static (decimal Dx, decimal Dy) ApplyTranslationDelta(decimal baseDx, decimal baseDy, decimal deltaX, decimal deltaY)
    {
        return (RoundModelValue(baseDx + deltaX), RoundModelValue(baseDy + deltaY));
    }

    internal static FloorPlanMoveDelta CalculateFloorPlanMoveDelta(
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point previousPointerPosition,
        Point currentPointerPosition)
    {
        if (viewport.Scale <= double.Epsilon)
        {
            return new FloorPlanMoveDelta(0m, 0m);
        }

        var deltaX = (decimal)((currentPointerPosition.X - previousPointerPosition.X) / viewport.Scale);
        var deltaY = (decimal)((previousPointerPosition.Y - currentPointerPosition.Y) / viewport.Scale);

        return new FloorPlanMoveDelta(RoundModelValue(deltaX), RoundModelValue(deltaY));
    }

    internal static DimensionDto ApplyDimensionHandleDelta(
        DimensionDto dimension,
        DimensionHandleKind handleKind,
        decimal deltaX,
        decimal deltaY)
    {
        return NativeDimensionEditor.ApplyHandleDelta(dimension, handleKind, deltaX, deltaY);
    }

    internal static DimensionHit? TryResolveDimensionHit(
        IReadOnlyList<DimensionDto>? dimensions,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point pointerPosition,
        double hitTolerancePixels)
    {
        return NativeDimensionHitTester.TryResolveDimensionHit(
            dimensions,
            viewport,
            pointerPosition,
            hitTolerancePixels,
            out var dimension,
            out var suggestedHandle,
            out var hitArea,
            out var referenceWorldPoint)
            ? new DimensionHit(dimension, suggestedHandle, hitArea, referenceWorldPoint)
            : null;
    }

    internal static bool CanResolveDimensionInteractions(bool areDimensionsVisible, bool isPinchPlacementArmed)
        => areDimensionsVisible && !isPinchPlacementArmed;

    internal static Point ResolveSnappedWorldPoint(
        Point worldPoint,
        CadViewportContext viewportContext,
        IReadOnlyList<Point> anchorPoints,
        bool enableGridSnap)
    {
        return NativeDimensionEditor.ResolveSnappedWorldPoint(worldPoint, viewportContext, anchorPoints, enableGridSnap);
    }

    internal static IReadOnlyList<GeometryPathDto> BuildHitTestGeometry(
        IReadOnlyList<GeometryPathDto>? geometryPaths,
        IReadOnlyList<OpeningCandidateDto>? openings = null,
        IReadOnlyList<FixedPlanComponentDto>? fixedPlanComponents = null,
        IReadOnlyList<ProtectedDetailAssemblyDto>? protectedDetailAssemblies = null,
        IReadOnlyList<CuratedPlanArtifactDto>? curatedArtifacts = null)
    {
        if (geometryPaths is not { Count: > 0 })
        {
            return [];
        }

        if (curatedArtifacts is { Count: > 0 })
        {
            return PreviewArtifactGeometryIndex
                .Create(curatedArtifacts)
                .OrderForHitTesting(geometryPaths);
        }

        return PreviewArtifactGeometryIndex
            .Create(openings, fixedPlanComponents, protectedDetailAssemblies)
            .OrderForHitTesting(geometryPaths);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        collectionObserverHub.AttachAll(CaptureObservedCollections());
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        collectionObserverHub.DetachAll();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var pointerProperties = e.GetCurrentPoint(this).Properties;
        if (pointerProperties.IsMiddleButtonPressed)
        {
            var panStart = PreviewInteractionCoordinator.HandleMiddleButtonPressed(
                PreviewInteractionCoordinator.CreatePointerPressedRequestForPan(
                    e.GetPosition(this),
                    previewZoomState));
            isPanningPreview = panStart.IsPanningPreview;
            panStartPoint = panStart.PanStartPoint;
            panStartZoomState = panStart.PanStartZoomState;
            activeDragEdge = null;
            activePreviewTrimSourceUnits = 0m;
            if (panStart.CapturePointer)
            {
                e.Pointer.Capture(this);
            }

            e.Handled = panStart.Handled;
            return;
        }

        if (!pointerProperties.IsLeftButtonPressed)
        {
            return;
        }

        var pointerPosition = e.GetPosition(this);
        var axisTag = ParseAxisTag();
        var viewport = GetPreviewViewport(axisTag);
        if (viewport is null)
        {
            return;
        }

        if (IsFloorPlanMoveToolActive)
        {
            activeFloorPlanMove = new FloorPlanMoveDragState(viewport.Value, pointerPosition);
            activeDragEdge = null;
            activeArtifactMove = null;
            activePreviewTrimSourceUnits = 0m;
            activeDimensionEdit = null;
            pendingDimensionEdit = null;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        var pressOutcome = PreviewInteractionCoordinator.HandleLeftButtonPressed(
            new PreviewInteractionCoordinator.LeftButtonPressRequest(
                pointerPosition,
                axisTag,
                IsPinchPlacementArmed,
                ResolveEdgeDrag,
                point => CanResolveDimensionInteractions(AreDimensionsVisible, IsPinchPlacementArmed) &&
                         TryResolveDimensionHandleHit(Dimensions, HighlightDimensionId, viewport.Value, point, out var handleHit)
                    ? handleHit
                    : null,
                point => CanResolveDimensionInteractions(AreDimensionsVisible, IsPinchPlacementArmed)
                    ? TryResolveDimensionHit(Dimensions, viewport.Value, point, HitTestTolerance)
                    : null,
                point => TryResolveRoomLabelHit(viewport.Value, point, out var roomLabel)
                    ? roomLabel
                    : null,
                point => TryResolveOpeningLabelHit(viewport.Value, point, out var openingLabel)
                    ? openingLabel
                    : null,
                point =>
                {
                    var hitTestGeometry = BuildHitTestGeometry(
                        GeometryPaths,
                        OpeningCandidates,
                        FixedPlanComponents,
                        ProtectedDetailAssemblies,
                        CuratedPlanArtifacts);
                    var hit = FloorPlanPreviewGeometry.HitTestPathDetail(hitTestGeometry, viewport.Value, point, HitTestTolerance);
                    return hit is null
                        ? null
                        : new PreviewInteractionCoordinator.GeometryHit(hit.Value.GeometryPathId, hit.Value.PositionRatio);
                },
                geometryPathId => TryResolveMovableArtifact(geometryPathId, out var movableArtifact)
                    ? movableArtifact
                    : null));

        if (!pressOutcome.Handled)
        {
            return;
        }

        if (pressOutcome.DimensionClickedId is { } dimensionId)
        {
            DimensionClicked?.Invoke(this, new DimensionClickedEventArgs(dimensionId));
        }

        if (pressOutcome.RoomLabelClickedId is { } roomLabelId)
        {
            RoomLabelClicked?.Invoke(this, new RoomLabelClickedEventArgs(roomLabelId));
        }

        if (pressOutcome.OpeningLabelClickedId is { } openingLabelId)
        {
            OpeningLabelClicked?.Invoke(this, new OpeningLabelClickedEventArgs(openingLabelId));
        }

        if (pressOutcome.GeometryClick is { } geometryClick)
        {
            GeometryPathClicked?.Invoke(this, new GeometryPathClickedEventArgs(geometryClick.GeometryPathId, geometryClick.PositionRatio));
        }

        if (pressOutcome.StartedEdgeDrag is null &&
            pressOutcome.StartedDimensionEdit is null &&
            pressOutcome.PendingDimensionEdit is null &&
            pressOutcome.StartedArtifactMove is null)
        {
            pendingDimensionEdit = null;
            activeDimensionEdit = null;
        }

        if (pressOutcome.StartedEdgeDrag is { } edge)
        {
            activeDragEdge = edge;
            dragStartPoint = pointerPosition;
            activePreviewTrimSourceUnits = 0m;
            pendingDimensionEdit = null;
        }

        if (pressOutcome.StartedDimensionEdit is { } dimensionEdit)
        {
            activeDimensionEdit = dimensionEdit;
            pendingDimensionEdit = null;
        }

        if (pressOutcome.PendingDimensionEdit is { } pendingEdit)
        {
            pendingDimensionEdit = pendingEdit;
            activeDimensionEdit = null;
        }

        if (pressOutcome.StartedArtifactMove is { } artifactMove)
        {
            activeArtifactMove = artifactMove;
            pendingDimensionEdit = null;
        }

        if (pressOutcome.CapturePointer)
        {
            e.Pointer.Capture(this);
        }

        if (pressOutcome.InvalidateVisual)
        {
            InvalidateVisual();
        }

        e.Handled = pressOutcome.Handled;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var axisTag = ParseAxisTag();
        var viewport = GetPreviewViewport(axisTag);
        var pointerPosition = e.GetPosition(this);

        if (activeFloorPlanMove is { } floorPlanMove)
        {
            var delta = CalculateFloorPlanMoveDelta(
                floorPlanMove.Viewport,
                floorPlanMove.PreviousPointerPosition,
                pointerPosition);

            activeFloorPlanMove = floorPlanMove with { PreviousPointerPosition = pointerPosition };
            if (delta.DeltaX != 0m || delta.DeltaY != 0m)
            {
                FloorPlanMoveDeltaRequested?.Invoke(this, new FloorPlanMoveDeltaEventArgs(delta.DeltaX, delta.DeltaY));
            }

            e.Handled = true;
            return;
        }

        Func<PreviewDimensionEditState, Point, Point>? resolveSnappedDimensionWorldPoint = null;
        if (viewport is { } activeViewport)
        {
            resolveSnappedDimensionWorldPoint = (edit, currentPointerPosition)
                => ResolveSnappedDimensionEditWorldPoint(edit, activeViewport, currentPointerPosition);
        }

        var moveOutcome = PreviewInteractionCoordinator.HandlePointerMoved(
            new PreviewInteractionCoordinator.PointerMovedRequest(
                CurrentState: CaptureInteractionState(),
                PointerPosition: pointerPosition,
                Viewport: viewport,
                ResolveSnappedDimensionWorldPoint: resolveSnappedDimensionWorldPoint));

        ApplyInteractionState(moveOutcome.NextState);
        if (moveOutcome.InvalidateVisual)
        {
            InvalidateVisual();
        }

        if (moveOutcome.Handled)
        {
            e.Handled = true;
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var viewportGeometry = BuildViewportGeometry(axisTag: null);
        if (viewportGeometry.Count == 0)
        {
            return;
        }

        var axisTag = ParseAxisTag();
        viewportGeometry = BuildViewportGeometry(axisTag);
        var baseViewport = FloorPlanPreviewGeometry.CalculateViewport(viewportGeometry, GetGeometryViewportBounds(axisTag), PreviewPadding);
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

        if (activeFloorPlanMove is not null)
        {
            activeFloorPlanMove = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        var releaseOutcome = PreviewInteractionCoordinator.HandlePointerReleased(
            new PreviewInteractionCoordinator.PointerReleasedRequest(
                CurrentState: CaptureInteractionState(),
                PointerPosition: e.GetPosition(this),
                Viewport: GetPreviewViewport(ParseAxisTag()),
                BuildDimensionEditedEventArgs: BuildDimensionEditedEventArgs));

        ApplyInteractionState(releaseOutcome.NextState);
        if (releaseOutcome.ReleasePointerCapture)
        {
            e.Pointer.Capture(null);
        }

        if (releaseOutcome.Handled)
        {
            e.Handled = true;
        }

        if (releaseOutcome.InvalidateVisual)
        {
            InvalidateVisual();
        }

        if (releaseOutcome.CommittedArtifactMove is { } movement)
        {
            MovableArtifactMoved?.Invoke(this, movement);
        }

        if (releaseOutcome.CommittedDimensionEdit is { } editedDimension)
        {
            DimensionEdited?.Invoke(this, editedDimension);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = GetLocalRenderBounds(Bounds);
        var axisTag = ParseAxisTag();
        var viewport = GetPreviewViewport(axisTag);
        var scene = BuildRenderScene(bounds, axisTag, viewport);

        using var previewClip = context.PushClip(bounds);
        PreviewRenderComposer.Render(context, scene);
    }

    internal static double CalculateWheelZoomFactor(double currentZoomFactor, double wheelDeltaY)
        => PreviewInteractionCoordinator.CalculateWheelZoomFactor(currentZoomFactor, wheelDeltaY);

    internal static PreviewZoomState ResolveZoomStateForWheel(
        FloorPlanPreviewGeometry.PreviewViewport baseViewport,
        PreviewZoomState currentState,
        Point pointerPosition,
        double wheelDeltaY)
        => PreviewInteractionCoordinator.ResolveZoomStateForWheel(baseViewport, currentState, pointerPosition, wheelDeltaY);

    internal static PreviewZoomState ResolvePanStateForDrag(
        PreviewZoomState startState,
        Point startPointerPosition,
        Point currentPointerPosition)
        => PreviewInteractionCoordinator.ResolvePanStateForDrag(startState, startPointerPosition, currentPointerPosition);

    internal static PreviewZoomState PreserveZoomStateForBaseViewportChange(
        FloorPlanPreviewGeometry.PreviewViewport oldBaseViewport,
        FloorPlanPreviewGeometry.PreviewViewport newBaseViewport,
        PreviewZoomState currentState,
        Point anchorScreenPoint)
    {
        var safeZoom = Math.Clamp(
            double.IsFinite(currentState.ZoomFactor) ? currentState.ZoomFactor : 1d,
            MinimumUserZoomFactor,
            MaximumUserZoomFactor);
        var oldViewport = oldBaseViewport.WithUserTransform(safeZoom, currentState.PanOffset);
        var anchorWorldPoint = oldViewport.Unproject(anchorScreenPoint);
        var newViewportWithoutPan = newBaseViewport.WithUserTransform(safeZoom, default);
        var projectedWithoutPan = newViewportWithoutPan.Project(anchorWorldPoint.X, anchorWorldPoint.Y);

        return new PreviewZoomState(
            safeZoom,
            new Vector(
                anchorScreenPoint.X - projectedWithoutPan.X,
                anchorScreenPoint.Y - projectedWithoutPan.Y));
    }

    internal static double CalculateChangePreviewGhostOpacity(TimeSpan elapsed, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return 0d;
        }

        var progress = Math.Clamp(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 0d, 1d);
        return Math.Round(1d - progress, 6, MidpointRounding.AwayFromZero);
    }

    internal static bool ShouldStartChangePreviewAnimation(
        PreviewCollectionObserverHub.PreviewObservedCollectionSlot slot,
        bool hasActiveFloorPlanMove)
        => slot == PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths &&
           !hasActiveFloorPlanMove;

    private IReadOnlyList<GeometryPathDto> BuildPreviewGeometry(PinchAxisTag? axisTag)
    {
        return BuildPreviewGeometry(axisTag, GeometryPaths);
    }

    private IReadOnlyList<GeometryPathDto> BuildPreviewGeometry(
        PinchAxisTag? axisTag,
        IReadOnlyList<GeometryPathDto>? geometryPaths)
    {
        if (axisTag is null || activeDragEdge is null)
        {
            return geometryPaths ?? [];
        }

        return FloorPlanPreviewGeometry.CreatePreviewGeometry(
            geometryPaths,
            axisTag.Value,
            PinchMarkerPreviewLayerRenderer.FilterForPreviewGroup(PinchMarkers, PreviewPinchGroupId),
            activePreviewTrimSourceUnits,
            activeDragEdge.Value,
            MeasurementContext?.ToMillimetersFactor ?? 1m);
    }

    private IReadOnlyList<GeometryPathDto> BuildViewportGeometry(
        PinchAxisTag? axisTag,
        IReadOnlyList<GeometryPathDto>? geometryPaths = null)
    {
        var previewGeometry = geometryPaths is null
            ? BuildPreviewGeometry(axisTag)
            : BuildPreviewGeometry(axisTag, geometryPaths);
        if (SitePlanGeometryPaths is not { Count: > 0 })
        {
            return previewGeometry;
        }

        return SitePlanGeometryPaths
            .Concat(previewGeometry)
            .ToArray();
    }

    private FloorPlanPreviewGeometry.PreviewCompressionEdge? ResolveEdgeDrag(Point pointerPosition, PinchAxisTag axisTag)
    {
        foreach (var handle in CompressionHandlePreviewLayerRenderer.GetVisibleHandles(
                     GetLocalRenderBounds(Bounds),
                     axisTag,
                     IsPinchPlacementArmed,
                     PinchMarkers,
                     PreviewPinchGroupId))
        {
            if (handle.Rect.Contains(pointerPosition))
            {
                return handle.Edge;
            }
        }

        return null;
    }

    private IReadOnlyList<GeometryPathDto> ApplyActiveArtifactMoveToGeometry(IReadOnlyList<GeometryPathDto> geometryPaths)
    {
        if (activeArtifactMove is not { PositionMode: FloorPlanArtifactPositionMode.Translation } move ||
            geometryPaths.Count == 0)
        {
            return geometryPaths;
        }

        var pathIds = ResolveGeometryPathIds(move.SourceArtifactKind, move.SourceArtifactId);
        if (pathIds.Count == 0)
        {
            return geometryPaths;
        }

        return geometryPaths
            .Select(path =>
            {
                if (!pathIds.Contains(path.Id))
                {
                    return path;
                }

                return new GeometryPathDto(
                    path.Id,
                    path.IsClosed,
                    path.Segments
                        .Select(segment => new GeometrySegmentDto(
                            segment.GeometryPathId,
                            segment.SortOrder,
                            segment.StartX + move.CurrentDeltaX,
                            segment.StartY + move.CurrentDeltaY,
                            segment.EndX + move.CurrentDeltaX,
                            segment.EndY + move.CurrentDeltaY))
                        .ToArray());
            })
            .ToArray();
    }

    private IReadOnlyList<RoomLabelDto> BuildRenderedRoomLabels()
    {
        if (RoomLabels is not { Count: > 0 } ||
            activeArtifactMove is not { PositionMode: FloorPlanArtifactPositionMode.AbsolutePoint } move ||
            !string.Equals(move.SourceArtifactKind, FloorPlanArtifactPositionSourceKinds.RoomLabel, StringComparison.Ordinal))
        {
            return RoomLabels ?? [];
        }

        var movedPoint = ApplyAbsolutePointDelta(move.BaseX, move.BaseY, move.CurrentDeltaX, move.CurrentDeltaY);
        return RoomLabels
            .Select(label => label.RoomLabelId == move.SourceArtifactId
                ? label with { X = movedPoint.X, Y = movedPoint.Y }
                : label)
            .ToArray();
    }

    private IReadOnlyList<OpeningLabelDto> BuildRenderedOpeningLabels()
    {
        if (OpeningLabels is not { Count: > 0 } ||
            activeArtifactMove is not { PositionMode: FloorPlanArtifactPositionMode.AbsolutePoint } move ||
            !string.Equals(move.SourceArtifactKind, FloorPlanArtifactPositionSourceKinds.OpeningLabel, StringComparison.Ordinal))
        {
            return OpeningLabels ?? [];
        }

        var movedPoint = ApplyAbsolutePointDelta(move.BaseX, move.BaseY, move.CurrentDeltaX, move.CurrentDeltaY);
        return OpeningLabels
            .Select(label => label.OpeningLabelId == move.SourceArtifactId
                ? label with { X = movedPoint.X, Y = movedPoint.Y }
                : label)
            .ToArray();
    }

    internal static IReadOnlyList<DimensionDto> BuildRenderedDimensionsForPreview(
        IReadOnlyList<DimensionDto>? dimensions,
        IReadOnlyList<DimensionAssociationDto>? dimensionAssociations,
        IReadOnlyList<DimensionBindingDto>? dimensionBindings,
        IReadOnlyList<GeometryPathDto>? previewGeometry,
        IReadOnlyList<WallCandidateDto>? wallCandidates,
        IReadOnlyList<OpeningCandidateDto>? openingCandidates,
        MeasurementContextDto? measurementContext,
        bool useReactivePreview,
        DimensionPreviewProjector.DimensionPreviewEditRequest? activeEdit,
        IReadOnlyList<MeasurementCorridorDto>? measurementCorridors = null,
        IReadOnlyList<MeasurementNodeDto>? measurementNodes = null,
        IReadOnlyList<DimensionIntervalBindingDto>? dimensionIntervalBindings = null,
        IReadOnlyList<ArticulationBandDto>? articulationBands = null,
        Guid? previewPinchGroupId = null,
        IReadOnlyList<GeometryPathDto>? sourceGeometry = null)
    {
        var renderedDimensions = useReactivePreview &&
                                 dimensions is { Count: > 0 } &&
                                 previewGeometry is { Count: > 0 } &&
                                 measurementCorridors is { Count: > 0 } &&
                                 measurementNodes is { Count: > 0 } &&
                                 dimensionIntervalBindings is { Count: > 0 } &&
                                 articulationBands is { Count: > 0 } &&
                                 previewPinchGroupId is not null
            ? DimensionIntervalReactiveProjector.Project(
                dimensions,
                previewGeometry,
                measurementCorridors,
                measurementNodes,
                dimensionIntervalBindings,
                articulationBands,
                previewPinchGroupId,
                sourceGeometry)
            : dimensions ?? [];

        return DimensionPreviewProjector.BuildRenderedDimensions(
            renderedDimensions,
            activeEdit,
            reactiveInputs: null);
    }

    private IReadOnlyList<DimensionDto> BuildRenderedDimensions(
        IReadOnlyList<GeometryPathDto> previewGeometry,
        PinchAxisTag? axisTag)
    {
        return BuildRenderedDimensionsForPreview(
            Dimensions,
            DimensionAssociations,
            DimensionBindings,
            previewGeometry,
            WallCandidates,
            OpeningCandidates,
            MeasurementContext,
            useReactivePreview: axisTag is not null && activeDragEdge is not null,
            activeEdit: activeDimensionEdit is { } edit
                ? new DimensionPreviewProjector.DimensionPreviewEditRequest(
                    edit.BaseDimension.DimensionId,
                    edit.BaseDimension,
                    edit.HandleKind,
                    edit.ReferenceWorldPoint,
                    edit.CurrentWorldPoint)
                : null,
            measurementCorridors: MeasurementCorridors,
            measurementNodes: MeasurementNodes,
            dimensionIntervalBindings: DimensionIntervalBindings,
            articulationBands: ArticulationBands,
            previewPinchGroupId: PreviewPinchGroupId,
            sourceGeometry: GeometryPaths);
    }

    private PreviewRenderScene BuildRenderScene(
        Rect bounds,
        PinchAxisTag? axisTag,
        FloorPlanPreviewGeometry.PreviewViewport? viewport)
    {
        var previewGeometry = BuildPreviewGeometry(axisTag);
        previewGeometry = ApplyActiveArtifactMoveToGeometry(previewGeometry);
        var roomLabels = BuildRenderedRoomLabels();
        var openingLabels = BuildRenderedOpeningLabels();
        var dimensions = BuildRenderedDimensions(previewGeometry, axisTag);
        var artifactIndex = CuratedPlanArtifacts is { Count: > 0 }
            ? PreviewArtifactGeometryIndex.Create(CuratedPlanArtifacts)
            : PreviewArtifactGeometryIndex.Create(OpeningCandidates, FixedPlanComponents, ProtectedDetailAssemblies);
        var ghostOpacity = ResolveChangePreviewGhostOpacity(DateTimeOffset.UtcNow);
        lastRenderedPreviewGeometry = previewGeometry.ToArray();

        return new PreviewRenderScene(
            Bounds: bounds,
            AxisTag: axisTag,
            IsPinchPlacementArmed: IsPinchPlacementArmed,
            Viewport: viewport,
            SitePlanGeometry: SitePlanGeometryPaths ?? [],
            SitePlanRenderPaths: SitePlanRenderPaths ?? [],
            SitePlanTexts: SitePlanTexts ?? [],
            PreviewGeometry: previewGeometry,
            RoomLabels: roomLabels,
            OpeningLabels: openingLabels,
            Dimensions: dimensions,
            ChangedNumberDimensionIds: ChangedNumberDimensionIds ?? [],
            AreDimensionsVisible: AreDimensionsVisible,
            ArtifactIndex: artifactIndex,
            OpeningCandidates: OpeningCandidates,
            FixedPlanComponents: FixedPlanComponents,
            ProtectedDetailAssemblies: ProtectedDetailAssemblies,
            CuratedPlanArtifacts: CuratedPlanArtifacts,
            PinchMarkers: PinchMarkers,
            MeasurementCorridors: MeasurementCorridors,
            MeasurementNodes: MeasurementNodes,
            DimensionIntervalBindings: DimensionIntervalBindings,
            ArticulationBands: ArticulationBands,
            SelectedMeasurementCorridorId: SelectedMeasurementCorridorId,
            SelectedMeasurementNodeId: SelectedMeasurementNodeId,
            SelectedMeasurementStartNodeId: SelectedMeasurementStartNodeId,
            SelectedMeasurementEndNodeId: SelectedMeasurementEndNodeId,
            HighlightGeometryPathId: HighlightGeometryPathId,
            HighlightRoomLabelId: HighlightRoomLabelId,
            HighlightOpeningLabelId: HighlightOpeningLabelId,
            HighlightDimensionId: HighlightDimensionId,
            PreviewPinchGroupId: PreviewPinchGroupId,
            PreviewAxisTag: PreviewAxisTag,
            ActiveDimensionHandleKind: activeDimensionEdit?.HandleKind,
            ChangePreviewGhostGeometry: changePreviewGhostGeometry,
            ChangePreviewGhostOpacity: ghostOpacity);
    }

    private DimensionDto? BuildEditedDimensionPreview(PreviewDimensionEditState edit, DimensionDto baseDimension)
    {
        var rendered = DimensionPreviewProjector.BuildRenderedDimensions(
            [baseDimension],
            new DimensionPreviewProjector.DimensionPreviewEditRequest(
                edit.BaseDimension.DimensionId,
                edit.BaseDimension,
                edit.HandleKind,
                edit.ReferenceWorldPoint,
                edit.CurrentWorldPoint));

        return rendered.FirstOrDefault();
    }

    private PreviewInteractionCoordinator.InteractionState CaptureInteractionState()
        => new(
            previewZoomState,
            isPanningPreview,
            panStartPoint,
            panStartZoomState,
            activeDragEdge,
            dragStartPoint,
            activePreviewTrimSourceUnits,
            activeArtifactMove,
            pendingDimensionEdit,
            activeDimensionEdit);

    private void ApplyInteractionState(PreviewInteractionCoordinator.InteractionState state)
    {
        previewZoomState = state.PreviewZoomState;
        isPanningPreview = state.IsPanningPreview;
        panStartPoint = state.PanStartPoint;
        panStartZoomState = state.PanStartZoomState;
        activeDragEdge = state.ActiveDragEdge;
        dragStartPoint = state.DragStartPoint;
        activePreviewTrimSourceUnits = state.ActivePreviewTrimSourceUnits;
        activeArtifactMove = state.ActiveArtifactMove;
        pendingDimensionEdit = state.PendingDimensionEdit;
        activeDimensionEdit = state.ActiveDimensionEdit;
    }

    private Point ResolveSnappedDimensionEditWorldPoint(
        PreviewDimensionEditState edit,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point pointerPosition)
    {
        var currentWorld = viewport.Unproject(pointerPosition);
        var viewportContext = CadViewportContext.Create(viewport);
        return ResolveSnappedWorldPoint(
            currentWorld,
            viewportContext,
            ResolveDimensionSnapAnchors(edit.BaseDimension, edit.HandleKind),
            enableGridSnap: true);
    }

    private DimensionEditedEventArgs? BuildDimensionEditedEventArgs(PreviewDimensionEditState edit)
    {
        var editedDimension = BuildEditedDimensionPreview(
            edit,
            Dimensions?.FirstOrDefault(item => item.DimensionId == edit.BaseDimension.DimensionId) ?? edit.BaseDimension);
        return editedDimension is not null && !editedDimension.Equals(edit.BaseDimension)
            ? new DimensionEditedEventArgs(
                edit.BaseDimension.DimensionId,
                ResolveSourceDimensionKey(edit.BaseDimension),
                editedDimension,
                edit.HandleKind)
            : null;
    }

    private Rect GetGeometryViewportBounds(PinchAxisTag? axisTag)
    {
        var bounds = GetLocalRenderBounds(Bounds);

        return axisTag is null
            ? bounds
            : FloorPlanPreviewGeometry.GetGeometryViewportBounds(bounds, axisTag.Value);
    }

    private bool TryResolveRoomLabelHit(
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point pointerPosition,
        out RoomLabelDto roomLabel)
    {
        if (RoomLabels is not { Count: > 0 })
        {
            roomLabel = null!;
            return false;
        }

        foreach (var item in RoomLabels.Reverse())
        {
            if (string.IsNullOrWhiteSpace(item.Text))
            {
                continue;
            }

            if (!CadTextPreviewLayerRenderer.GetRoomLabelBounds(item, viewport).Contains(pointerPosition))
            {
                continue;
            }

            roomLabel = item;
            return true;
        }

        roomLabel = null!;
        return false;
    }

    private bool TryResolveOpeningLabelHit(
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point pointerPosition,
        out OpeningLabelDto openingLabel)
    {
        if (OpeningLabels is not { Count: > 0 })
        {
            openingLabel = null!;
            return false;
        }

        foreach (var item in OpeningLabels.Reverse())
        {
            if (string.IsNullOrWhiteSpace(item.Text))
            {
                continue;
            }

            if (!CadTextPreviewLayerRenderer.GetOpeningLabelBounds(item, viewport).Contains(pointerPosition))
            {
                continue;
            }

            openingLabel = item;
            return true;
        }

        openingLabel = null!;
        return false;
    }

    private static bool TryResolveDimensionHandleHit(
        IReadOnlyList<DimensionDto>? dimensions,
        Guid? highlightedDimensionId,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point pointerPosition,
        out DimensionHandleHit handleHit)
    {
        if (NativeDimensionHitTester.TryResolveHandleHit(
                dimensions,
                highlightedDimensionId,
                viewport,
                pointerPosition,
                DimensionHandleHitTolerance,
                out var dimension,
                out var handleKind,
                out var referenceWorldPoint))
        {
            handleHit = new DimensionHandleHit(dimension, handleKind, referenceWorldPoint);
            return true;
        }

        handleHit = default;
        return false;
    }

    private static IReadOnlyList<Point> ResolveDimensionSnapAnchors(DimensionDto dimension, DimensionHandleKind activeHandleKind)
    {
        return NativeDimensionEditor.ResolveSnapAnchors(dimension, activeHandleKind);
    }

    private FloorPlanPreviewGeometry.PreviewViewport? GetPreviewViewport(PinchAxisTag? axisTag)
    {
        var baseViewport = FloorPlanPreviewGeometry.CalculateViewport(BuildViewportGeometry(axisTag), GetGeometryViewportBounds(axisTag), PreviewPadding);
        if (baseViewport is null)
        {
            return null;
        }

        if (preserveViewportOnNextRender &&
            lastBaseViewport is { } previousBaseViewport &&
            Nullable.Equals(lastBaseViewportAxisTag, axisTag))
        {
            previewZoomState = PreserveZoomStateForBaseViewportChange(
                previousBaseViewport,
                baseViewport.Value,
                previewZoomState,
                GetGeometryViewportBounds(axisTag).Center);
        }

        preserveViewportOnNextRender = false;
        lastBaseViewport = baseViewport.Value;
        lastBaseViewportAxisTag = axisTag;

        return baseViewport.Value.WithUserTransform(previewZoomState.ZoomFactor, previewZoomState.PanOffset);
    }

    private PinchAxisTag? ParseAxisTag()
    {
        return Enum.TryParse<PinchAxisTag>(PreviewAxisTag, out var axisTag)
            ? axisTag
            : null;
    }

    private bool TryResolveMovableArtifact(Guid geometryPathId, out PreviewMovableArtifactDescriptor artifact)
    {
        if (CuratedPlanArtifacts is { Count: > 0 })
        {
            var curated = CuratedPlanArtifacts.FirstOrDefault(item => item.GeometryPathIds.Contains(geometryPathId));
            if (curated is not null)
            {
                artifact = new PreviewMovableArtifactDescriptor(
                    curated.SourceArtifactKind,
                    curated.SourceArtifactId,
                    FloorPlanArtifactPositionMode.Translation,
                    0m,
                    0m,
                    curated.TranslationDx,
                    curated.TranslationDy,
                    curated.GeometryPathIds);
                return true;
            }
        }

        if (OpeningCandidates is { Count: > 0 })
        {
            var opening = OpeningCandidates.FirstOrDefault(item => item.GeometryPathId == geometryPathId);
            if (opening is not null)
            {
                artifact = new PreviewMovableArtifactDescriptor(
                    FloorPlanArtifactSourceKinds.OpeningCandidate,
                    opening.OpeningCandidateId,
                    FloorPlanArtifactPositionMode.Translation,
                    0m,
                    0m,
                    0m,
                    0m,
                    opening.GeometryPathId is Guid pathId ? [pathId] : []);
                return true;
            }
        }

        if (FixedPlanComponents is { Count: > 0 })
        {
            var fixedPlanComponent = FixedPlanComponents.FirstOrDefault(item => item.GeometryPathIds.Contains(geometryPathId));
            if (fixedPlanComponent is not null)
            {
                artifact = new PreviewMovableArtifactDescriptor(
                    FloorPlanArtifactSourceKinds.FixedPlanComponent,
                    fixedPlanComponent.FixedPlanComponentId,
                    FloorPlanArtifactPositionMode.Translation,
                    0m,
                    0m,
                    0m,
                    0m,
                    fixedPlanComponent.GeometryPathIds);
                return true;
            }
        }

        if (ProtectedDetailAssemblies is { Count: > 0 })
        {
            var protectedAssembly = ProtectedDetailAssemblies.FirstOrDefault(item => item.GeometryPathIds.Contains(geometryPathId));
            if (protectedAssembly is not null)
            {
                artifact = new PreviewMovableArtifactDescriptor(
                    FloorPlanArtifactSourceKinds.ProtectedDetailAssembly,
                    protectedAssembly.ProtectedDetailAssemblyId,
                    FloorPlanArtifactPositionMode.Translation,
                    0m,
                    0m,
                    0m,
                    0m,
                    protectedAssembly.GeometryPathIds);
                return true;
            }
        }

        artifact = default;
        return false;
    }

    private IReadOnlySet<Guid> ResolveGeometryPathIds(string sourceArtifactKind, Guid sourceArtifactId)
    {
        if (CuratedPlanArtifacts is { Count: > 0 })
        {
            var curated = CuratedPlanArtifacts.FirstOrDefault(item =>
                item.SourceArtifactId == sourceArtifactId &&
                string.Equals(item.SourceArtifactKind, sourceArtifactKind, StringComparison.Ordinal));
            if (curated is not null)
            {
                return curated.GeometryPathIds.ToHashSet();
            }
        }

        if (string.Equals(sourceArtifactKind, FloorPlanArtifactSourceKinds.OpeningCandidate, StringComparison.Ordinal))
        {
            return OpeningCandidates?
                       .FirstOrDefault(item => item.OpeningCandidateId == sourceArtifactId)?
                       .GeometryPathId is Guid geometryPathId
                ? new HashSet<Guid> { geometryPathId }
                : new HashSet<Guid>();
        }

        if (string.Equals(sourceArtifactKind, FloorPlanArtifactSourceKinds.FixedPlanComponent, StringComparison.Ordinal))
        {
            return FixedPlanComponents?
                       .FirstOrDefault(item => item.FixedPlanComponentId == sourceArtifactId)?
                       .GeometryPathIds
                       .ToHashSet()
                   ?? new HashSet<Guid>();
        }

        if (string.Equals(sourceArtifactKind, FloorPlanArtifactSourceKinds.ProtectedDetailAssembly, StringComparison.Ordinal))
        {
            return ProtectedDetailAssemblies?
                       .FirstOrDefault(item => item.ProtectedDetailAssemblyId == sourceArtifactId)?
                       .GeometryPathIds
                       .ToHashSet()
                   ?? new HashSet<Guid>();
        }

        return new HashSet<Guid>();
    }

    private static string ResolveSourceDimensionKey(DimensionDto dimension)
    {
        return !string.IsNullOrWhiteSpace(dimension.SourceHandle)
            ? dimension.SourceHandle
            : dimension.SourceEntityRef;
    }

    private static decimal RoundModelValue(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero);
    }

    private void OnSitePlanGeometryPathsChanged(IReadOnlyList<GeometryPathDto>? oldValue, IReadOnlyList<GeometryPathDto>? newValue)
    {
        preserveViewportOnNextRender = true;
        ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.SitePlanGeometryPaths, oldValue, newValue);
    }

    private void OnSitePlanRenderPathsChanged(IReadOnlyList<SitePlanRenderPathDto>? oldValue, IReadOnlyList<SitePlanRenderPathDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.SitePlanRenderPaths, oldValue, newValue);

    private void OnSitePlanTextsChanged(IReadOnlyList<SitePlanTextDto>? oldValue, IReadOnlyList<SitePlanTextDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.SitePlanTexts, oldValue, newValue);

    private void OnGeometryPathsChanged(IReadOnlyList<GeometryPathDto>? oldValue, IReadOnlyList<GeometryPathDto>? newValue)
    {
        preserveViewportOnNextRender = true;
        ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths, oldValue, newValue);
    }

    private void OnPinchMarkersChanged(IReadOnlyList<PinchMarkerDto>? oldValue, IReadOnlyList<PinchMarkerDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.PinchMarkers, oldValue, newValue);

    private void OnRoomLabelsChanged(IReadOnlyList<RoomLabelDto>? oldValue, IReadOnlyList<RoomLabelDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.RoomLabels, oldValue, newValue);

    private void OnOpeningCandidatesChanged(IReadOnlyList<OpeningCandidateDto>? oldValue, IReadOnlyList<OpeningCandidateDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.OpeningCandidates, oldValue, newValue);

    private void OnOpeningLabelsChanged(IReadOnlyList<OpeningLabelDto>? oldValue, IReadOnlyList<OpeningLabelDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.OpeningLabels, oldValue, newValue);

    private void OnDimensionsChanged(IReadOnlyList<DimensionDto>? oldValue, IReadOnlyList<DimensionDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.Dimensions, oldValue, newValue);

    private void OnWallCandidatesChanged(IReadOnlyList<WallCandidateDto>? oldValue, IReadOnlyList<WallCandidateDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.WallCandidates, oldValue, newValue);

    private void OnDimensionBindingsChanged(
        IReadOnlyList<DimensionBindingDto>? oldValue,
        IReadOnlyList<DimensionBindingDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.DimensionBindings, oldValue, newValue);

    private void OnDimensionAssociationsChanged(
        IReadOnlyList<DimensionAssociationDto>? oldValue,
        IReadOnlyList<DimensionAssociationDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.DimensionAssociations, oldValue, newValue);

    private void OnMeasurementCorridorsChanged(
        IReadOnlyList<MeasurementCorridorDto>? oldValue,
        IReadOnlyList<MeasurementCorridorDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.MeasurementCorridors, oldValue, newValue);

    private void OnMeasurementNodesChanged(
        IReadOnlyList<MeasurementNodeDto>? oldValue,
        IReadOnlyList<MeasurementNodeDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.MeasurementNodes, oldValue, newValue);

    private void OnDimensionIntervalBindingsChanged(
        IReadOnlyList<DimensionIntervalBindingDto>? oldValue,
        IReadOnlyList<DimensionIntervalBindingDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.DimensionIntervalBindings, oldValue, newValue);

    private void OnArticulationBandsChanged(
        IReadOnlyList<ArticulationBandDto>? oldValue,
        IReadOnlyList<ArticulationBandDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.ArticulationBands, oldValue, newValue);

    private void OnFixedPlanComponentsChanged(IReadOnlyList<FixedPlanComponentDto>? oldValue, IReadOnlyList<FixedPlanComponentDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.FixedPlanComponents, oldValue, newValue);

    private void OnProtectedDetailAssembliesChanged(
        IReadOnlyList<ProtectedDetailAssemblyDto>? oldValue,
        IReadOnlyList<ProtectedDetailAssemblyDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.ProtectedDetailAssemblies, oldValue, newValue);

    private void OnCuratedPlanArtifactsChanged(
        IReadOnlyList<CuratedPlanArtifactDto>? oldValue,
        IReadOnlyList<CuratedPlanArtifactDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.CuratedPlanArtifacts, oldValue, newValue);

    private PreviewCollectionObserverHub.PreviewObservedCollections CaptureObservedCollections()
        => new(
            GeometryPaths,
            PinchMarkers,
            RoomLabels,
            WallCandidates,
            OpeningCandidates,
            OpeningLabels,
            Dimensions,
            DimensionBindings,
            DimensionAssociations,
            MeasurementCorridors,
            MeasurementNodes,
            DimensionIntervalBindings,
            ArticulationBands,
            FixedPlanComponents,
            ProtectedDetailAssemblies,
            CuratedPlanArtifacts,
            SitePlanGeometryPaths,
            SitePlanRenderPaths,
            SitePlanTexts);

    private void ReplaceObservedCollection<T>(
        PreviewCollectionObserverHub.PreviewObservedCollectionSlot slot,
        IReadOnlyList<T>? oldValue,
        IReadOnlyList<T>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            collectionObserverHub.Replace(slot, newValue);
        }

        InvalidateVisual();
    }

    private void OnObservedCollectionChanged(PreviewCollectionObserverHub.PreviewObservedCollectionSlot slot)
    {
        if (slot is PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths or
            PreviewCollectionObserverHub.PreviewObservedCollectionSlot.SitePlanGeometryPaths)
        {
            preserveViewportOnNextRender = true;
        }

        if (ShouldStartChangePreviewAnimation(slot, activeFloorPlanMove is not null))
        {
            StartChangePreviewAnimation();
        }

        InvalidateVisual();
    }

    private double ResolveChangePreviewGhostOpacity(DateTimeOffset now)
    {
        if (changePreviewAnimationStartedAt is not { } startedAt || changePreviewGhostGeometry.Count == 0)
        {
            return 0d;
        }

        return CalculateChangePreviewGhostOpacity(now - startedAt, ChangePreviewAnimationDuration);
    }

    private void StartChangePreviewAnimation()
    {
        if (lastRenderedPreviewGeometry.Count == 0)
        {
            return;
        }

        changePreviewGhostGeometry = lastRenderedPreviewGeometry.ToArray();
        changePreviewAnimationStartedAt = DateTimeOffset.UtcNow;
        if (!changePreviewAnimationTimer.IsEnabled)
        {
            changePreviewAnimationTimer.Start();
        }
    }

    private void TickChangePreviewAnimation()
    {
        if (ResolveChangePreviewGhostOpacity(DateTimeOffset.UtcNow) <= 0d)
        {
            changePreviewAnimationTimer.Stop();
            changePreviewAnimationStartedAt = null;
            changePreviewGhostGeometry = [];
        }

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

    public sealed class RoomLabelClickedEventArgs : EventArgs
    {
        public RoomLabelClickedEventArgs(Guid roomLabelId)
        {
            RoomLabelId = roomLabelId;
        }

        public Guid RoomLabelId { get; }
    }

    public sealed class OpeningLabelClickedEventArgs : EventArgs
    {
        public OpeningLabelClickedEventArgs(Guid openingLabelId)
        {
            OpeningLabelId = openingLabelId;
        }

        public Guid OpeningLabelId { get; }
    }

    public sealed class DimensionClickedEventArgs : EventArgs
    {
        public DimensionClickedEventArgs(Guid dimensionId)
        {
            DimensionId = dimensionId;
        }

        public Guid DimensionId { get; }
    }

    public sealed class DimensionEditedEventArgs : EventArgs
    {
        public DimensionEditedEventArgs(
            Guid dimensionId,
            string sourceDimensionKey,
            DimensionDto dimension,
            DimensionHandleKind handleKind)
        {
            DimensionId = dimensionId;
            SourceDimensionKey = sourceDimensionKey;
            Dimension = dimension;
            HandleKind = handleKind;
        }

        public Guid DimensionId { get; }

        public string SourceDimensionKey { get; }

        public DimensionDto Dimension { get; }

        public DimensionHandleKind HandleKind { get; }
    }

    public sealed class FloorPlanMoveDeltaEventArgs : EventArgs
    {
        public FloorPlanMoveDeltaEventArgs(decimal deltaX, decimal deltaY)
        {
            DeltaX = deltaX;
            DeltaY = deltaY;
        }

        public decimal DeltaX { get; }

        public decimal DeltaY { get; }
    }

    public sealed class MovableArtifactMovedEventArgs : EventArgs
    {
        public MovableArtifactMovedEventArgs(
            string sourceArtifactKind,
            Guid sourceArtifactId,
            FloorPlanArtifactPositionMode positionMode,
            decimal? resolvedX,
            decimal? resolvedY,
            decimal? translationDx,
            decimal? translationDy)
        {
            SourceArtifactKind = sourceArtifactKind;
            SourceArtifactId = sourceArtifactId;
            PositionMode = positionMode;
            ResolvedX = resolvedX;
            ResolvedY = resolvedY;
            TranslationDx = translationDx;
            TranslationDy = translationDy;
        }

        public string SourceArtifactKind { get; }

        public Guid SourceArtifactId { get; }

        public FloorPlanArtifactPositionMode PositionMode { get; }

        public decimal? ResolvedX { get; }

        public decimal? ResolvedY { get; }

        public decimal? TranslationDx { get; }

        public decimal? TranslationDy { get; }
    }

    internal readonly record struct PreviewZoomState(double ZoomFactor, Vector PanOffset)
    {
        public static PreviewZoomState Default { get; } = new(1d, default);
    }

    internal readonly record struct FloorPlanMoveDelta(decimal DeltaX, decimal DeltaY);

    private readonly record struct FloorPlanMoveDragState(
        FloorPlanPreviewGeometry.PreviewViewport Viewport,
        Point PreviousPointerPosition);

    public enum DimensionHandleKind
    {
        FirstDefinitionPoint = 1,
        SecondDefinitionPoint = 2,
        DimensionLinePoint = 3
    }

    internal enum DimensionHitArea
    {
        StartExtent = 1,
        EndExtent = 2,
        BodyText = 3,
        BodyLine = 4
    }

    internal readonly record struct DimensionHit(
        DimensionDto Dimension,
        DimensionHandleKind SuggestedHandle,
        DimensionHitArea HitArea,
        Point ReferenceWorldPoint);

    internal readonly record struct DimensionHandleHit(
        DimensionDto Dimension,
        DimensionHandleKind HandleKind,
        Point ReferenceWorldPoint);

    internal readonly record struct PreviewArtifactMoveState(
        string SourceArtifactKind,
        Guid SourceArtifactId,
        FloorPlanArtifactPositionMode PositionMode,
        Point PointerStart,
        decimal BaseX,
        decimal BaseY,
        decimal BaseDx,
        decimal BaseDy,
        decimal CurrentDeltaX,
        decimal CurrentDeltaY)
    {
        public static PreviewArtifactMoveState ForAbsolutePoint(
            string sourceArtifactKind,
            Guid sourceArtifactId,
            Point pointerStart,
            decimal baseX,
            decimal baseY)
            => new(
                sourceArtifactKind,
                sourceArtifactId,
                FloorPlanArtifactPositionMode.AbsolutePoint,
                pointerStart,
                baseX,
                baseY,
                0m,
                0m,
                0m,
                0m);

        public static PreviewArtifactMoveState ForTranslation(
            string sourceArtifactKind,
            Guid sourceArtifactId,
            Point pointerStart,
            decimal baseDx,
            decimal baseDy)
            => new(
                sourceArtifactKind,
                sourceArtifactId,
                FloorPlanArtifactPositionMode.Translation,
                pointerStart,
                0m,
                0m,
                baseDx,
                baseDy,
                0m,
                0m);
    }

    internal readonly record struct PreviewMovableArtifactDescriptor(
        string SourceArtifactKind,
        Guid SourceArtifactId,
        FloorPlanArtifactPositionMode PositionMode,
        decimal BaseX,
        decimal BaseY,
        decimal BaseDx,
        decimal BaseDy,
        IReadOnlyList<Guid> GeometryPathIds);

    internal readonly record struct PreviewDimensionEditState(
        DimensionDto BaseDimension,
        DimensionHandleKind HandleKind,
        Point PointerStart,
        Point ReferenceWorldPoint,
        Point? CurrentWorldPoint)
    {
        public static PreviewDimensionEditState Start(
            DimensionDto baseDimension,
            DimensionHandleKind handleKind,
            Point pointerStart,
            Point referenceWorldPoint)
            => new(baseDimension, handleKind, pointerStart, referenceWorldPoint, null);
    }

    internal readonly record struct PreviewPendingDimensionEditState(
        DimensionDto BaseDimension,
        DimensionHandleKind HandleKind,
        Point PointerStart,
        Point ReferenceWorldPoint)
    {
        public static PreviewPendingDimensionEditState Start(
            DimensionDto baseDimension,
            DimensionHandleKind handleKind,
            Point pointerStart,
            Point referenceWorldPoint)
            => new(baseDimension, handleKind, pointerStart, referenceWorldPoint);
    }
}
