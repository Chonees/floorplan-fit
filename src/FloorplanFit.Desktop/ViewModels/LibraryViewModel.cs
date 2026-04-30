using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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

        await RefreshItemsAsync(cancellationToken);
        StatusMessage = $"Imported {response.Item.Name} v{response.Item.ActiveVersionNumber}";
    }

    private async Task RefreshItemsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetFloorPlanLibraryHandler>();
        var items = await handler.HandleAsync(cancellationToken);

        Items.Clear();

        foreach (var item in items)
        {
            Items.Add(item);
        }
    }
}
