using System.Collections.ObjectModel;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.ViewModels;

public sealed class CuratedArtifactGroupViewModel
{
    public CuratedArtifactGroupViewModel(
        string family,
        string category,
        string title,
        string subtitle,
        string colorArgb,
        IReadOnlyList<CuratedPlanArtifactDto> items)
    {
        Family = family;
        Category = category;
        Title = title;
        Subtitle = subtitle;
        ColorArgb = colorArgb;
        Items = new ObservableCollection<CuratedPlanArtifactDto>(items);
    }

    public string Family { get; }

    public string Category { get; }

    public string Title { get; }

    public string Subtitle { get; }

    public string ColorArgb { get; }

    public ObservableCollection<CuratedPlanArtifactDto> Items { get; }

    public int Count => Items.Count;
}
