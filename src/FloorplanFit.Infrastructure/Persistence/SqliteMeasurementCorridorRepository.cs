using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteMeasurementCorridorRepository : IMeasurementCorridorRepository
{
    private readonly SqliteSession session;

    public SqliteMeasurementCorridorRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(MeasurementCorridor corridor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO measurement_corridors (
                id,
                floorplan_curation_id,
                name,
                axis_tag,
                guide_geometry_path_id,
                band_min_coordinate,
                band_max_coordinate,
                status,
                sort_order
            )
            VALUES (
                $id,
                $floorplan_curation_id,
                $name,
                $axis_tag,
                $guide_geometry_path_id,
                $band_min_coordinate,
                $band_max_coordinate,
                $status,
                $sort_order
            )
            """);
        command.Parameters.AddWithValue("$id", corridor.Id.ToString());
        command.Parameters.AddWithValue("$floorplan_curation_id", corridor.FloorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$name", corridor.Name);
        command.Parameters.AddWithValue("$axis_tag", (int)corridor.AxisTag);
        command.Parameters.AddWithValue("$guide_geometry_path_id", corridor.GuideGeometryPathId.ToString());
        command.Parameters.AddWithValue("$band_min_coordinate", corridor.BandMinCoordinate.ToString(System.Globalization.CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$band_max_coordinate", corridor.BandMaxCoordinate.ToString(System.Globalization.CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$status", corridor.Status);
        command.Parameters.AddWithValue("$sort_order", corridor.SortOrder);
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid corridorId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            DELETE FROM measurement_corridors
            WHERE id = $id
            """);
        command.Parameters.AddWithValue("$id", corridorId.ToString());
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task<MeasurementCorridor?> GetByIdAsync(Guid corridorId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                id,
                floorplan_curation_id,
                name,
                axis_tag,
                guide_geometry_path_id,
                band_min_coordinate,
                band_max_coordinate,
                status,
                sort_order
            FROM measurement_corridors
            WHERE id = $id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$id", corridorId.ToString());

        using var reader = command.ExecuteReader();
        return Task.FromResult(reader.Read() ? Map(reader) : null);
    }

    public Task<IReadOnlyList<MeasurementCorridor>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var command = CreateCommand(
            """
            SELECT
                id,
                floorplan_curation_id,
                name,
                axis_tag,
                guide_geometry_path_id,
                band_min_coordinate,
                band_max_coordinate,
                status,
                sort_order
            FROM measurement_corridors
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY sort_order ASC, name ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());

        using var reader = command.ExecuteReader();
        var items = new List<MeasurementCorridor>();
        while (reader.Read())
        {
            items.Add(Map(reader));
        }

        return Task.FromResult<IReadOnlyList<MeasurementCorridor>>(items);
    }

    private static MeasurementCorridor Map(SqliteDataReader reader)
    {
        return new MeasurementCorridor(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            (PinchAxisTag)reader.GetInt32(3),
            Guid.Parse(reader.GetString(4)),
            decimal.Parse(reader.GetString(5), System.Globalization.CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(6), System.Globalization.CultureInfo.InvariantCulture),
            reader.GetString(7),
            reader.GetInt32(8));
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
