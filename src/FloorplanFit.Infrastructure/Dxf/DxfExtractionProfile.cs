using System.Text.RegularExpressions;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed partial class DxfExtractionProfile
{
    private readonly IReadOnlyDictionary<string, string> openingGeometryLayerKinds;
    private readonly IReadOnlyDictionary<string, string> openingLabelLayerKinds;
    private readonly IReadOnlyDictionary<string, string> fixedComponentLayerKinds;
    private readonly IReadOnlyDictionary<string, string> protectedDetailLayerKinds;
    private readonly IReadOnlyList<ProfileTokenRule> fixedComponentBlockRules;
    private readonly IReadOnlySet<string> roomLabelLayers;
    private readonly IReadOnlyList<string> roomNameKeywords;
    private readonly IReadOnlyList<string> excludedRoomLabelTokens;

    private DxfExtractionProfile(
        string name,
        IReadOnlyList<string> seedFloorPlans,
        IReadOnlyDictionary<string, string> openingGeometryLayerKinds,
        IReadOnlyDictionary<string, string> openingLabelLayerKinds,
        IReadOnlyDictionary<string, string> fixedComponentLayerKinds,
        IReadOnlyDictionary<string, string> protectedDetailLayerKinds,
        IReadOnlyList<ProfileTokenRule> fixedComponentBlockRules,
        IReadOnlySet<string> roomLabelLayers,
        IReadOnlyList<string> roomNameKeywords,
        IReadOnlyList<string> excludedRoomLabelTokens)
    {
        Name = name;
        SeedFloorPlans = seedFloorPlans;
        this.openingGeometryLayerKinds = openingGeometryLayerKinds;
        this.openingLabelLayerKinds = openingLabelLayerKinds;
        this.fixedComponentLayerKinds = fixedComponentLayerKinds;
        this.protectedDetailLayerKinds = protectedDetailLayerKinds;
        this.fixedComponentBlockRules = fixedComponentBlockRules;
        this.roomLabelLayers = roomLabelLayers;
        this.roomNameKeywords = roomNameKeywords;
        this.excludedRoomLabelTokens = excludedRoomLabelTokens;
    }

    public static DxfExtractionProfile PointeHomes { get; } = CreatePointeHomesProfile();

    public string Name { get; }

    public IReadOnlyList<string> SeedFloorPlans { get; }

    public bool IsWallCandidateLayer(string? layerName)
    {
        return ContainsToken(layerName, "WALL");
    }

    public bool IsPhysicalWallLayer(string? layerName)
    {
        return IsWallCandidateLayer(layerName) && !ContainsToken(layerName, "ELECTRICAL");
    }

    public bool IsRoomLabelLayer(string? layerName)
    {
        return !string.IsNullOrWhiteSpace(layerName) && roomLabelLayers.Contains(layerName);
    }

    public bool LooksLikeRoomName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (excludedRoomLabelTokens.Any(token => ContainsToken(value, token)))
        {
            return false;
        }

        return roomNameKeywords.Any(keyword => ContainsToken(value, keyword));
    }

    public string? ResolveOpeningGeometryKind(string? layerName)
    {
        return ResolveKind(openingGeometryLayerKinds, layerName);
    }

    public string? ResolveOpeningLabelKind(string? layerName)
    {
        return ResolveKind(openingLabelLayerKinds, layerName);
    }

    public bool LooksLikeOpeningModelOrSizeLabel(string text)
    {
        return !string.IsNullOrWhiteSpace(text) && OpeningModelOrSizeLabelRegex().IsMatch(text);
    }

    public string? ResolveFixedComponentLayerKind(string? layerName)
    {
        return ResolveKind(fixedComponentLayerKinds, layerName);
    }

    public string? ResolveFixedComponentBlockKind(string? blockName)
    {
        if (string.IsNullOrWhiteSpace(blockName))
        {
            return null;
        }

        return fixedComponentBlockRules
            .FirstOrDefault(rule => ContainsToken(blockName, rule.Token))
            .Kind;
    }

    public string? ResolveProtectedDetailLayerKind(string? layerName)
    {
        return ResolveKind(protectedDetailLayerKinds, layerName);
    }

    private static DxfExtractionProfile CreatePointeHomesProfile()
    {
        return new DxfExtractionProfile(
            "Pointe Homes CAD",
            ["SEMINOLE2000", "SANTA-BARBARA"],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["DOORS"] = "Door",
                ["WIN"] = "Window",
                ["WINS"] = "Window"
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["DOORTEXT"] = "Door",
                ["WINDWS LBLS"] = "Window"
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CABS"] = "Cabinet",
                ["CABS-FLOORPLAN"] = "Cabinet",
                ["FIXTURES"] = "Fixture",
                ["L1"] = "Fixture"
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["MISC"] = "WetAreaDetail",
                ["HATCH"] = "WetAreaDetail"
            },
            [
                new ProfileTokenRule("TOILET", "Toilet"),
                new ProfileTokenRule("STOVE", "Appliance"),
                new ProfileTokenRule("DISHWASHER", "Appliance"),
                new ProfileTokenRule("WASH", "Appliance"),
                new ProfileTokenRule("DRY", "Appliance"),
                new ProfileTokenRule("SINK", "Fixture"),
                new ProfileTokenRule("TUB", "Fixture"),
                new ProfileTokenRule("SHOWER", "Fixture")
            ],
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ROOM LBLS"
            },
            [
                "BATH",
                "BEDROOM",
                "CLOS",
                "CLOSET",
                "DINING",
                "ENTRY",
                "GARAGE",
                "KITCHEN",
                "LIVING",
                "MASTER",
                "MSTR",
                "PANTRY",
                "PATIO",
                "PORCH",
                "PWDR",
                "UTILITY"
            ],
            [
                "FLOOR PLAN",
                "WALL LEGEND"
            ]);
    }

    private static string? ResolveKind(IReadOnlyDictionary<string, string> kindByLayer, string? layerName)
    {
        return layerName is not null && kindByLayer.TryGetValue(layerName, out var kind)
            ? kind
            : null;
    }

    private static bool ContainsToken(string? value, string token)
    {
        return value?.Contains(token, StringComparison.OrdinalIgnoreCase) == true;
    }

    [GeneratedRegex("""(\b\d{4}\b|\b\d+'\s*W\s*X\s*\d+'\s*H\b|\b\d+(?:\.\d+)?\s*["”]\s*(?:DR\.?|R\.?\s*O\.?)\b)""", RegexOptions.IgnoreCase)]
    private static partial Regex OpeningModelOrSizeLabelRegex();

    private readonly record struct ProfileTokenRule(string Token, string Kind);
}
