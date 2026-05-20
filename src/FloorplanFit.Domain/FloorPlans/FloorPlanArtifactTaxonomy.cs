namespace FloorplanFit.Domain.FloorPlans;

public static class FloorPlanArtifactTaxonomy
{
    public const string OpeningFamily = "Opening";
    public const string FixedFamily = "Fixed";
    public const string ProtectedFamily = "Protected";

    public const string OpeningCategory = "Opening";
    public const string WetFixtureCategory = "WetFixture";
    public const string ApplianceCategory = "Appliance";
    public const string MillworkCategory = "Millwork";
    public const string GenericFixedCategory = "GenericFixed";
    public const string WetAssemblyCategory = "WetAssembly";
    public const string TechnicalDetailCategory = "TechnicalDetail";
    public const string GenericProtectedCategory = "GenericProtected";

    public const string DoorType = "Door";
    public const string WindowType = "Window";
    public const string UnknownOpeningType = "UnknownOpening";
    public const string ToiletType = "Toilet";
    public const string SinkType = "Sink";
    public const string TubType = "Tub";
    public const string ShowerType = "Shower";
    public const string UnknownWetFixtureType = "UnknownWetFixture";
    public const string CooktopType = "Cooktop";
    public const string OvenType = "Oven";
    public const string DishwasherType = "Dishwasher";
    public const string RefrigeratorType = "Refrigerator";
    public const string WasherType = "Washer";
    public const string DryerType = "Dryer";
    public const string UnknownApplianceType = "UnknownAppliance";
    public const string CabinetType = "Cabinet";
    public const string VanityType = "Vanity";
    public const string IslandType = "Island";
    public const string CounterType = "Counter";
    public const string UnknownMillworkType = "UnknownMillwork";
    public const string GenericFixtureType = "GenericFixture";
    public const string ShowerEnclosureType = "ShowerEnclosure";
    public const string TubEnclosureType = "TubEnclosure";
    public const string WetHatchType = "WetHatch";
    public const string UnknownWetAssemblyType = "UnknownWetAssembly";
    public const string HatchType = "Hatch";
    public const string SafetyDetailType = "SafetyDetail";
    public const string MiscDetailType = "MiscDetail";
    public const string UnknownTechnicalDetailType = "UnknownTechnicalDetail";
    public const string GenericProtectedDetailType = "GenericProtectedDetail";

    public const string DoorColorArgb = "#FF455668";
    public const string WindowColorArgb = DoorColorArgb;
    public const string WetFixtureColorArgb = "#FFDC2626";
    public const string ApplianceColorArgb = "#FFF59E0B";
    public const string MillworkColorArgb = "#FF06B6D4";
    public const string GenericFixedColorArgb = "#FFEF4444";
    public const string WetAssemblyColorArgb = "#FF8B5CF6";
    public const string TechnicalDetailColorArgb = "#FFEAB308";
    public const string GenericProtectedColorArgb = "#FFA78BFA";
    public const string UnknownColorArgb = "#FF6B7280";

    private static readonly IReadOnlyList<string> families =
    [
        OpeningFamily,
        FixedFamily,
        ProtectedFamily
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> categoriesByFamily =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [OpeningFamily] = [OpeningCategory],
            [FixedFamily] = [WetFixtureCategory, ApplianceCategory, MillworkCategory, GenericFixedCategory],
            [ProtectedFamily] = [WetAssemblyCategory, TechnicalDetailCategory, GenericProtectedCategory]
        };

    private static readonly IReadOnlyDictionary<(string Family, string Category), IReadOnlyList<string>> typesByCategory =
        new Dictionary<(string Family, string Category), IReadOnlyList<string>>
        {
            [(OpeningFamily, OpeningCategory)] = [DoorType, WindowType, UnknownOpeningType],
            [(FixedFamily, WetFixtureCategory)] = [ToiletType, SinkType, TubType, ShowerType, UnknownWetFixtureType],
            [(FixedFamily, ApplianceCategory)] = [CooktopType, OvenType, DishwasherType, RefrigeratorType, WasherType, DryerType, UnknownApplianceType],
            [(FixedFamily, MillworkCategory)] = [CabinetType, VanityType, IslandType, CounterType, UnknownMillworkType],
            [(FixedFamily, GenericFixedCategory)] = [GenericFixtureType],
            [(ProtectedFamily, WetAssemblyCategory)] = [ShowerEnclosureType, TubEnclosureType, WetHatchType, UnknownWetAssemblyType],
            [(ProtectedFamily, TechnicalDetailCategory)] = [HatchType, SafetyDetailType, MiscDetailType, UnknownTechnicalDetailType],
            [(ProtectedFamily, GenericProtectedCategory)] = [GenericProtectedDetailType]
        };

    public static IReadOnlyList<string> Families => families;

