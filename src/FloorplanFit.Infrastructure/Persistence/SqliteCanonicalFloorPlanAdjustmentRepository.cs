using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteCanonicalFloorPlanAdjustmentRepository : ICanonicalFloorPlanAdjustmentRepository
{
    private readonly SqliteSession session;

    public SqliteCanonicalFloorPlanAdjustmentRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(CanonicalFloorPlanAdjustment adjustment, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText =
            """
            INSERT INTO canonical_floor_plan_adjustments (
                id,
                plan_set_version_id,
                canonical_floor_plan_version_id,
                site_plan_source_path,
                canonical_floor_plan_export_path,
                placement_json,
                adjustment_recipe_json,
                created_at_utc)
            VALUES (
                $id,
                $plan_set_version_id,
                $canonical_floor_plan_version_id,
                $site_plan_source_path,
                $canonical_floor_plan_export_path,
                $placement_json,
                $adjustment_recipe_json,
                $created_at_utc)
            """;
        command.Parameters.AddWithValue("$id", adjustment.Id.ToString());
        command.Parameters.AddWithValue("$plan_set_version_id", adjustment.PlanSetVersionId.ToString());
        command.Parameters.AddWithValue("$canonical_floor_plan_version_id", adjustment.CanonicalFloorPlanVersionId.ToString());
        command.Parameters.AddWithValue("$site_plan_source_path", adjustment.SitePlanSourcePath);
        command.Parameters.AddWithValue("$canonical_floor_plan_export_path", adjustment.CanonicalFloorPlanExportPath);
        command.Parameters.AddWithValue("$placement_json", adjustment.PlacementJson);
        command.Parameters.AddWithValue("$adjustment_recipe_json", adjustment.AdjustmentRecipeJson);
        command.Parameters.AddWithValue("$created_at_utc", adjustment.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<CanonicalFloorPlanAdjustment?> GetByIdAsync(
        Guid adjustmentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText =
            """
            SELECT
                id,
                plan_set_version_id,
                canonical_floor_plan_version_id,
                site_plan_source_path,
                canonical_floor_plan_export_path,
                placement_json,
                adjustment_recipe_json,
                created_at_utc
            FROM canonical_floor_plan_adjustments
            WHERE id = $id
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$id", adjustmentId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<CanonicalFloorPlanAdjustment?>(null);
        }

        return Task.FromResult<CanonicalFloorPlanAdjustment?>(new CanonicalFloorPlanAdjustment(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            Guid.Parse(reader.GetString(2)),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
    }
}
