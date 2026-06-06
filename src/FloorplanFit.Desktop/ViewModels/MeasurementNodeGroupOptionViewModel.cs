using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.ViewModels;

public sealed record MeasurementNodeGroupOptionViewModel(
    MeasurementCorridorDto Corridor,
    string Name,
    string Details)
{
    public Guid CorridorId => Corridor.CorridorId;
}
