using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Extraction;
using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Application.FloorPlans.Library;
using FloorplanFit.Contracts.FloorPlans;
using Microsoft.Extensions.DependencyInjection;

namespace FloorplanFit.Desktop.ViewModels;

public sealed partial class LibraryViewModel : ObservableObject
{
    private readonly IServiceScopeFactory scopeFactory;

    public LibraryViewModel(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    public ObservableCollection<FloorPlanLibraryItemDto> Items { get; } = [];

    [ObservableProperty]
    private FloorPlanLibraryItemDto? selectedItem;

    [ObservableProperty]
    private string statusMessage = "Ready";

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        StatusMessage = "Loading...";
        await RefreshItemsAsync(cancellationToken);
        StatusMessage = Items.Count == 0
            ? "Ready"
            : $"Loaded {Items.Count} floor plan(s)";
    }

    public async Task ImportAsync(string filePath, CancellationToken cancellationToken)
    {
        StatusMessage = "Importing...";

        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportFloorPlanHandler>();
        var response = await handler.HandleAsync(new ImportFloorPlanRequest(filePath), cancellationToken);

        await ExtractByTemplateAsync(response.Item.TemplateId, cancellationToken);
        await RefreshItemsAsync(cancellationToken);
        SelectedItem = Items.FirstOrDefault(item => item.TemplateId == response.Item.TemplateId);
        StatusMessage = $"Imported and extracted {response.Item.Name} v{response.Item.ActiveVersionNumber}";
    }

    public async Task ExtractSelectedAsync(CancellationToken cancellationToken)
    {
        if (SelectedItem is null)
        {
            return;
        }

        StatusMessage = $"Extracting walls for {SelectedItem.Name}...";
        await ExtractByTemplateAsync(SelectedItem.TemplateId, cancellationToken);
        await RefreshItemsAsync(cancellationToken);
        SelectedItem = Items.FirstOrDefault(item => item.TemplateId == SelectedItem.TemplateId);
        StatusMessage = $"Extracted walls for {SelectedItem?.Name ?? "floor plan"}";
    }

    public async Task<FloorPlanReviewViewModel?> OpenSelectedReviewAsync(CancellationToken cancellationToken)
    {
        if (SelectedItem is null)
        {
            return null;
        }

        var templateId = SelectedItem.TemplateId;
        if (string.Equals(SelectedItem.Status, "Imported", StringComparison.OrdinalIgnoreCase))
        {
            await ExtractSelectedAsync(cancellationToken);
        }

        var reviewViewModel = new FloorPlanReviewViewModel(scopeFactory, templateId);
        await reviewViewModel.LoadAsync(cancellationToken);
        return reviewViewModel;
    }

    private async Task RefreshItemsAsync(CancellationToken cancellationToken)
    {
        var selectedTemplateId = SelectedItem?.TemplateId;
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetFloorPlanLibraryHandler>();
        var items = await handler.HandleAsync(cancellationToken);

        Items.Clear();

        foreach (var item in items)
        {
            Items.Add(item);
        }

        SelectedItem = selectedTemplateId is null
            ? Items.FirstOrDefault()
            : Items.FirstOrDefault(item => item.TemplateId == selectedTemplateId);
    }

    private async Task ExtractByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sourceReader = scope.ServiceProvider.GetRequiredService<IFloorPlanExtractionSourceReader>();
        var source = await sourceReader.GetCurrentSourceAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Current floor plan version was not found for extraction.");

        var handler = scope.ServiceProvider.GetRequiredService<ExtractWallCandidatesHandler>();
        await handler.HandleAsync(source.FloorPlanVersionId, source.ManagedFilePath, cancellationToken);
    }
}
