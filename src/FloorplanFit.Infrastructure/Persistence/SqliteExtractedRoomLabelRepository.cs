using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteExtractedRoomLabelRepository : IExtractedRoomLabelRepository
{
    private readonly SqliteSession session;

    public SqliteExtractedRoomLabelRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddRangeAsync(IReadOnlyList<ExtractedRoomLabel> labels, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var label in labels)
        {
            using var command = CreateCommand(
                """
                INSERT INTO extracted_room_labels (
                    id,
                    wall_extraction_run_id,
                    source_entity_ref,
                    source_layer,
                    text,
                    x,
                    y,
                    confidence,
                    detection_notes,
                    sort_order,
                    source_entity_kind,
                    text_height,
                    rotation_degrees,
                    text_style_name,
                    horizontal_alignment,
                    vertical_alignment,
                    attachment_point,
                    color_argb)
                VALUES (
                    $id,
                    $wall_extraction_run_id,
                    $source_entity_ref,
                    $source_layer,
                    $text,
                    $x,
                    $y,
                    $confidence,
                    $detection_notes,
                    $sort_order,
                    $source_entity_kind,
                    $text_height,
                    $rotation_degrees,
                    $text_style_name,
                    $horizontal_alignment,
                    $vertical_alignment,
                    $attachment_point,
                    $color_argb)
                """);

            command.Parameters.AddWithValue("$id", label.Id.ToString());
            command.Parameters.AddWithValue("$wall_extraction_run_id", label.WallExtractionRunId.ToString());
            command.Parameters.AddWithValue("$source_entity_ref", label.SourceEntityRef);
            command.Parameters.AddWithValue("$source_layer", (object?)label.SourceLayer ?? DBNull.Value);
            command.Parameters.AddWithValue("$text", label.Text);
            command.Parameters.AddWithValue("$x", label.X.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$y", label.Y.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$confidence", label.Confidence.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$detection_notes", (object?)label.DetectionNotes ?? DBNull.Value);
            command.Parameters.AddWithValue("$sort_order", label.SortOrder);
            command.Parameters.AddWithValue("$source_entity_kind", (object?)label.SourceEntityKind ?? DBNull.Value);
            command.Parameters.AddWithValue("$text_height", label.TextHeight is null ? DBNull.Value : label.TextHeight.Value.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$rotation_degrees", label.RotationDegrees.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$text_style_name", (object?)label.TextStyleName ?? DBNull.Value);
            command.Parameters.AddWithValue("$horizontal_alignment", (object?)label.HorizontalAlignment ?? DBNull.Value);
            command.Parameters.AddWithValue("$vertical_alignment", (object?)label.VerticalAlignment ?? DBNull.Value);
            command.Parameters.AddWithValue("$attachment_point", (object?)label.AttachmentPoint ?? DBNull.Value);
            command.Parameters.AddWithValue("$color_argb", (object?)label.ColorArgb ?? DBNull.Value);
            command.ExecuteNonQuery();
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ExtractedRoomLabel>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                id,
                wall_extraction_run_id,
                source_entity_ref,
                source_layer,
                text,
                x,
                y,
                confidence,
                detection_notes,
                sort_order,
                source_entity_kind,
                text_height,
                rotation_degrees,
                text_style_name,
                horizontal_alignment,
                vertical_alignment,
                attachment_point,
                color_argb
            FROM extracted_room_labels
            WHERE wall_extraction_run_id = $wall_extraction_run_id
            ORDER BY sort_order ASC, id ASC
            """);
        command.Parameters.AddWithValue("$wall_extraction_run_id", wallExtractionRunId.ToString());

        var labels = new List<ExtractedRoomLabel>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            labels.Add(new ExtractedRoomLabel(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetString(4),
                decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.GetInt32(9),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.IsDBNull(11) ? null : decimal.Parse(reader.GetString(11), CultureInfo.InvariantCulture),
                reader.IsDBNull(12) ? 0m : decimal.Parse(reader.GetString(12), CultureInfo.InvariantCulture),
                reader.IsDBNull(13) ? null : reader.GetString(13),
                reader.IsDBNull(14) ? null : reader.GetString(14),
                reader.IsDBNull(15) ? null : reader.GetString(15),
                reader.IsDBNull(16) ? null : reader.GetString(16),
                reader.IsDBNull(17) ? null : reader.GetString(17)));
        }

        return Task.FromResult<IReadOnlyList<ExtractedRoomLabel>>(labels);
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
