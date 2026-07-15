namespace FloorplanFit.Application.Abstractions;

public sealed class ProjectedPlanSheetManualReviewRequiredException : InvalidOperationException
{
    public ProjectedPlanSheetManualReviewRequiredException(string message)
        : base(message)
    {
    }
}
