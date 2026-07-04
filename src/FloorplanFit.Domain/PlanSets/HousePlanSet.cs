namespace FloorplanFit.Domain.PlanSets;

public sealed class HousePlanSet
{
    public HousePlanSet(
        Guid id,
        Guid sourceFloorPlanTemplateId,
        string code,
        string name,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("House plan set id is required.", nameof(id));
        }

        if (sourceFloorPlanTemplateId == Guid.Empty)
        {
            throw new ArgumentException("Source floor-plan template id is required.", nameof(sourceFloorPlanTemplateId));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("House plan set code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("House plan set name is required.", nameof(name));
        }

        Id = id;
        SourceFloorPlanTemplateId = sourceFloorPlanTemplateId;
        Code = code.Trim();
        Name = name.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid SourceFloorPlanTemplateId { get; }

    public string Code { get; }

    public string Name { get; }

    public DateTime CreatedAtUtc { get; }
}
