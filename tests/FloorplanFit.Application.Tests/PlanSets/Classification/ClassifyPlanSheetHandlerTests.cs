using FloorplanFit.Application.PlanSets.Classification;
using FloorplanFit.Contracts.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Classification;

public sealed class ClassifyPlanSheetHandlerTests
{
    [Fact]
    public async Task HandleAsync_classifies_electrical_sheet_from_file_name()
    {
        var handler = new ClassifyPlanSheetHandler();

        var response = await handler.HandleAsync(
            new ClassifyPlanSheetRequest("lot-42-electrical-plan.dxf"),
            CancellationToken.None);

        Assert.Equal("ElectricalPlan", response.SheetType);
        Assert.Equal(0.9m, response.Confidence);
        Assert.False(response.RequiresManualConfirmation);
        Assert.Contains("electrical", response.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_classifies_facade_elevation_from_sheet_title()
    {
        var handler = new ClassifyPlanSheetHandler();

        var response = await handler.HandleAsync(
            new ClassifyPlanSheetRequest("A-400.dxf", SheetTitle: "Front Elevation"),
            CancellationToken.None);

        Assert.Equal("FacadeElevation", response.SheetType);
        Assert.Equal(0.85m, response.Confidence);
        Assert.False(response.RequiresManualConfirmation);
    }

    [Fact]
    public async Task HandleAsync_requires_manual_confirmation_for_unknown_sheet()
    {
        var handler = new ClassifyPlanSheetHandler();

        var response = await handler.HandleAsync(
            new ClassifyPlanSheetRequest("sheet-02.dxf"),
            CancellationToken.None);

        Assert.Equal("Unknown", response.SheetType);
        Assert.Equal(0m, response.Confidence);
        Assert.True(response.RequiresManualConfirmation);
    }

    [Fact]
    public async Task HandleAsync_requires_manual_confirmation_for_conflicting_sheet_hints()
    {
        var handler = new ClassifyPlanSheetHandler();

        var response = await handler.HandleAsync(
            new ClassifyPlanSheetRequest("roof-electrical-plan.dxf"),
            CancellationToken.None);

        Assert.Equal("Unknown", response.SheetType);
        Assert.Equal(0.4m, response.Confidence);
        Assert.True(response.RequiresManualConfirmation);
        Assert.Contains("multiple", response.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_classifies_electrical_sheet_from_layer_hints()
    {
        var handler = new ClassifyPlanSheetHandler();

        var response = await handler.HandleAsync(
            new ClassifyPlanSheetRequest(
                "A-201.dxf",
                LayerHints: ["E-LIGHTING", "E-POWER"]),
            CancellationToken.None);

        Assert.Equal("ElectricalPlan", response.SheetType);
        Assert.Equal(0.9m, response.Confidence);
        Assert.False(response.RequiresManualConfirmation);
        Assert.Contains("layer", response.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_requires_manual_confirmation_for_conflicting_layer_hints()
    {
        var handler = new ClassifyPlanSheetHandler();

        var response = await handler.HandleAsync(
            new ClassifyPlanSheetRequest(
                "A-201.dxf",
                LayerHints: ["E-POWER", "ROOF-OVERHANG"]),
            CancellationToken.None);

        Assert.Equal("Unknown", response.SheetType);
        Assert.Equal(0.4m, response.Confidence);
        Assert.True(response.RequiresManualConfirmation);
        Assert.Contains("multiple", response.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
