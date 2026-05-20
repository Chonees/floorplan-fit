using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteFloorPlanDimensionOverrideRepository : IFloorPlanDimensionOverrideRepository
{
    private readonly SqliteSession session;

    public SqliteFloorPlanDimensionOverrideRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task<IReadOnlyList<FloorPlanDimensionOverride>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                floorplan_curation_id,
                source_dimension_key,
                source_entity_ref,
                source_handle,
                display_text,
                def_point_x,
                def_point_y,
                def_point_z,
                def_point2_x,
                def_point2_y,
                def_point2_z,
                def_point3_x,
                def_point3_y,
                def_point3_z,
                render_text_x,
                render_text_y,
                render_text_height,
                render_text_rotation_degrees,
                render_text_style_name,
                render_text_horizontal_alignment,
                render_text_vertical_alignment,
                render_text_attachment_point,
                updated_at_utc,
                last_exported_at_utc
            FROM floorplan_dimension_overrides
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY updated_at_utc ASC, source_dimension_key ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());

        var items = new List<FloorPlanDimensionOverride>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var sourceDimensionKey = reader.GetString(1);
            items.Add(FloorPlanDimensionOverride.CreateManualSnapshot(
                Guid.Parse(reader.GetString(0)),
                sourceDimensionKey,
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetString(4),
                decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(8), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(9), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(10), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(11), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(12), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(13), CultureInfo.InvariantCulture),
                ParseNullableDecimal(reader, 14),
                ParseNullableDecimal(reader, 15),
                ParseNullableDecimal(reader, 16),
                ParseNullableDecimal(reader, 17),
                reader.IsDBNull(18) ? null : reader.GetString(18),
                reader.IsDBNull(19) ? null : reader.GetString(19),
                reader.IsDBNull(20) ? null : reader.GetString(20),
                reader.IsDBNull(21) ? null : reader.GetString(21),
                GetLinePrimitives(floorPlanCurationId, sourceDimensionKey),
                GetTextPrimitives(floorPlanCurationId, sourceDimensionKey),
                GetInsertPrimitives(floorPlanCurationId, sourceDimensionKey),
                GetCirclePrimitives(floorPlanCurationId, sourceDimensionKey),
                GetArcPrimitives(floorPlanCurationId, sourceDimensionKey),
                GetSolidPrimitives(floorPlanCurationId, sourceDimensionKey),
                DateTime.Parse(reader.GetString(22), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                reader.IsDBNull(23) ? null : DateTime.Parse(reader.GetString(23), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
        }

        return Task.FromResult<IReadOnlyList<FloorPlanDimensionOverride>>(items);
    }

    public Task UpsertAsync(FloorPlanDimensionOverride dimensionOverride, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO floorplan_dimension_overrides (
                floorplan_curation_id,
                source_dimension_key,
                source_entity_ref,
                source_handle,
                display_text,
                def_point_x,
                def_point_y,
                def_point_z,
                def_point2_x,
                def_point2_y,
                def_point2_z,
                def_point3_x,
                def_point3_y,
                def_point3_z,
                render_text_x,
                render_text_y,
                render_text_height,
                render_text_rotation_degrees,
                render_text_style_name,
                render_text_horizontal_alignment,
                render_text_vertical_alignment,
                render_text_attachment_point,
                updated_at_utc,
                last_exported_at_utc
            )
            VALUES (
                $floorplan_curation_id,
                $source_dimension_key,
                $source_entity_ref,
                $source_handle,
                $display_text,
                $def_point_x,
                $def_point_y,
                $def_point_z,
                $def_point2_x,
                $def_point2_y,
                $def_point2_z,
                $def_point3_x,
                $def_point3_y,
                $def_point3_z,
                $render_text_x,
                $render_text_y,
                $render_text_height,
                $render_text_rotation_degrees,
                $render_text_style_name,
                $render_text_horizontal_alignment,
                $render_text_vertical_alignment,
                $render_text_attachment_point,
                $updated_at_utc,
                $last_exported_at_utc
            )
            ON CONFLICT(floorplan_curation_id, source_dimension_key)
            DO UPDATE SET
                source_entity_ref = excluded.source_entity_ref,
                source_handle = excluded.source_handle,
                display_text = excluded.display_text,
                def_point_x = excluded.def_point_x,
                def_point_y = excluded.def_point_y,
                def_point_z = excluded.def_point_z,
                def_point2_x = excluded.def_point2_x,
                def_point2_y = excluded.def_point2_y,
                def_point2_z = excluded.def_point2_z,
                def_point3_x = excluded.def_point3_x,
                def_point3_y = excluded.def_point3_y,
                def_point3_z = excluded.def_point3_z,
                render_text_x = excluded.render_text_x,
                render_text_y = excluded.render_text_y,
                render_text_height = excluded.render_text_height,
                render_text_rotation_degrees = excluded.render_text_rotation_degrees,
                render_text_style_name = excluded.render_text_style_name,
                render_text_horizontal_alignment = excluded.render_text_horizontal_alignment,
                render_text_vertical_alignment = excluded.render_text_vertical_alignment,
                render_text_attachment_point = excluded.render_text_attachment_point,
                updated_at_utc = excluded.updated_at_utc,
                last_exported_at_utc = excluded.last_exported_at_utc
            """);
        BindMainRow(command, dimensionOverride);
        command.ExecuteNonQuery();

        DeletePrimitiveRows(dimensionOverride.FloorPlanCurationId, dimensionOverride.SourceDimensionKey);
        AddPrimitives(dimensionOverride);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid floorPlanCurationId, string sourceDimensionKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DeletePrimitiveRows(floorPlanCurationId, sourceDimensionKey);

        using var command = CreateCommand(
            """
            DELETE FROM floorplan_dimension_overrides
            WHERE floorplan_curation_id = $floorplan_curation_id
              AND source_dimension_key = $source_dimension_key
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$source_dimension_key", sourceDimensionKey);
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task MarkExportedAsync(Guid floorPlanCurationId, IReadOnlyList<string> sourceDimensionKeys, DateTime exportedAtUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var sourceDimensionKey in sourceDimensionKeys.Distinct(StringComparer.Ordinal))
        {
            using var command = CreateCommand(
                """
                UPDATE floorplan_dimension_overrides
                SET last_exported_at_utc = $last_exported_at_utc
                WHERE floorplan_curation_id = $floorplan_curation_id
                  AND source_dimension_key = $source_dimension_key
                """);
            command.Parameters.AddWithValue("$last_exported_at_utc", exportedAtUtc.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());
            command.Parameters.AddWithValue("$source_dimension_key", sourceDimensionKey);
            command.ExecuteNonQuery();
        }

        return Task.CompletedTask;
    }

    private void BindMainRow(SqliteCommand command, FloorPlanDimensionOverride dimensionOverride)
    {
        command.Parameters.AddWithValue("$floorplan_curation_id", dimensionOverride.FloorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$source_dimension_key", dimensionOverride.SourceDimensionKey);
        command.Parameters.AddWithValue("$source_entity_ref", dimensionOverride.SourceEntityRef);
        command.Parameters.AddWithValue("$source_handle", (object?)dimensionOverride.SourceHandle ?? DBNull.Value);
        command.Parameters.AddWithValue("$display_text", dimensionOverride.DisplayText);
        command.Parameters.AddWithValue("$def_point_x", dimensionOverride.DefPointX.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$def_point_y", dimensionOverride.DefPointY.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$def_point_z", dimensionOverride.DefPointZ.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$def_point2_x", dimensionOverride.DefPoint2X.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$def_point2_y", dimensionOverride.DefPoint2Y.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$def_point2_z", dimensionOverride.DefPoint2Z.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$def_point3_x", dimensionOverride.DefPoint3X.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$def_point3_y", dimensionOverride.DefPoint3Y.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$def_point3_z", dimensionOverride.DefPoint3Z.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$render_text_x", ConvertPrimitiveValue(dimensionOverride.RenderTextX));
        command.Parameters.AddWithValue("$render_text_y", ConvertPrimitiveValue(dimensionOverride.RenderTextY));
        command.Parameters.AddWithValue("$render_text_height", ConvertPrimitiveValue(dimensionOverride.RenderTextHeight));
        command.Parameters.AddWithValue("$render_text_rotation_degrees", ConvertPrimitiveValue(dimensionOverride.RenderTextRotationDegrees));
        command.Parameters.AddWithValue("$render_text_style_name", (object?)dimensionOverride.RenderTextStyleName ?? DBNull.Value);
        command.Parameters.AddWithValue("$render_text_horizontal_alignment", (object?)dimensionOverride.RenderTextHorizontalAlignment ?? DBNull.Value);
        command.Parameters.AddWithValue("$render_text_vertical_alignment", (object?)dimensionOverride.RenderTextVerticalAlignment ?? DBNull.Value);
        command.Parameters.AddWithValue("$render_text_attachment_point", (object?)dimensionOverride.RenderTextAttachmentPoint ?? DBNull.Value);
        command.Parameters.AddWithValue("$updated_at_utc", dimensionOverride.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$last_exported_at_utc", dimensionOverride.LastExportedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
    }

    private void DeletePrimitiveRows(Guid floorPlanCurationId, string sourceDimensionKey)
    {
        using var command = CreateCommand(
            """
            DELETE FROM floorplan_dimension_override_primitives
            WHERE floorplan_curation_id = $floorplan_curation_id
              AND source_dimension_key = $source_dimension_key
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$source_dimension_key", sourceDimensionKey);
        command.ExecuteNonQuery();
    }

    private void AddPrimitives(FloorPlanDimensionOverride dimensionOverride)
    {
        foreach (var primitive in dimensionOverride.LinePrimitives)
        {
            InsertPrimitive(dimensionOverride, primitive.PrimitiveKey, "LINE", primitive.SortOrder, primitive.SourceHandle, primitive.SourceLayer,
                ("$start_x", primitive.StartX), ("$start_y", primitive.StartY), ("$end_x", primitive.EndX), ("$end_y", primitive.EndY));
        }

        foreach (var primitive in dimensionOverride.TextPrimitives)
        {
            InsertPrimitive(dimensionOverride, primitive.PrimitiveKey, "TEXT", primitive.SortOrder, primitive.SourceHandle, primitive.SourceLayer,
                ("$text_value", primitive.Text), ("$x", primitive.X), ("$y", primitive.Y), ("$height", primitive.Height), ("$rotation_degrees", primitive.RotationDegrees),
                ("$style_name", primitive.StyleName), ("$horizontal_alignment", primitive.HorizontalAlignment), ("$vertical_alignment", primitive.VerticalAlignment), ("$attachment_point", primitive.AttachmentPoint));
        }

        foreach (var primitive in dimensionOverride.InsertPrimitives)
        {
            InsertPrimitive(dimensionOverride, primitive.PrimitiveKey, "INSERT", primitive.SortOrder, primitive.SourceHandle, primitive.SourceLayer,
                ("$insert_name", primitive.Name), ("$x", primitive.X), ("$y", primitive.Y), ("$z", primitive.Z), ("$rotation_degrees", primitive.RotationDegrees),
                ("$scale_x", primitive.ScaleX), ("$scale_y", primitive.ScaleY), ("$scale_z", primitive.ScaleZ));
        }

        foreach (var primitive in dimensionOverride.CirclePrimitives)
        {
            InsertPrimitive(dimensionOverride, primitive.PrimitiveKey, "CIRCLE", primitive.SortOrder, primitive.SourceHandle, primitive.SourceLayer,
                ("$center_x", primitive.CenterX), ("$center_y", primitive.CenterY), ("$radius", primitive.Radius));
        }

        foreach (var primitive in dimensionOverride.ArcPrimitives)
        {
            InsertPrimitive(dimensionOverride, primitive.PrimitiveKey, "ARC", primitive.SortOrder, primitive.SourceHandle, primitive.SourceLayer,
                ("$center_x", primitive.CenterX), ("$center_y", primitive.CenterY), ("$radius", primitive.Radius), ("$start_angle_degrees", primitive.StartAngleDegrees), ("$end_angle_degrees", primitive.EndAngleDegrees));
        }

        foreach (var primitive in dimensionOverride.SolidPrimitives)
        {
            InsertPrimitive(dimensionOverride, primitive.PrimitiveKey, "SOLID", primitive.SortOrder, primitive.SourceHandle, primitive.SourceLayer,
                ("$point1_x", primitive.Point1X), ("$point1_y", primitive.Point1Y), ("$point2_x", primitive.Point2X), ("$point2_y", primitive.Point2Y),
                ("$point3_x", primitive.Point3X), ("$point3_y", primitive.Point3Y), ("$point4_x", primitive.Point4X), ("$point4_y", primitive.Point4Y));
        }
    }

    private void InsertPrimitive(
        FloorPlanDimensionOverride dimensionOverride,
        string primitiveKey,
        string primitiveKind,
        int sortOrder,
        string? sourceHandle,
        string? sourceLayer,
        params (string Name, object? Value)[] values)
    {
        using var command = CreateCommand(
            """
            INSERT INTO floorplan_dimension_override_primitives (
                floorplan_curation_id,
                source_dimension_key,
                primitive_key,
                primitive_kind,
                sort_order,
                source_handle,
                source_layer,
                start_x,
                start_y,
                end_x,
                end_y,
                text_value,
                x,
                y,
                z,
                height,
                rotation_degrees,
                style_name,
                horizontal_alignment,
                vertical_alignment,
                attachment_point,
                insert_name,
                scale_x,
                scale_y,
                scale_z,
                center_x,
                center_y,
                radius,
                start_angle_degrees,
                end_angle_degrees,
                point1_x,
                point1_y,
                point2_x,
                point2_y,
                point3_x,
                point3_y,
                point4_x,
                point4_y)
            VALUES (
                $floorplan_curation_id,
                $source_dimension_key,
                $primitive_key,
                $primitive_kind,
                $sort_order,
                $source_handle,
                $source_layer,
                $start_x,
                $start_y,
                $end_x,
                $end_y,
                $text_value,
                $x,
                $y,
                $z,
                $height,
                $rotation_degrees,
                $style_name,
                $horizontal_alignment,
                $vertical_alignment,
                $attachment_point,
                $insert_name,
                $scale_x,
                $scale_y,
                $scale_z,
                $center_x,
                $center_y,
                $radius,
                $start_angle_degrees,
                $end_angle_degrees,
                $point1_x,
                $point1_y,
                $point2_x,
                $point2_y,
                $point3_x,
                $point3_y,
                $point4_x,
                $point4_y)
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", dimensionOverride.FloorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$source_dimension_key", dimensionOverride.SourceDimensionKey);
        command.Parameters.AddWithValue("$primitive_key", primitiveKey);
        command.Parameters.AddWithValue("$primitive_kind", primitiveKind);
        command.Parameters.AddWithValue("$sort_order", sortOrder);
        command.Parameters.AddWithValue("$source_handle", (object?)sourceHandle ?? DBNull.Value);
        command.Parameters.AddWithValue("$source_layer", (object?)sourceLayer ?? DBNull.Value);

        foreach (var parameterName in PrimitiveParameterNames)
        {
            command.Parameters.AddWithValue(parameterName, DBNull.Value);
        }

        foreach (var (name, value) in values)
        {
            command.Parameters[name].Value = ConvertPrimitiveValue(value);
        }

        command.ExecuteNonQuery();
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }

    private IReadOnlyList<ExtractedDimensionLinePrimitive> GetLinePrimitives(Guid floorPlanCurationId, string sourceDimensionKey)
        => ListPrimitives(
            floorPlanCurationId,
            sourceDimensionKey,
            "LINE",
            reader => new ExtractedDimensionLinePrimitive(
                reader.GetString(0),
                reader.GetInt32(1),
                ParseNullableDecimal(reader, 4) ?? 0m,
                ParseNullableDecimal(reader, 5) ?? 0m,
                ParseNullableDecimal(reader, 6) ?? 0m,
                ParseNullableDecimal(reader, 7) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3)
            });

    private IReadOnlyList<ExtractedDimensionTextPrimitive> GetTextPrimitives(Guid floorPlanCurationId, string sourceDimensionKey)
        => ListPrimitives(
            floorPlanCurationId,
            sourceDimensionKey,
            "TEXT",
            reader => new ExtractedDimensionTextPrimitive(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                ParseNullableDecimal(reader, 9) ?? 0m,
                ParseNullableDecimal(reader, 10) ?? 0m,
                ParseNullableDecimal(reader, 12) ?? 0m,
                ParseNullableDecimal(reader, 13) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3),
                StyleName = reader.IsDBNull(14) ? null : reader.GetString(14),
                HorizontalAlignment = reader.IsDBNull(15) ? null : reader.GetString(15),
                VerticalAlignment = reader.IsDBNull(16) ? null : reader.GetString(16),
                AttachmentPoint = reader.IsDBNull(17) ? null : reader.GetString(17)
            });

    private IReadOnlyList<ExtractedDimensionInsertPrimitive> GetInsertPrimitives(Guid floorPlanCurationId, string sourceDimensionKey)
        => ListPrimitives(
            floorPlanCurationId,
            sourceDimensionKey,
            "INSERT",
            reader => new ExtractedDimensionInsertPrimitive(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.IsDBNull(18) ? string.Empty : reader.GetString(18),
                ParseNullableDecimal(reader, 9) ?? 0m,
                ParseNullableDecimal(reader, 10) ?? 0m,
                ParseNullableDecimal(reader, 11) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3),
                RotationDegrees = ParseNullableDecimal(reader, 13) ?? 0m,
                ScaleX = ParseNullableDecimal(reader, 19) ?? 1m,
                ScaleY = ParseNullableDecimal(reader, 20) ?? 1m,
                ScaleZ = ParseNullableDecimal(reader, 21) ?? 1m
            });

    private IReadOnlyList<ExtractedDimensionCirclePrimitive> GetCirclePrimitives(Guid floorPlanCurationId, string sourceDimensionKey)
        => ListPrimitives(
            floorPlanCurationId,
            sourceDimensionKey,
            "CIRCLE",
            reader => new ExtractedDimensionCirclePrimitive(
                reader.GetString(0),
                reader.GetInt32(1),
                ParseNullableDecimal(reader, 22) ?? 0m,
                ParseNullableDecimal(reader, 23) ?? 0m,
                ParseNullableDecimal(reader, 24) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3)
            });

    private IReadOnlyList<ExtractedDimensionArcPrimitive> GetArcPrimitives(Guid floorPlanCurationId, string sourceDimensionKey)
        => ListPrimitives(
            floorPlanCurationId,
            sourceDimensionKey,
            "ARC",
            reader => new ExtractedDimensionArcPrimitive(
                reader.GetString(0),
                reader.GetInt32(1),
                ParseNullableDecimal(reader, 22) ?? 0m,
                ParseNullableDecimal(reader, 23) ?? 0m,
                ParseNullableDecimal(reader, 24) ?? 0m,
                ParseNullableDecimal(reader, 25) ?? 0m,
                ParseNullableDecimal(reader, 26) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3)
            });

    private IReadOnlyList<ExtractedDimensionSolidPrimitive> GetSolidPrimitives(Guid floorPlanCurationId, string sourceDimensionKey)
        => ListPrimitives(
            floorPlanCurationId,
            sourceDimensionKey,
            "SOLID",
            reader => new ExtractedDimensionSolidPrimitive(
                reader.GetString(0),
                reader.GetInt32(1),
                ParseNullableDecimal(reader, 27) ?? 0m,
                ParseNullableDecimal(reader, 28) ?? 0m,
                ParseNullableDecimal(reader, 29) ?? 0m,
                ParseNullableDecimal(reader, 30) ?? 0m,
                ParseNullableDecimal(reader, 31) ?? 0m,
                ParseNullableDecimal(reader, 32) ?? 0m,
                ParseNullableDecimal(reader, 33) ?? 0m,
                ParseNullableDecimal(reader, 34) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3)
            });

    private IReadOnlyList<T> ListPrimitives<T>(Guid floorPlanCurationId, string sourceDimensionKey, string primitiveKind, Func<SqliteDataReader, T> map)
    {
        using var command = CreateCommand(
            """
            SELECT
                primitive_key,
                sort_order,
                source_handle,
                source_layer,
                start_x,
                start_y,
                end_x,
                end_y,
                text_value,
                x,
                y,
                z,
                height,
                rotation_degrees,
                style_name,
                horizontal_alignment,
                vertical_alignment,
                attachment_point,
                insert_name,
                scale_x,
                scale_y,
                scale_z,
                center_x,
                center_y,
                radius,
                start_angle_degrees,
                end_angle_degrees,
                point1_x,
                point1_y,
                point2_x,
                point2_y,
                point3_x,
                point3_y,
                point4_x,
                point4_y
            FROM floorplan_dimension_override_primitives
            WHERE floorplan_curation_id = $floorplan_curation_id
              AND source_dimension_key = $source_dimension_key
              AND primitive_kind = $primitive_kind
            ORDER BY sort_order ASC, primitive_key ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$source_dimension_key", sourceDimensionKey);
        command.Parameters.AddWithValue("$primitive_kind", primitiveKind);

        var items = new List<T>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(map(reader));
        }

        return items;
    }

    private static decimal? ParseNullableDecimal(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : decimal.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture);

    private static object ConvertPrimitiveValue(object? value)
    {
        return value switch
        {
            null => DBNull.Value,
            decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),
            _ => value
        };
    }

    private static readonly string[] PrimitiveParameterNames =
    [
        "$start_x",
        "$start_y",
        "$end_x",
        "$end_y",
        "$text_value",
        "$x",
        "$y",
        "$z",
        "$height",
        "$rotation_degrees",
        "$style_name",
        "$horizontal_alignment",
        "$vertical_alignment",
        "$attachment_point",
        "$insert_name",
        "$scale_x",
        "$scale_y",
        "$scale_z",
        "$center_x",
        "$center_y",
        "$radius",
        "$start_angle_degrees",
        "$end_angle_degrees",
        "$point1_x",
        "$point1_y",
        "$point2_x",
        "$point2_y",
        "$point3_x",
        "$point3_y",
        "$point4_x",
        "$point4_y"
    ];
}
