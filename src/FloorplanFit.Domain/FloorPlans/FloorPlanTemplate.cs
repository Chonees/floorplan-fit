namespace FloorplanFit.Domain.FloorPlans;

public sealed class FloorPlanTemplate
{
    public FloorPlanTemplate(Guid id, string code, string name, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Id = id;
        Code = code;
        Name = name;
        IsActive = isActive;
    }

    public Guid Id { get; }

    public string Code { get; }

    public string Name { get; }

    public Guid? CurrentVersionId { get; private set; }

    public bool IsActive { get; }

    public void SetCurrentVersion(Guid versionId)
    {
        CurrentVersionId = versionId;
    }
}
