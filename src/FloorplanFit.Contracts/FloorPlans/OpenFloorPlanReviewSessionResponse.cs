namespace FloorplanFit.Contracts.FloorPlans;

public sealed record OpenFloorPlanReviewSessionResponse(
    Guid DraftCurationId,
    FloorPlanReviewSessionDto Session);