    public static IReadOnlyList<string> GetCategories(string family)
    {
        return categoriesByFamily.TryGetValue(family, out var categories)
            ? categories
            : [];
    }

    public static IReadOnlyList<string> GetTypes(string family, string category)
    {
        return typesByCategory.TryGetValue((family, category), out var types)
            ? types
            : [];
    }

    public static bool IsValidClassification(string family, string category, string type)
    {
        return GetCategories(family).Contains(category, StringComparer.Ordinal) &&
               GetTypes(family, category).Contains(type, StringComparer.Ordinal);
    }

    public static (string Family, string Category, string Type) ResolveDetectedOpeningClassification(string kind)
    {
        if (string.Equals(kind, WindowType, StringComparison.OrdinalIgnoreCase))
        {
            return (OpeningFamily, OpeningCategory, WindowType);
        }

        return string.Equals(kind, DoorType, StringComparison.OrdinalIgnoreCase)
            ? (OpeningFamily, OpeningCategory, DoorType)
            : (OpeningFamily, OpeningCategory, UnknownOpeningType);
    }

    public static (string Family, string Category, string Type) ResolveDetectedFixedClassification(string kind)
    {
        if (string.Equals(kind, ToiletType, StringComparison.OrdinalIgnoreCase))
        {
            return (FixedFamily, WetFixtureCategory, ToiletType);
        }

        if (string.Equals(kind, SinkType, StringComparison.OrdinalIgnoreCase))
        {
            return (FixedFamily, WetFixtureCategory, SinkType);
        }

        if (string.Equals(kind, TubType, StringComparison.OrdinalIgnoreCase))
        {
            return (FixedFamily, WetFixtureCategory, TubType);
        }

        if (string.Equals(kind, ShowerType, StringComparison.OrdinalIgnoreCase))
        {
            return (FixedFamily, WetFixtureCategory, ShowerType);
        }

        if (string.Equals(kind, CabinetType, StringComparison.OrdinalIgnoreCase))
        {
            return (FixedFamily, MillworkCategory, CabinetType);
        }

        return string.Equals(kind, ApplianceCategory, StringComparison.OrdinalIgnoreCase)
            ? (FixedFamily, ApplianceCategory, UnknownApplianceType)
            : (FixedFamily, GenericFixedCategory, GenericFixtureType);
    }

    public static (string Family, string Category, string Type) ResolveDetectedProtectedClassification(string kind)
    {
        if (string.Equals(kind, "WetAreaDetail", StringComparison.OrdinalIgnoreCase))
        {
            return (ProtectedFamily, WetAssemblyCategory, UnknownWetAssemblyType);
        }

        if (string.Equals(kind, HatchType, StringComparison.OrdinalIgnoreCase))
        {
            return (ProtectedFamily, TechnicalDetailCategory, HatchType);
        }

        return (ProtectedFamily, GenericProtectedCategory, GenericProtectedDetailType);
    }

    public static string ResolveColorArgb(string family, string category, string type)
    {
        if (type.StartsWith("Unknown", StringComparison.Ordinal))
        {
            return UnknownColorArgb;
        }

        return (family, category) switch
        {
            (OpeningFamily, OpeningCategory) when string.Equals(type, WindowType, StringComparison.Ordinal) => WindowColorArgb,
            (OpeningFamily, OpeningCategory) => DoorColorArgb,
            (FixedFamily, WetFixtureCategory) => WetFixtureColorArgb,
            (FixedFamily, ApplianceCategory) => ApplianceColorArgb,
            (FixedFamily, MillworkCategory) => MillworkColorArgb,
            (FixedFamily, GenericFixedCategory) => GenericFixedColorArgb,
            (ProtectedFamily, WetAssemblyCategory) => WetAssemblyColorArgb,
            (ProtectedFamily, TechnicalDetailCategory) => TechnicalDetailColorArgb,
            (ProtectedFamily, GenericProtectedCategory) => GenericProtectedColorArgb,
            _ => UnknownColorArgb
        };
    }

    public static int ResolveFamilyHitTestPriority(string family)
    {
        return family switch
        {
            ProtectedFamily => 3,
            FixedFamily => 2,
            OpeningFamily => 1,
            _ => 0
        };
    }

    public static int ResolveFamilySortOrder(string family)
    {
        return family switch
        {
            OpeningFamily => 1,
            FixedFamily => 2,
            ProtectedFamily => 3,
            _ => int.MaxValue
        };
    }

    public static int ResolveCategorySortOrder(string family, string category)
    {
        var categories = GetCategories(family);
        for (var index = 0; index < categories.Count; index++)
        {
            if (string.Equals(categories[index], category, StringComparison.Ordinal))
            {
                return index + 1;
            }
        }

        return int.MaxValue;
    }
}
