namespace FloorplanFit.Domain.PlanSets;

public readonly record struct SheetAdjustmentProjectionCapability(
    bool SupportsAffinePlacement,
    bool SupportsCanonicalCompression);

public static class SheetAdjustmentProjectionCapabilities
{
    public static SheetAdjustmentProjectionCapability For(SheetAdjustmentProjectionMethod method)
        => method switch
        {
            SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity => new(
                SupportsAffinePlacement: true,
                SupportsCanonicalCompression: true),
            SheetAdjustmentProjectionMethod.RoofOverhangPreserving => new(
                SupportsAffinePlacement: true,
                SupportsCanonicalCompression: false),
            SheetAdjustmentProjectionMethod.FacadeHorizontalPreservingVerticals => new(
                SupportsAffinePlacement: true,
                SupportsCanonicalCompression: false),
            _ => throw new ArgumentOutOfRangeException(
                nameof(method),
                method,
                "Projection method is not supported.")
        };

    public static bool TryGetUnsupportedReason(
        SheetAdjustmentProjectionMethod method,
        int canonicalCompressionStepCount,
        out string? reason)
    {
        if (canonicalCompressionStepCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(canonicalCompressionStepCount),
                "Compression step count cannot be negative.");
        }

        var capability = For(method);
        if (canonicalCompressionStepCount == 0 || capability.SupportsCanonicalCompression)
        {
            reason = null;
            return false;
        }

        reason = $"{method} is affine-only; canonical compression is not implemented, and manual confirmation cannot make it exportable.";
        return true;
    }

    public static void EnsureSupported(
        SheetAdjustmentProjectionMethod method,
        int canonicalCompressionStepCount)
    {
        if (TryGetUnsupportedReason(method, canonicalCompressionStepCount, out var reason))
        {
            throw new InvalidOperationException(reason);
        }
    }
}
