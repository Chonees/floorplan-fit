using System.Text;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Desktop.ViewModels;

internal static class FloorPlanReviewDisplayText
{
    public static string GetCuratedGroupTitle(string family, string category)
    {
        return (family, category) switch
        {
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.OpeningFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.OpeningCategory) => "Doors & Windows",
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.FixedFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.WetFixtureCategory) => "Plumbing Fixtures",
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.FixedFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.ApplianceCategory) => "Appliances",
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.FixedFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.MillworkCategory) => "Cabinets & Millwork",
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.FixedFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.GenericFixedCategory) => "Generic Fixtures",
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.ProtectedFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.WetAssemblyCategory) => "Wet Area Details",
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.ProtectedFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.TechnicalDetailCategory) => "Technical Details",
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.ProtectedFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.GenericProtectedCategory) => "Protected Details",
            _ => $"{ToTitleCase(family)} / {ToTitleCase(category)}"
        };
    }

    public static string GetCuratedGroupSubtitle(string family, string category)
    {
        return (family, category) switch
        {
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.OpeningFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.OpeningCategory) => "Door and window geometry curated from the plan.",
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.FixedFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.WetFixtureCategory) => "Tubs, sinks, toilets and showers.",
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.FixedFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.ApplianceCategory) => "Kitchen and laundry equipment.",
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.FixedFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.MillworkCategory) => "Cabinets, vanities, counters and islands.",
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.FixedFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.GenericFixedCategory) => "Fixed geometry that still needs better classification.",
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.ProtectedFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.WetAssemblyCategory) => "Protected wet-zone enclosures and hatches.",
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.ProtectedFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.TechnicalDetailCategory) => "Technical or safety details that should survive fitting.",
            (var f, var c) when Is(f, FloorPlanArtifactTaxonomy.ProtectedFamily) &&
                              Is(c, FloorPlanArtifactTaxonomy.GenericProtectedCategory) => "Protected geometry still awaiting better classification.",
            _ => "Curated geometry grouped by semantic category."
        };
    }

    public static string ToTitleCase(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(raw.Length + 8);
        for (var index = 0; index < raw.Length; index++)
        {
            var current = raw[index];
            if (index > 0 &&
                char.IsUpper(current) &&
                (char.IsLower(raw[index - 1]) || char.IsDigit(raw[index - 1])))
            {
                builder.Append(' ');
            }

            if (current is '_' or '-')
            {
                builder.Append(' ');
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString().Trim();
    }

    private static bool Is(string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
