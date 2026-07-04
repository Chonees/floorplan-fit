using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class ChangeMeasurementCorridorAxisHandler
{
    private readonly IMeasurementCorridorRepository measurementCorridorRepository;
    private readonly IMeasurementNodeRepository measurementNodeRepository;
    private readonly IDimensionIntervalBindingRepository dimensionIntervalBindingRepository;
    private readonly IClock clock;
    private readonly IUnitOfWork unitOfWork;

    public ChangeMeasurementCorridorAxisHandler(
        IMeasurementCorridorRepository measurementCorridorRepository,
        IMeasurementNodeRepository measurementNodeRepository,
        IDimensionIntervalBindingRepository dimensionIntervalBindingRepository,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        this.measurementCorridorRepository = measurementCorridorRepository;
        this.measurementNodeRepository = measurementNodeRepository;
        this.dimensionIntervalBindingRepository = dimensionIntervalBindingRepository;
        this.clock = clock;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        Guid curationId,
        Guid corridorId,
        PinchAxisTag axisTag,
        decimal bandMinCoordinate,
        decimal bandMaxCoordinate,
        CancellationToken cancellationToken)
    {
        var corridor = await measurementCorridorRepository.GetByIdAsync(corridorId, cancellationToken)
            ?? throw new InvalidOperationException("Measurement corridor was not found.");
        if (corridor.FloorPlanCurationId != curationId)
        {
            throw new InvalidOperationException("Measurement corridor does not belong to the active curation.");
        }

        var updatedCorridor = new MeasurementCorridor(
            corridor.Id,
            corridor.FloorPlanCurationId,
            corridor.Name,
            axisTag,
            corridor.GuideGeometryPathId,
            bandMinCoordinate,
            bandMaxCoordinate,
            corridor.Status,
            corridor.SortOrder);

        await measurementCorridorRepository.UpdateAsync(updatedCorridor, cancellationToken);

        var originalAxisTag = corridor.AxisTag;
        var updatedNodes = new Dictionary<Guid, MeasurementNode>();
        var nodes = await measurementNodeRepository.ListByCorridorAsync(corridorId, cancellationToken);
        foreach (var node in nodes)
        {
            var offsets = ResolveOffsetsForAxisChange(node, originalAxisTag, axisTag);
            var updatedNode = new MeasurementNode(
                node.Id,
                node.FloorPlanCurationId,
                node.CorridorId,
                node.SortOrder,
                node.ReferenceKind,
                node.SourceArtifactKind,
                node.SourceArtifactId,
                node.GeometryPathId,
                node.SnapKind,
                node.AnchorX,
                node.AnchorY,
                ResolveAxisCoordinate(node.AnchorX, node.AnchorY, offsets.OffsetAlongAxis, axisTag),
                offsets.OffsetAlongAxis,
                offsets.OffsetNormal,
                node.PositionRatio);

            await measurementNodeRepository.UpdateAsync(updatedNode, cancellationToken);
            updatedNodes[updatedNode.Id] = updatedNode;
        }

        var bindings = await dimensionIntervalBindingRepository.ListByCurationAsync(curationId, cancellationToken);
        foreach (var binding in bindings.Where(item => item.CorridorId == corridorId))
        {
            if (!updatedNodes.TryGetValue(binding.StartNodeId, out var startNode) ||
                !updatedNodes.TryGetValue(binding.EndNodeId, out var endNode))
            {
                throw new InvalidOperationException("Dimension interval binding references a missing measurement node.");
            }

            await dimensionIntervalBindingRepository.UpsertAsync(
                new DimensionIntervalBinding(
                    binding.FloorPlanCurationId,
                    binding.DimensionId,
                    binding.CorridorId,
                    binding.StartNodeId,
                    binding.EndNodeId,
                    binding.BindingStatus,
                    startNode.AxisCoordinate,
                    endNode.AxisCoordinate,
                    clock.UtcNow),
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static (decimal OffsetAlongAxis, decimal OffsetNormal) ResolveOffsetsForAxisChange(
        MeasurementNode node,
        PinchAxisTag originalAxisTag,
        PinchAxisTag nextAxisTag)
    {
        if (originalAxisTag == nextAxisTag)
        {
            return (node.OffsetAlongAxis, node.OffsetNormal);
        }

        return (node.OffsetNormal, node.OffsetAlongAxis);
    }

    private static decimal ResolveAxisCoordinate(
        decimal anchorX,
        decimal anchorY,
        decimal offsetAlongAxis,
        PinchAxisTag axisTag)
        => axisTag == PinchAxisTag.Height
            ? decimal.Round(anchorY + offsetAlongAxis, 3, MidpointRounding.AwayFromZero)
            : decimal.Round(anchorX + offsetAlongAxis, 3, MidpointRounding.AwayFromZero);
}
