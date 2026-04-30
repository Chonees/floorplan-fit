using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteCuratedWallRepository : ICuratedWallRepository
{
    private readonly SqliteSession session;

    public SqliteCuratedWallRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(CuratedWall wall, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO curated_walls (
                id,
                floorplan_curation_id,
                stable_wall_id,
                source_candidate_id,
                source_entity_ref,
                geometry_path_id,
                wall_role,
                mobility_level,
                protection_level,
                thickness_mm,
                assembly_code,
                height_mm,
                is_exterior,
                is_structural_hint,
                wall_group_id,
                sort_order,
                notes)
            VALUES (
                $id,
                $floorplan_curation_id,
                $stable_wall_id,
                $source_candidate_id,
                $source_entity_ref,
                $geometry_path_id,
                $wall_role,
                $mobility_level,
                $protection_level,
                $thickness_mm,
                $assembly_code,
                $height_mm,
                $is_exterior,
                $is_structural_hint,
                $wall_group_id,
                $sort_order,
                $notes)
            """);

        BindWallParameters(command, wall);
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<CuratedWall?> GetByIdAsync(Guid curatedWallId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                id,
                floorplan_curation_id,
                stable_wall_id,
                source_candidate_id,
                source_entity_ref,
                geometry_path_id,
                wall_role,
                mobility_level,
                protection_level,
                thickness_mm,
                assembly_code,
                height_mm,
                is_exterior,
                is_structural_hint,
                wall_group_id,
                sort_order,
                notes
            FROM curated_walls
            WHERE id = $id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$id", curatedWallId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<CuratedWall?>(null);
        }

        return Task.FromResult<CuratedWall?>(MapWall(reader));
    }

    public Task<IReadOnlyList<CuratedWall>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                id,
                floorplan_curation_id,
                stable_wall_id,
                source_candidate_id,
                source_entity_ref,
                geometry_path_id,
                wall_role,
                mobility_level,
                protection_level,
                thickness_mm,
                assembly_code,
                height_mm,
                is_exterior,
                is_structural_hint,
                wall_group_id,
                sort_order,
                notes
            FROM curated_walls
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY sort_order ASC, stable_wall_id ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());

        var items = new List<CuratedWall>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(MapWall(reader));
        }

        return Task.FromResult<IReadOnlyList<CuratedWall>>(items);
    }

    public Task RemoveBySourceCandidateAsync(Guid curationId, Guid sourceCandidateId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            DELETE FROM curated_walls
            WHERE floorplan_curation_id = $floorplan_curation_id
              AND source_candidate_id = $source_candidate_id
            """);

        command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());
        command.Parameters.AddWithValue("$source_candidate_id", sourceCandidateId.ToString());
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task UpdateAsync(CuratedWall wall, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            UPDATE curated_walls
            SET wall_role = $wall_role,
                mobility_level = $mobility_level,
                protection_level = $protection_level,
                thickness_mm = $thickness_mm,
                assembly_code = $assembly_code,
                height_mm = $height_mm,
                is_exterior = $is_exterior,
                is_structural_hint = $is_structural_hint,
                notes = $notes
            WHERE id = $id
            """);

        command.Parameters.AddWithValue("$id", wall.Id.ToString());
        command.Parameters.AddWithValue("$wall_role", (int)wall.WallRole);
        command.Parameters.AddWithValue("$mobility_level", (int)wall.MobilityLevel);
        command.Parameters.AddWithValue("$protection_level", (int)wall.ProtectionLevel);
        command.Parameters.AddWithValue("$thickness_mm", (object?)wall.ThicknessMm?.ToString(CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$assembly_code", (object?)wall.AssemblyCode ?? DBNull.Value);
        command.Parameters.AddWithValue("$height_mm", (object?)wall.HeightMm?.ToString(CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$is_exterior", wall.IsExterior ? 1 : 0);
        command.Parameters.AddWithValue("$is_structural_hint", wall.IsStructuralHint ? 1 : 0);
        command.Parameters.AddWithValue("$notes", (object?)wall.Notes ?? DBNull.Value);
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private static CuratedWall MapWall(SqliteDataReader reader)
    {
        return new CuratedWall(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : Guid.Parse(reader.GetString(3)),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : Guid.Parse(reader.GetString(5)),
            (WallRole)reader.GetInt32(6),
            (WallMobilityLevel)reader.GetInt32(7),
            (WallProtectionLevel)reader.GetInt32(8),
            reader.IsDBNull(9) ? null : decimal.Parse(reader.GetString(9), CultureInfo.InvariantCulture),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.IsDBNull(11) ? null : decimal.Parse(reader.GetString(11), CultureInfo.InvariantCulture),
            reader.GetInt32(12) == 1,
            reader.GetInt32(13) == 1,
            reader.IsDBNull(14) ? null : Guid.Parse(reader.GetString(14)),
            reader.GetInt32(15),
            reader.IsDBNull(16) ? null : reader.GetString(16));
    }

    private static void BindWallParameters(SqliteCommand command, CuratedWall wall)
    {
        command.Parameters.AddWithValue("$id", wall.Id.ToString());
        command.Parameters.AddWithValue("$floorplan_curation_id", wall.FloorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$stable_wall_id", wall.StableWallId);
        command.Parameters.AddWithValue("$source_candidate_id", (object?)wall.SourceCandidateId?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("$source_entity_ref", (object?)wall.SourceEntityRef ?? DBNull.Value);
        command.Parameters.AddWithValue("$geometry_path_id", (object?)wall.GeometryPathId?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("$wall_role", (int)wall.WallRole);
        command.Parameters.AddWithValue("$mobility_level", (int)wall.MobilityLevel);
        command.Parameters.AddWithValue("$protection_level", (int)wall.ProtectionLevel);
        command.Parameters.AddWithValue("$thickness_mm", (object?)wall.ThicknessMm?.ToString(CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$assembly_code", (object?)wall.AssemblyCode ?? DBNull.Value);
        command.Parameters.AddWithValue("$height_mm", (object?)wall.HeightMm?.ToString(CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$is_exterior", wall.IsExterior ? 1 : 0);
        command.Parameters.AddWithValue("$is_structural_hint", wall.IsStructuralHint ? 1 : 0);
        command.Parameters.AddWithValue("$wall_group_id", (object?)wall.WallGroupId?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("$sort_order", wall.SortOrder);
        command.Parameters.AddWithValue("$notes", (object?)wall.Notes ?? DBNull.Value);
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
