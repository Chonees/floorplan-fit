using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.ViewModels;

public sealed record MeasurementNodeGroupNodeOptionViewModel(
    string Name,
    MeasurementNodeDto Node,
    string Details)
{
    public Guid NodeId => Node.NodeId;
}
