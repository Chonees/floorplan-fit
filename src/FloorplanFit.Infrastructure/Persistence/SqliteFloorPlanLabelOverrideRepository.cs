using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteFloorPlanLabelOverrideRepository : IFloorPlanLabelOverrideRepository
{
    private readonly SqliteSession session;

    public SqliteFloorPlanLabelOverrideRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task<IReadOnlyList<FloorPlanLabelOverride>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                floorplan_curation_id,
                source_artifact_kind,
                source_artifact_id,
                resolved_text_height,
                updated_at_utc
            FROM floorplan_label_overrides
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY updated_at_utc ASC, source_artifact_kind ASC, source_artifact_id ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());

        var items = new List<FloorPlanLabelOverride>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(reader.IsDBNull(3)
                ? FloorPlanLabelOverride.CreateDetectedDefault(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    Guid.Parse(reader.GetString(2)),
                    DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind))
                : FloorPlanLabelOverride.CreateResolvedTextHeight(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    Guid.Parse(reader.GetString(2)),
                    decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                    DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
        }

        return Task.FromResult<IReadOnlyList<FloorPlanLabelOverride>>(items);
    }

    public Task UpsertAsync(FloorPlanLabelOverride labelOverride, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO floorplan_label_overrides (
                floorplan_curation_id,
                source_artifact_kind,
                source_artifact_id,
                resolved_text_height,
                updated_at_utc
            )
            VALUES (
                $floorplan_curation_id,
                $source_artifact_kind,
                $source_artifact_id,
                $resolved_text_height,
                $updated_at_utc
            )
            ON CONFLICT(floorplan_curation_id, source_artifact_kind, source_artifact_id)
            DO UPDATE SET
                resolved_text_height = excluded.resolved_text_height,
                updated_at_utc = excluded.updated_at_utc
            """);

        command.Parameters.AddWithValue("$floorplan_curation_id", labelOverride.FloorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$source_artifact_kind", labelOverride.SourceArtifactKind);
        command.Parameters.AddWithValue("$source_artifact_id", labelOverride.SourceArtifactId.ToString());
        command.Parameters.AddWithValue("$resolved_text_height", labelOverride.ResolvedTextHeight is null ? DBNull.Value : labelOverride.ResolvedTextHeight.Value.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$updated_at_utc", labelOverride.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
