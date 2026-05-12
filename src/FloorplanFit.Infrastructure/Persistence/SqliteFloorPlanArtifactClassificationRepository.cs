using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteFloorPlanArtifactClassificationRepository : IFloorPlanArtifactClassificationRepository
{
    private readonly SqliteSession session;

    public SqliteFloorPlanArtifactClassificationRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task<IReadOnlyList<FloorPlanArtifactClassification>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                floorplan_curation_id,
                source_artifact_kind,
                source_artifact_id,
                resolved_family,
                resolved_category,
                resolved_type,
                decision_state,
                updated_at_utc
            FROM floorplan_artifact_classifications
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY updated_at_utc ASC, source_artifact_kind ASC, source_artifact_id ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());

        var items = new List<FloorPlanArtifactClassification>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(Map(reader));
        }

        return Task.FromResult<IReadOnlyList<FloorPlanArtifactClassification>>(items);
    }

    public Task UpsertAsync(FloorPlanArtifactClassification classification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO floorplan_artifact_classifications (
                floorplan_curation_id,
                source_artifact_kind,
                source_artifact_id,
                resolved_family,
                resolved_category,
                resolved_type,
                decision_state,
                updated_at_utc
            )
            VALUES (
                $floorplan_curation_id,
                $source_artifact_kind,
                $source_artifact_id,
                $resolved_family,
                $resolved_category,
                $resolved_type,
                $decision_state,
                $updated_at_utc
            )
            ON CONFLICT(floorplan_curation_id, source_artifact_kind, source_artifact_id)
            DO UPDATE SET
                resolved_family = excluded.resolved_family,
                resolved_category = excluded.resolved_category,
                resolved_type = excluded.resolved_type,
                decision_state = excluded.decision_state,
                updated_at_utc = excluded.updated_at_utc
            """);

        command.Parameters.AddWithValue("$floorplan_curation_id", classification.FloorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$source_artifact_kind", classification.SourceArtifactKind);
        command.Parameters.AddWithValue("$source_artifact_id", classification.SourceArtifactId.ToString());
        command.Parameters.AddWithValue("$resolved_family", classification.ResolvedFamily);
        command.Parameters.AddWithValue("$resolved_category", classification.ResolvedCategory);
        command.Parameters.AddWithValue("$resolved_type", classification.ResolvedType);
        command.Parameters.AddWithValue("$decision_state", (int)classification.DecisionState);
        command.Parameters.AddWithValue("$updated_at_utc", classification.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private static FloorPlanArtifactClassification Map(SqliteDataReader reader)
    {
        return new FloorPlanArtifactClassification(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            Guid.Parse(reader.GetString(2)),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            (FloorPlanArtifactDecisionState)reader.GetInt32(6),
            DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
