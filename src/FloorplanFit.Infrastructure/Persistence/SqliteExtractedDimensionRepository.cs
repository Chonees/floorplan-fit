using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteExtractedDimensionRepository : IExtractedDimensionRepository
{
    private readonly SqliteSession session;

    public SqliteExtractedDimensionRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddRangeAsync(IReadOnlyList<ExtractedDimension> dimensions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var dimension in dimensions)
        {
            using var command = CreateCommand(
                """
                INSERT INTO extracted_dimensions (
                    id,
                    wall_extraction_run_id,
                    source_entity_ref,
                    source_handle,
                    source_layer,
                    source_entity_kind,
                    geometry_block_name,
                    display_text,
                    display_text_source,
                    raw_text_override,
                    measurement_source_units,
                    measurement_millimeters,
                    source_unit,
                    dim_type,
                    angle,
                    oblique_angle,
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
                    confidence,
                    detection_notes,
                    sort_order)
                VALUES (
                    $id,
                    $wall_extraction_run_id,
                    $source_entity_ref,
                    $source_handle,
                    $source_layer,
                    $source_entity_kind,
                    $geometry_block_name,
                    $display_text,
                    $display_text_source,
                    $raw_text_override,
                    $measurement_source_units,
                    $measurement_millimeters,
                    $source_unit,
                    $dim_type,
                    $angle,
                    $oblique_angle,
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
                    $confidence,
                    $detection_notes,
                    $sort_order)
                """);

            command.Parameters.AddWithValue("$id", dimension.Id.ToString());
            command.Parameters.AddWithValue("$wall_extraction_run_id", dimension.WallExtractionRunId.ToString());
            command.Parameters.AddWithValue("$source_entity_ref", dimension.SourceEntityRef);
            command.Parameters.AddWithValue("$source_handle", (object?)dimension.SourceHandle ?? DBNull.Value);
            command.Parameters.AddWithValue("$source_layer", (object?)dimension.SourceLayer ?? DBNull.Value);
            command.Parameters.AddWithValue("$source_entity_kind", dimension.SourceEntityKind);
            command.Parameters.AddWithValue("$geometry_block_name", (object?)dimension.GeometryBlockName ?? DBNull.Value);
            command.Parameters.AddWithValue("$display_text", dimension.DisplayText);
            command.Parameters.AddWithValue("$display_text_source", dimension.DisplayTextSource);
            command.Parameters.AddWithValue("$raw_text_override", dimension.RawTextOverride);
            command.Parameters.AddWithValue("$measurement_source_units", dimension.MeasurementSourceUnits.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$measurement_millimeters", dimension.MeasurementMillimeters.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$source_unit", dimension.SourceUnit);
            command.Parameters.AddWithValue("$dim_type", dimension.DimType);
            command.Parameters.AddWithValue("$angle", dimension.Angle.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$oblique_angle", dimension.ObliqueAngle.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$def_point_x", dimension.DefPointX.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$def_point_y", dimension.DefPointY.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$def_point_z", dimension.DefPointZ.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$def_point2_x", dimension.DefPoint2X.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$def_point2_y", dimension.DefPoint2Y.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$def_point2_z", dimension.DefPoint2Z.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$def_point3_x", dimension.DefPoint3X.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$def_point3_y", dimension.DefPoint3Y.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$def_point3_z", dimension.DefPoint3Z.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$render_text_x", dimension.RenderTextX?.ToString(CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$render_text_y", dimension.RenderTextY?.ToString(CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$render_text_height", dimension.RenderTextHeight?.ToString(CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$render_text_rotation_degrees", dimension.RenderTextRotationDegrees?.ToString(CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$render_text_style_name", (object?)dimension.RenderTextStyleName ?? DBNull.Value);
            command.Parameters.AddWithValue("$render_text_horizontal_alignment", (object?)dimension.RenderTextHorizontalAlignment ?? DBNull.Value);
            command.Parameters.AddWithValue("$render_text_vertical_alignment", (object?)dimension.RenderTextVerticalAlignment ?? DBNull.Value);
            command.Parameters.AddWithValue("$render_text_attachment_point", (object?)dimension.RenderTextAttachmentPoint ?? DBNull.Value);
            command.Parameters.AddWithValue("$confidence", dimension.Confidence.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$detection_notes", (object?)dimension.DetectionNotes ?? DBNull.Value);
            command.Parameters.AddWithValue("$sort_order", dimension.SortOrder);
            command.ExecuteNonQuery();

            for (var index = 0; index < dimension.LineSegments.Count; index++)
            {
                var segment = dimension.LineSegments[index];
                using var segmentCommand = CreateCommand(
                    """
                    INSERT INTO extracted_dimension_line_segments (
                        dimension_id,
                        sort_order,
                        start_x,
                        start_y,
                        end_x,
                        end_y)
                    VALUES (
                        $dimension_id,
                        $sort_order,
                        $start_x,
                        $start_y,
                        $end_x,
                        $end_y)
                    """);
                segmentCommand.Parameters.AddWithValue("$dimension_id", dimension.Id.ToString());
                segmentCommand.Parameters.AddWithValue("$sort_order", index + 1);
                segmentCommand.Parameters.AddWithValue("$start_x", segment.StartX.ToString(CultureInfo.InvariantCulture));
                segmentCommand.Parameters.AddWithValue("$start_y", segment.StartY.ToString(CultureInfo.InvariantCulture));
                segmentCommand.Parameters.AddWithValue("$end_x", segment.EndX.ToString(CultureInfo.InvariantCulture));
                segmentCommand.Parameters.AddWithValue("$end_y", segment.EndY.ToString(CultureInfo.InvariantCulture));
                segmentCommand.ExecuteNonQuery();
            }

            AddPrimitives(dimension);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ExtractedDimension>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                id,
                wall_extraction_run_id,
                source_entity_ref,
                source_handle,
                source_layer,
                source_entity_kind,
                geometry_block_name,
                display_text,
                display_text_source,
                raw_text_override,
                measurement_source_units,
                measurement_millimeters,
                source_unit,
                dim_type,
                angle,
                oblique_angle,
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
                confidence,
                detection_notes,
                sort_order
            FROM extracted_dimensions
            WHERE wall_extraction_run_id = $wall_extraction_run_id
            ORDER BY sort_order ASC, id ASC
            """);
        command.Parameters.AddWithValue("$wall_extraction_run_id", wallExtractionRunId.ToString());

        var items = new List<ExtractedDimension>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var dimensionId = Guid.Parse(reader.GetString(0));
            items.Add(new ExtractedDimension(
                dimensionId,
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9),
                decimal.Parse(reader.GetString(10), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(11), CultureInfo.InvariantCulture),
                reader.GetString(12),
                reader.GetInt32(13),
                decimal.Parse(reader.GetString(14), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(15), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(16), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(17), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(18), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(19), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(20), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(21), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(22), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(23), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(24), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(33), CultureInfo.InvariantCulture),
                reader.IsDBNull(34) ? null : reader.GetString(34),
                reader.GetInt32(35),
                renderTextX: reader.IsDBNull(25) ? null : decimal.Parse(reader.GetString(25), CultureInfo.InvariantCulture),
                renderTextY: reader.IsDBNull(26) ? null : decimal.Parse(reader.GetString(26), CultureInfo.InvariantCulture),
                renderTextHeight: reader.IsDBNull(27) ? null : decimal.Parse(reader.GetString(27), CultureInfo.InvariantCulture),
                renderTextRotationDegrees: reader.IsDBNull(28) ? null : decimal.Parse(reader.GetString(28), CultureInfo.InvariantCulture),
                renderTextStyleName: reader.IsDBNull(29) ? null : reader.GetString(29),
                renderTextHorizontalAlignment: reader.IsDBNull(30) ? null : reader.GetString(30),
                renderTextVerticalAlignment: reader.IsDBNull(31) ? null : reader.GetString(31),
                renderTextAttachmentPoint: reader.IsDBNull(32) ? null : reader.GetString(32),
                lineSegments: GetLineSegments(dimensionId),
                sourceHandle: reader.IsDBNull(3) ? null : reader.GetString(3),
                linePrimitives: GetLinePrimitives(dimensionId),
                textPrimitives: GetTextPrimitives(dimensionId),
                insertPrimitives: GetInsertPrimitives(dimensionId),
                circlePrimitives: GetCirclePrimitives(dimensionId),
                arcPrimitives: GetArcPrimitives(dimensionId),
                solidPrimitives: GetSolidPrimitives(dimensionId)));
        }

        return Task.FromResult<IReadOnlyList<ExtractedDimension>>(items);
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }

    private IReadOnlyList<ExtractedDimensionLineSegment> GetLineSegments(Guid dimensionId)
    {
        using var command = CreateCommand(
            """
            SELECT
                start_x,
                start_y,
                end_x,
                end_y
            FROM extracted_dimension_line_segments
            WHERE dimension_id = $dimension_id
            ORDER BY sort_order ASC
            """);
        command.Parameters.AddWithValue("$dimension_id", dimensionId.ToString());

        var items = new List<ExtractedDimensionLineSegment>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new ExtractedDimensionLineSegment(
                decimal.Parse(reader.GetString(0), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture)));
        }

        return items;
    }

    private void AddPrimitives(ExtractedDimension dimension)
    {
        foreach (var primitive in dimension.LinePrimitives)
        {
            InsertPrimitive(
                dimension.Id,
                primitive.PrimitiveKey,
                "LINE",
                primitive.SortOrder,
                primitive.SourceHandle,
                primitive.SourceLayer,
                ("$start_x", primitive.StartX),
                ("$start_y", primitive.StartY),
                ("$end_x", primitive.EndX),
                ("$end_y", primitive.EndY));
        }

        foreach (var primitive in dimension.TextPrimitives)
        {
            InsertPrimitive(
                dimension.Id,
                primitive.PrimitiveKey,
                "TEXT",
                primitive.SortOrder,
                primitive.SourceHandle,
                primitive.SourceLayer,
                ("$text_value", primitive.Text),
                ("$x", primitive.X),
                ("$y", primitive.Y),
                ("$height", primitive.Height),
                ("$rotation_degrees", primitive.RotationDegrees),
                ("$style_name", primitive.StyleName),
                ("$horizontal_alignment", primitive.HorizontalAlignment),
                ("$vertical_alignment", primitive.VerticalAlignment),
                ("$attachment_point", primitive.AttachmentPoint));
        }

        foreach (var primitive in dimension.InsertPrimitives)
        {
            InsertPrimitive(
                dimension.Id,
                primitive.PrimitiveKey,
                "INSERT",
                primitive.SortOrder,
                primitive.SourceHandle,
                primitive.SourceLayer,
                ("$insert_name", primitive.Name),
                ("$x", primitive.X),
                ("$y", primitive.Y),
                ("$z", primitive.Z),
                ("$rotation_degrees", primitive.RotationDegrees),
                ("$scale_x", primitive.ScaleX),
                ("$scale_y", primitive.ScaleY),
                ("$scale_z", primitive.ScaleZ));
        }

        foreach (var primitive in dimension.CirclePrimitives)
        {
            InsertPrimitive(
                dimension.Id,
                primitive.PrimitiveKey,
                "CIRCLE",
                primitive.SortOrder,
                primitive.SourceHandle,
                primitive.SourceLayer,
                ("$center_x", primitive.CenterX),
                ("$center_y", primitive.CenterY),
                ("$radius", primitive.Radius));
        }

        foreach (var primitive in dimension.ArcPrimitives)
        {
            InsertPrimitive(
                dimension.Id,
                primitive.PrimitiveKey,
                "ARC",
                primitive.SortOrder,
                primitive.SourceHandle,
                primitive.SourceLayer,
                ("$center_x", primitive.CenterX),
                ("$center_y", primitive.CenterY),
                ("$radius", primitive.Radius),
                ("$start_angle_degrees", primitive.StartAngleDegrees),
                ("$end_angle_degrees", primitive.EndAngleDegrees));
        }

        foreach (var primitive in dimension.SolidPrimitives)
        {
            InsertPrimitive(
                dimension.Id,
                primitive.PrimitiveKey,
                "SOLID",
                primitive.SortOrder,
                primitive.SourceHandle,
                primitive.SourceLayer,
                ("$point1_x", primitive.Point1X),
                ("$point1_y", primitive.Point1Y),
                ("$point2_x", primitive.Point2X),
                ("$point2_y", primitive.Point2Y),
                ("$point3_x", primitive.Point3X),
                ("$point3_y", primitive.Point3Y),
                ("$point4_x", primitive.Point4X),
                ("$point4_y", primitive.Point4Y));
        }
    }

    private void InsertPrimitive(
        Guid dimensionId,
        string primitiveKey,
        string primitiveKind,
        int sortOrder,
        string? sourceHandle,
        string? sourceLayer,
        params (string Name, object? Value)[] values)
    {
        using var command = CreateCommand(
            """
            INSERT INTO extracted_dimension_primitives (
                dimension_id,
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
                $dimension_id,
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
        command.Parameters.AddWithValue("$dimension_id", dimensionId.ToString());
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

    private static object ConvertPrimitiveValue(object? value)
    {
        return value switch
        {
            null => DBNull.Value,
            decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),
            _ => value
        };
    }

    private IReadOnlyList<ExtractedDimensionLinePrimitive> GetLinePrimitives(Guid dimensionId)
    {
        return ListPrimitives(
            dimensionId,
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
    }

    private IReadOnlyList<ExtractedDimensionTextPrimitive> GetTextPrimitives(Guid dimensionId)
    {
        return ListPrimitives(
            dimensionId,
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
    }

    private IReadOnlyList<ExtractedDimensionInsertPrimitive> GetInsertPrimitives(Guid dimensionId)
    {
        return ListPrimitives(
            dimensionId,
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
    }

    private IReadOnlyList<ExtractedDimensionCirclePrimitive> GetCirclePrimitives(Guid dimensionId)
    {
        return ListPrimitives(
            dimensionId,
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
    }

    private IReadOnlyList<ExtractedDimensionArcPrimitive> GetArcPrimitives(Guid dimensionId)
    {
        return ListPrimitives(
            dimensionId,
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
    }

    private IReadOnlyList<ExtractedDimensionSolidPrimitive> GetSolidPrimitives(Guid dimensionId)
    {
        return ListPrimitives(
            dimensionId,
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
    }

    private IReadOnlyList<T> ListPrimitives<T>(Guid dimensionId, string primitiveKind, Func<SqliteDataReader, T> map)
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
            FROM extracted_dimension_primitives
            WHERE dimension_id = $dimension_id
              AND primitive_kind = $primitive_kind
            ORDER BY sort_order ASC, primitive_key ASC
            """);
        command.Parameters.AddWithValue("$dimension_id", dimensionId.ToString());
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
    {
        return reader.IsDBNull(ordinal)
            ? null
            : decimal.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture);
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
