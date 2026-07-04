using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteFloorPlanArtifactPositionRepository : IFloorPlanArtifactPositionRepository
{
    private readonly SqliteSession session;

    public SqliteFloorPlanArtifactPositionRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task<IReadOnlyList<FloorPlanArtifactPosition>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                floorplan_curation_id,
                source_artifact_kind,
                source_artifact_id,
                position_mode,
                resolved_x,
                resolved_y,
                translation_dx,
                translation_dy,
                updated_at_utc
            FROM floorplan_artifact_positions
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY updated_at_utc ASC, source_artifact_kind ASC, source_artifact_id ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());

        var items = new List<FloorPlanArtifactPosition>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var positionMode = (FloorPlanArtifactPositionMode)reader.GetInt32(3);
            items.Add(positionMode == FloorPlanArtifactPositionMode.AbsolutePoint
                ? FloorPlanArtifactPosition.CreateAbsolutePoint(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    Guid.Parse(reader.GetString(2)),
                    decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                    DateTime.Parse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind))
                : FloorPlanArtifactPosition.CreateTranslation(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    Guid.Parse(reader.GetString(2)),
                    decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                    DateTime.Parse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
        }

        return Task.FromResult<IReadOnlyList<FloorPlanArtifactPosition>>(items);
    }

    public Task UpsertAsync(FloorPlanArtifactPosition position, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO floorplan_artifact_positions (
                floorplan_curation_id,
                source_artifact_kind,
                source_artifact_id,
                position_mode,
                resolved_x,
                resolved_y,
                translation_dx,
                translation_dy,
                updated_at_utc
            )
            VALUES (
                $floorplan_curation_id,
                $source_artifact_kind,
                $source_artifact_id,
                $position_mode,
                $resolved_x,
                $resolved_y,
                $translation_dx,
                $translation_dy,
                $updated_at_utc
            )
            ON CONFLICT(floorplan_curation_id, source_artifact_kind, source_artifact_id)
            DO UPDATE SET
                position_mode = excluded.position_mode,
                resolved_x = excluded.resolved_x,
                resolved_y = excluded.resolved_y,
                translation_dx = excluded.translation_dx,
                translation_dy = excluded.translation_dy,
                updated_at_utc = excluded.updated_at_utc
            """);

        command.Parameters.AddWithValue("$floorplan_curation_id", position.FloorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$source_artifact_kind", position.SourceArtifactKind);
        command.Parameters.AddWithValue("$source_artifact_id", position.SourceArtifactId.ToString());
        command.Parameters.AddWithValue("$position_mode", (int)position.PositionMode);
        command.Parameters.AddWithValue("$resolved_x", position.ResolvedX is null ? DBNull.Value : position.ResolvedX.Value.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$resolved_y", position.ResolvedY is null ? DBNull.Value : position.ResolvedY.Value.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$translation_dx", position.TranslationDx is null ? DBNull.Value : position.TranslationDx.Value.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$translation_dy", position.TranslationDy is null ? DBNull.Value : position.TranslationDy.Value.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$updated_at_utc", position.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
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
