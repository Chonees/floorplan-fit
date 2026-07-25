using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteCommissionedHouseAdaptationProfileRepository
    : ICommissionedHouseAdaptationProfileRepository
{
    private readonly SqliteSession session;

    public SqliteCommissionedHouseAdaptationProfileRepository(SqliteSession session)
    {
        this.session = session;
    }

    public async Task UpsertAsync(
        CommissionedHouseAdaptationProfile profile,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText =
            """
            INSERT INTO commissioned_house_adaptation_profiles (
                floorplan_version_id,
                profile_json)
            VALUES (
                $floorplan_version_id,
                $profile_json)
            ON CONFLICT(floorplan_version_id) DO UPDATE SET
                profile_json = excluded.profile_json
            """;
        command.Parameters.AddWithValue("$floorplan_version_id", profile.FloorPlanVersionId.ToString());
        command.Parameters.AddWithValue("$profile_json", JsonSerializer.Serialize(profile));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<CommissionedHouseAdaptationProfile?> GetByFloorPlanVersionIdAsync(
        Guid floorPlanVersionId,
        Guid expectedPublishedCurationId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText =
            """
            SELECT profile_json
            FROM commissioned_house_adaptation_profiles
            WHERE floorplan_version_id = $floorplan_version_id
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$floorplan_version_id", floorPlanVersionId.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        if (reader.IsDBNull(0))
        {
            throw new InvalidOperationException(
                $"Stored commissioned house adaptation profile for FloorPlan version '{floorPlanVersionId}' " +
                "has a null profile_json value and cannot be loaded.");
        }

        var profileJson = reader.GetString(0);
        CommissionedHouseAdaptationProfile? profile;
        try
        {
            profile = JsonSerializer.Deserialize<CommissionedHouseAdaptationProfile>(profileJson);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Stored commissioned house adaptation profile for FloorPlan version '{floorPlanVersionId}' " +
                "contains corrupt JSON and cannot be loaded.",
                exception);
        }

        if (profile is null)
        {
            throw new InvalidOperationException(
                $"Stored commissioned house adaptation profile for FloorPlan version '{floorPlanVersionId}' " +
                "deserialized to null and cannot be loaded.");
        }

        if (profile.FloorPlanVersionId != floorPlanVersionId)
        {
            throw new InvalidOperationException(
                $"Stored commissioned house adaptation profile key '{floorPlanVersionId}' does not match " +
                $"the JSON FloorPlanVersionId '{profile.FloorPlanVersionId}'.");
        }

        if (profile.PublishedCurationId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Stored commissioned house adaptation profile for FloorPlan version '{floorPlanVersionId}' " +
                "has no JSON PublishedCurationId identity.");
        }

        if (profile.PublishedCurationId != expectedPublishedCurationId)
        {
            return null;
        }

        return profile;
    }

    public async Task RemoveByFloorPlanVersionIdAsync(
        Guid floorPlanVersionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText =
            """
            DELETE FROM commissioned_house_adaptation_profiles
            WHERE floorplan_version_id = $floorplan_version_id
            """;
        command.Parameters.AddWithValue("$floorplan_version_id", floorPlanVersionId.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
