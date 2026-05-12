using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public static class SqliteSchemaInitializer
{
    public static Task InitializeAsync(string databasePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var databaseDirectory = Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS measurement_contexts (
                id TEXT PRIMARY KEY,
                source_unit INTEGER NOT NULL,
                to_millimeters_factor TEXT NOT NULL,
                linear_tolerance_mm TEXT NOT NULL,
                angular_tolerance_deg TEXT NOT NULL,
                created_at_utc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS imported_documents (
                id TEXT PRIMARY KEY,
                document_type INTEGER NOT NULL,
                original_file_name TEXT NOT NULL,
                storage_path TEXT NOT NULL,
                sha256 TEXT NOT NULL,
                dxf_version TEXT NULL,
                measurement_context_id TEXT NOT NULL,
                imported_at_utc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS floorplan_templates (
                id TEXT PRIMARY KEY,
                code TEXT NOT NULL UNIQUE,
                name TEXT NOT NULL,
                current_version_id TEXT NULL,
                active_published_curation_id TEXT NULL,
                is_active INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS floorplan_versions (
                id TEXT PRIMARY KEY,
                floorplan_template_id TEXT NOT NULL,
                imported_document_id TEXT NOT NULL,
                geometry_fingerprint TEXT NOT NULL,
                version_number INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL,
                UNIQUE(floorplan_template_id, version_number)
            );

            CREATE TABLE IF NOT EXISTS geometry_paths (
                id TEXT PRIMARY KEY,
                is_closed INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS geometry_segments (
                id TEXT PRIMARY KEY,
                geometry_path_id TEXT NOT NULL,
                sort_order INTEGER NOT NULL,
                start_x TEXT NOT NULL,
                start_y TEXT NOT NULL,
                end_x TEXT NOT NULL,
                end_y TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS wall_extraction_runs (
                id TEXT PRIMARY KEY,
                floorplan_version_id TEXT NOT NULL,
                status TEXT NOT NULL,
                started_at_utc TEXT NOT NULL,
                finished_at_utc TEXT NULL,
                extractor_version TEXT NOT NULL,
                error_message TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS extracted_wall_candidates (
                id TEXT PRIMARY KEY,
                wall_extraction_run_id TEXT NOT NULL,
                source_entity_ref TEXT NOT NULL,
                source_layer TEXT NULL,
                geometry_path_id TEXT NULL,
                thickness_mm TEXT NULL,
                confidence TEXT NOT NULL,
                detection_notes TEXT NULL,
                status INTEGER NOT NULL,
                sort_order INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS extracted_room_labels (
                id TEXT PRIMARY KEY,
                wall_extraction_run_id TEXT NOT NULL,
                source_entity_ref TEXT NOT NULL,
                source_layer TEXT NULL,
                text TEXT NOT NULL,
                x TEXT NOT NULL,
                y TEXT NOT NULL,
                confidence TEXT NOT NULL,
                detection_notes TEXT NULL,
                sort_order INTEGER NOT NULL,
                source_entity_kind TEXT NULL,
                text_height TEXT NULL,
                rotation_degrees TEXT NULL,
                text_style_name TEXT NULL,
                horizontal_alignment TEXT NULL,
                vertical_alignment TEXT NULL,
                attachment_point TEXT NULL,
                color_argb TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS extracted_opening_candidates (
                id TEXT PRIMARY KEY,
                wall_extraction_run_id TEXT NOT NULL,
                source_entity_ref TEXT NOT NULL,
                source_layer TEXT NULL,
                kind TEXT NOT NULL,
                source_entity_kind TEXT NULL,
                geometry_path_id TEXT NULL,
                confidence TEXT NOT NULL,
                detection_notes TEXT NULL,
                sort_order INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS extracted_opening_labels (
                id TEXT PRIMARY KEY,
                wall_extraction_run_id TEXT NOT NULL,
                source_entity_ref TEXT NOT NULL,
                source_layer TEXT NULL,
                kind TEXT NOT NULL,
                text TEXT NOT NULL,
                x TEXT NOT NULL,
                y TEXT NOT NULL,
                confidence TEXT NOT NULL,
                detection_notes TEXT NULL,
                sort_order INTEGER NOT NULL,
                source_entity_kind TEXT NULL,
                text_height TEXT NULL,
                rotation_degrees TEXT NULL,
                text_style_name TEXT NULL,
                horizontal_alignment TEXT NULL,
                vertical_alignment TEXT NULL,
                attachment_point TEXT NULL,
                color_argb TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS extracted_fixed_plan_components (
                id TEXT PRIMARY KEY,
                wall_extraction_run_id TEXT NOT NULL,
                source_entity_ref TEXT NOT NULL,
                source_layer TEXT NULL,
                kind TEXT NOT NULL,
                source_entity_kind TEXT NULL,
                source_block_name TEXT NULL,
                confidence TEXT NOT NULL,
                detection_notes TEXT NULL,
                sort_order INTEGER NOT NULL,
                color_argb TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS extracted_fixed_plan_component_paths (
                fixed_plan_component_id TEXT NOT NULL,
                geometry_path_id TEXT NOT NULL,
                sort_order INTEGER NOT NULL,
                PRIMARY KEY (fixed_plan_component_id, geometry_path_id)
            );

            CREATE TABLE IF NOT EXISTS extracted_protected_detail_assemblies (
                id TEXT PRIMARY KEY,
                wall_extraction_run_id TEXT NOT NULL,
                source_entity_ref TEXT NOT NULL,
                source_layer TEXT NULL,
                kind TEXT NOT NULL,
                source_entity_kind TEXT NULL,
                confidence TEXT NOT NULL,
                detection_notes TEXT NULL,
                sort_order INTEGER NOT NULL,
                color_argb TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS extracted_protected_detail_assembly_paths (
                protected_detail_assembly_id TEXT NOT NULL,
                geometry_path_id TEXT NOT NULL,
                sort_order INTEGER NOT NULL,
                PRIMARY KEY (protected_detail_assembly_id, geometry_path_id)
            );

            CREATE TABLE IF NOT EXISTS extracted_dimensions (
                id TEXT PRIMARY KEY,
                wall_extraction_run_id TEXT NOT NULL,
                source_entity_ref TEXT NOT NULL,
                source_layer TEXT NULL,
                source_entity_kind TEXT NOT NULL,
                geometry_block_name TEXT NULL,
                display_text TEXT NOT NULL,
                display_text_source TEXT NOT NULL,
                raw_text_override TEXT NOT NULL,
                measurement_source_units TEXT NOT NULL,
                measurement_millimeters TEXT NOT NULL,
                source_unit TEXT NOT NULL,
                dim_type INTEGER NOT NULL,
                angle TEXT NOT NULL,
                oblique_angle TEXT NOT NULL,
                def_point_x TEXT NOT NULL,
                def_point_y TEXT NOT NULL,
                def_point_z TEXT NOT NULL,
                def_point2_x TEXT NOT NULL,
                def_point2_y TEXT NOT NULL,
                def_point2_z TEXT NOT NULL,
                def_point3_x TEXT NOT NULL,
                def_point3_y TEXT NOT NULL,
                def_point3_z TEXT NOT NULL,
                confidence TEXT NOT NULL,
                detection_notes TEXT NULL,
                sort_order INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS extracted_dimension_line_segments (
                dimension_id TEXT NOT NULL,
                sort_order INTEGER NOT NULL,
                start_x TEXT NOT NULL,
                start_y TEXT NOT NULL,
                end_x TEXT NOT NULL,
                end_y TEXT NOT NULL,
                PRIMARY KEY (dimension_id, sort_order)
            );

            CREATE TABLE IF NOT EXISTS extracted_dimension_primitives (
                dimension_id TEXT NOT NULL,
                primitive_key TEXT NOT NULL,
                primitive_kind TEXT NOT NULL,
                sort_order INTEGER NOT NULL,
                source_handle TEXT NULL,
                source_layer TEXT NULL,
                start_x TEXT NULL,
                start_y TEXT NULL,
                end_x TEXT NULL,
                end_y TEXT NULL,
                text_value TEXT NULL,
                x TEXT NULL,
                y TEXT NULL,
                z TEXT NULL,
                height TEXT NULL,
                rotation_degrees TEXT NULL,
                style_name TEXT NULL,
                horizontal_alignment TEXT NULL,
                vertical_alignment TEXT NULL,
                attachment_point TEXT NULL,
                insert_name TEXT NULL,
                scale_x TEXT NULL,
                scale_y TEXT NULL,
                scale_z TEXT NULL,
                center_x TEXT NULL,
                center_y TEXT NULL,
                radius TEXT NULL,
                start_angle_degrees TEXT NULL,
                end_angle_degrees TEXT NULL,
                point1_x TEXT NULL,
                point1_y TEXT NULL,
                point2_x TEXT NULL,
                point2_y TEXT NULL,
                point3_x TEXT NULL,
                point3_y TEXT NULL,
                point4_x TEXT NULL,
                point4_y TEXT NULL,
                PRIMARY KEY (dimension_id, primitive_key)
            );

            CREATE TABLE IF NOT EXISTS floorplan_curations (
                id TEXT PRIMARY KEY,
                floorplan_version_id TEXT NOT NULL,
                curation_version INTEGER NOT NULL,
                status INTEGER NOT NULL,
                based_on_curation_id TEXT NULL,
                notes TEXT NULL,
                created_at_utc TEXT NOT NULL,
                published_at_utc TEXT NULL,
                UNIQUE(floorplan_version_id, curation_version)
            );

            CREATE TABLE IF NOT EXISTS pinch_groups (
                id TEXT PRIMARY KEY,
                floorplan_curation_id TEXT NOT NULL,
                name TEXT NOT NULL,
                axis_tag INTEGER NOT NULL,
                sort_order INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS pinch_markers (
                id TEXT PRIMARY KEY,
                floorplan_curation_id TEXT NOT NULL,
                pinch_group_id TEXT NOT NULL,
                source_candidate_id TEXT NOT NULL,
                geometry_path_id TEXT NOT NULL,
                position_ratio TEXT NOT NULL,
                max_trim_mm TEXT NOT NULL,
                sort_order INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS floorplan_artifact_classifications (
                floorplan_curation_id TEXT NOT NULL,
                source_artifact_kind TEXT NOT NULL,
                source_artifact_id TEXT NOT NULL,
                resolved_family TEXT NOT NULL,
                resolved_category TEXT NOT NULL,
                resolved_type TEXT NOT NULL,
                decision_state INTEGER NOT NULL,
                updated_at_utc TEXT NOT NULL,
                PRIMARY KEY (floorplan_curation_id, source_artifact_kind, source_artifact_id)
            );

            CREATE TABLE IF NOT EXISTS floorplan_artifact_positions (
                floorplan_curation_id TEXT NOT NULL,
                source_artifact_kind TEXT NOT NULL,
                source_artifact_id TEXT NOT NULL,
                position_mode INTEGER NOT NULL,
                resolved_x TEXT NULL,
                resolved_y TEXT NULL,
                translation_dx TEXT NULL,
                translation_dy TEXT NULL,
                updated_at_utc TEXT NOT NULL,
                PRIMARY KEY (floorplan_curation_id, source_artifact_kind, source_artifact_id)
            );

            CREATE TABLE IF NOT EXISTS floorplan_label_overrides (
                floorplan_curation_id TEXT NOT NULL,
                source_artifact_kind TEXT NOT NULL,
                source_artifact_id TEXT NOT NULL,
                resolved_text_height TEXT NULL,
                updated_at_utc TEXT NOT NULL,
                PRIMARY KEY (floorplan_curation_id, source_artifact_kind, source_artifact_id)
            );

            CREATE TABLE IF NOT EXISTS floorplan_dimension_overrides (
                floorplan_curation_id TEXT NOT NULL,
                source_dimension_key TEXT NOT NULL,
                source_entity_ref TEXT NOT NULL,
                source_handle TEXT NULL,
                display_text TEXT NOT NULL,
                def_point_x TEXT NOT NULL,
                def_point_y TEXT NOT NULL,
                def_point_z TEXT NOT NULL,
                def_point2_x TEXT NOT NULL,
                def_point2_y TEXT NOT NULL,
                def_point2_z TEXT NOT NULL,
                def_point3_x TEXT NOT NULL,
                def_point3_y TEXT NOT NULL,
                def_point3_z TEXT NOT NULL,
                render_text_x TEXT NULL,
                render_text_y TEXT NULL,
                render_text_height TEXT NULL,
                render_text_rotation_degrees TEXT NULL,
                render_text_style_name TEXT NULL,
                render_text_horizontal_alignment TEXT NULL,
                render_text_vertical_alignment TEXT NULL,
                render_text_attachment_point TEXT NULL,
                updated_at_utc TEXT NOT NULL,
                last_exported_at_utc TEXT NULL,
                PRIMARY KEY (floorplan_curation_id, source_dimension_key)
            );

            CREATE TABLE IF NOT EXISTS floorplan_dimension_override_primitives (
                floorplan_curation_id TEXT NOT NULL,
                source_dimension_key TEXT NOT NULL,
                primitive_key TEXT NOT NULL,
                primitive_kind TEXT NOT NULL,
                sort_order INTEGER NOT NULL,
                source_handle TEXT NULL,
                source_layer TEXT NULL,
                start_x TEXT NULL,
                start_y TEXT NULL,
                end_x TEXT NULL,
                end_y TEXT NULL,
                text_value TEXT NULL,
                x TEXT NULL,
                y TEXT NULL,
                z TEXT NULL,
                height TEXT NULL,
                rotation_degrees TEXT NULL,
                style_name TEXT NULL,
                horizontal_alignment TEXT NULL,
                vertical_alignment TEXT NULL,
                attachment_point TEXT NULL,
                insert_name TEXT NULL,
                scale_x TEXT NULL,
                scale_y TEXT NULL,
                scale_z TEXT NULL,
                center_x TEXT NULL,
                center_y TEXT NULL,
                radius TEXT NULL,
                start_angle_degrees TEXT NULL,
                end_angle_degrees TEXT NULL,
                point1_x TEXT NULL,
                point1_y TEXT NULL,
                point2_x TEXT NULL,
                point2_y TEXT NULL,
                point3_x TEXT NULL,
                point3_y TEXT NULL,
                point4_x TEXT NULL,
                point4_y TEXT NULL,
                PRIMARY KEY (floorplan_curation_id, source_dimension_key, primitive_key)
            );
            """;

        command.ExecuteNonQuery();
        EnsureColumnExists(connection, "floorplan_templates", "active_published_curation_id", "TEXT NULL");
        EnsureRoomLabelsSchema(connection);
        EnsureFixedPlanComponentsSchema(connection);
        EnsureDimensionsSchema(connection);
        EnsureArtifactClassificationSchema(connection);
        EnsureArtifactPositionSchema(connection);
        EnsureLabelOverrideSchema(connection);
        EnsureDimensionOverrideSchema(connection);
        EnsurePinchMarkersSchema(connection);
        return Task.CompletedTask;
    }

    private static void EnsureRoomLabelsSchema(SqliteConnection connection)
    {
        EnsureColumnExists(connection, "extracted_room_labels", "source_entity_kind", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_room_labels", "text_height", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_room_labels", "rotation_degrees", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_room_labels", "text_style_name", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_room_labels", "horizontal_alignment", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_room_labels", "vertical_alignment", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_room_labels", "attachment_point", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_room_labels", "color_argb", "TEXT NULL");
    }

    private static void EnsureFixedPlanComponentsSchema(SqliteConnection connection)
    {
        EnsureColumnExists(connection, "extracted_fixed_plan_components", "color_argb", "TEXT NULL");
    }

    private static void EnsureDimensionsSchema(SqliteConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS extracted_dimension_line_segments (
                    dimension_id TEXT NOT NULL,
                    sort_order INTEGER NOT NULL,
                    start_x TEXT NOT NULL,
                    start_y TEXT NOT NULL,
                    end_x TEXT NOT NULL,
                    end_y TEXT NOT NULL,
                    PRIMARY KEY (dimension_id, sort_order)
                );
                """;
            command.ExecuteNonQuery();
        }

        using (var primitiveCommand = connection.CreateCommand())
        {
            primitiveCommand.CommandText =
                """
                CREATE TABLE IF NOT EXISTS extracted_dimension_primitives (
                    dimension_id TEXT NOT NULL,
                    primitive_key TEXT NOT NULL,
                    primitive_kind TEXT NOT NULL,
                    sort_order INTEGER NOT NULL,
                    source_handle TEXT NULL,
                    source_layer TEXT NULL,
                    start_x TEXT NULL,
                    start_y TEXT NULL,
                    end_x TEXT NULL,
                    end_y TEXT NULL,
                    text_value TEXT NULL,
                    x TEXT NULL,
                    y TEXT NULL,
                    z TEXT NULL,
                    height TEXT NULL,
                    rotation_degrees TEXT NULL,
                    style_name TEXT NULL,
                    horizontal_alignment TEXT NULL,
                    vertical_alignment TEXT NULL,
                    attachment_point TEXT NULL,
                    insert_name TEXT NULL,
                    scale_x TEXT NULL,
                    scale_y TEXT NULL,
                    scale_z TEXT NULL,
                    center_x TEXT NULL,
                    center_y TEXT NULL,
                    radius TEXT NULL,
                    start_angle_degrees TEXT NULL,
                    end_angle_degrees TEXT NULL,
                    point1_x TEXT NULL,
                    point1_y TEXT NULL,
                    point2_x TEXT NULL,
                    point2_y TEXT NULL,
                    point3_x TEXT NULL,
                    point3_y TEXT NULL,
                    point4_x TEXT NULL,
                    point4_y TEXT NULL,
                    PRIMARY KEY (dimension_id, primitive_key)
                );
                """;
            primitiveCommand.ExecuteNonQuery();
        }

        EnsureColumnExists(connection, "extracted_dimensions", "source_handle", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_dimensions", "source_entity_kind", "TEXT NOT NULL DEFAULT 'DIMENSION'");
        EnsureColumnExists(connection, "extracted_dimensions", "geometry_block_name", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_dimensions", "display_text", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "extracted_dimensions", "display_text_source", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "extracted_dimensions", "raw_text_override", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "extracted_dimensions", "measurement_source_units", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "extracted_dimensions", "measurement_millimeters", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "extracted_dimensions", "source_unit", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "extracted_dimensions", "dim_type", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumnExists(connection, "extracted_dimensions", "angle", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "extracted_dimensions", "oblique_angle", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "extracted_dimensions", "def_point_x", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "extracted_dimensions", "def_point_y", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "extracted_dimensions", "def_point_z", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "extracted_dimensions", "def_point2_x", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "extracted_dimensions", "def_point2_y", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "extracted_dimensions", "def_point2_z", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "extracted_dimensions", "def_point3_x", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "extracted_dimensions", "def_point3_y", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "extracted_dimensions", "def_point3_z", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "extracted_dimensions", "render_text_x", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_dimensions", "render_text_y", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_dimensions", "render_text_height", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_dimensions", "render_text_rotation_degrees", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_dimensions", "render_text_style_name", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_dimensions", "render_text_horizontal_alignment", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_dimensions", "render_text_vertical_alignment", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_dimensions", "render_text_attachment_point", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_dimensions", "confidence", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "extracted_dimensions", "detection_notes", "TEXT NULL");
        EnsureColumnExists(connection, "extracted_dimensions", "sort_order", "INTEGER NOT NULL DEFAULT 1");
    }

    private static void EnsureArtifactClassificationSchema(SqliteConnection connection)
    {
        EnsureColumnExists(connection, "floorplan_artifact_classifications", "resolved_family", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "floorplan_artifact_classifications", "resolved_category", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "floorplan_artifact_classifications", "resolved_type", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "floorplan_artifact_classifications", "decision_state", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumnExists(connection, "floorplan_artifact_classifications", "updated_at_utc", "TEXT NOT NULL DEFAULT ''");
    }

    private static void EnsureArtifactPositionSchema(SqliteConnection connection)
    {
        EnsureColumnExists(connection, "floorplan_artifact_positions", "position_mode", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumnExists(connection, "floorplan_artifact_positions", "resolved_x", "TEXT NULL");
        EnsureColumnExists(connection, "floorplan_artifact_positions", "resolved_y", "TEXT NULL");
        EnsureColumnExists(connection, "floorplan_artifact_positions", "translation_dx", "TEXT NULL");
        EnsureColumnExists(connection, "floorplan_artifact_positions", "translation_dy", "TEXT NULL");
        EnsureColumnExists(connection, "floorplan_artifact_positions", "updated_at_utc", "TEXT NOT NULL DEFAULT ''");
    }

    private static void EnsureLabelOverrideSchema(SqliteConnection connection)
    {
        EnsureColumnExists(connection, "floorplan_label_overrides", "resolved_text_height", "TEXT NULL");
        EnsureColumnExists(connection, "floorplan_label_overrides", "updated_at_utc", "TEXT NOT NULL DEFAULT ''");
    }

    private static void EnsureDimensionOverrideSchema(SqliteConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS floorplan_dimension_overrides (
                    floorplan_curation_id TEXT NOT NULL,
                    source_dimension_key TEXT NOT NULL,
                    source_entity_ref TEXT NOT NULL,
                    source_handle TEXT NULL,
                    display_text TEXT NOT NULL,
                    def_point_x TEXT NOT NULL,
                    def_point_y TEXT NOT NULL,
                    def_point_z TEXT NOT NULL,
                    def_point2_x TEXT NOT NULL,
                    def_point2_y TEXT NOT NULL,
                    def_point2_z TEXT NOT NULL,
                    def_point3_x TEXT NOT NULL,
                    def_point3_y TEXT NOT NULL,
                    def_point3_z TEXT NOT NULL,
                    render_text_x TEXT NULL,
                    render_text_y TEXT NULL,
                    render_text_height TEXT NULL,
                    render_text_rotation_degrees TEXT NULL,
                    render_text_style_name TEXT NULL,
                    render_text_horizontal_alignment TEXT NULL,
                    render_text_vertical_alignment TEXT NULL,
                    render_text_attachment_point TEXT NULL,
                    updated_at_utc TEXT NOT NULL,
                    last_exported_at_utc TEXT NULL,
                    PRIMARY KEY (floorplan_curation_id, source_dimension_key)
                );
                """;
            command.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS floorplan_dimension_override_primitives (
                    floorplan_curation_id TEXT NOT NULL,
                    source_dimension_key TEXT NOT NULL,
                    primitive_key TEXT NOT NULL,
                    primitive_kind TEXT NOT NULL,
                    sort_order INTEGER NOT NULL,
                    source_handle TEXT NULL,
                    source_layer TEXT NULL,
                    start_x TEXT NULL,
                    start_y TEXT NULL,
                    end_x TEXT NULL,
                    end_y TEXT NULL,
                    text_value TEXT NULL,
                    x TEXT NULL,
                    y TEXT NULL,
                    z TEXT NULL,
                    height TEXT NULL,
                    rotation_degrees TEXT NULL,
                    style_name TEXT NULL,
                    horizontal_alignment TEXT NULL,
                    vertical_alignment TEXT NULL,
                    attachment_point TEXT NULL,
                    insert_name TEXT NULL,
                    scale_x TEXT NULL,
                    scale_y TEXT NULL,
                    scale_z TEXT NULL,
                    center_x TEXT NULL,
                    center_y TEXT NULL,
                    radius TEXT NULL,
                    start_angle_degrees TEXT NULL,
                    end_angle_degrees TEXT NULL,
                    point1_x TEXT NULL,
                    point1_y TEXT NULL,
                    point2_x TEXT NULL,
                    point2_y TEXT NULL,
                    point3_x TEXT NULL,
                    point3_y TEXT NULL,
                    point4_x TEXT NULL,
                    point4_y TEXT NULL,
                    PRIMARY KEY (floorplan_curation_id, source_dimension_key, primitive_key)
                );
                """;
            command.ExecuteNonQuery();
        }

        EnsureColumnExists(connection, "floorplan_dimension_overrides", "source_entity_ref", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "source_handle", "TEXT NULL");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "display_text", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "def_point_x", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "def_point_y", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "def_point_z", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "def_point2_x", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "def_point2_y", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "def_point2_z", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "def_point3_x", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "def_point3_y", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "def_point3_z", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "render_text_x", "TEXT NULL");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "render_text_y", "TEXT NULL");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "render_text_height", "TEXT NULL");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "render_text_rotation_degrees", "TEXT NULL");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "render_text_style_name", "TEXT NULL");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "render_text_horizontal_alignment", "TEXT NULL");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "render_text_vertical_alignment", "TEXT NULL");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "render_text_attachment_point", "TEXT NULL");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "updated_at_utc", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "floorplan_dimension_overrides", "last_exported_at_utc", "TEXT NULL");
    }

    private static void EnsureColumnExists(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
    {
        using var infoCommand = connection.CreateCommand();
        infoCommand.CommandText = $"PRAGMA table_info({tableName})";

        using var reader = infoCommand.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}";
        alterCommand.ExecuteNonQuery();
    }

    private static void EnsurePinchMarkersSchema(SqliteConnection connection)
    {
        var columnNames = GetColumnNames(connection, "pinch_markers");
        if (columnNames.Contains("source_candidate_id") &&
            columnNames.Contains("pinch_group_id") &&
            !columnNames.Contains("curated_wall_id"))
        {
            return;
        }

        if (columnNames.Contains("source_candidate_id") && columnNames.Contains("axis_tag"))
        {
            MigrateAxisTaggedPinchMarkers(connection);
            return;
        }

        if (columnNames.Contains("pinch_group_id") && columnNames.Contains("curated_wall_id"))
        {
            MigrateCuratedWallPinchMarkers(connection);
            return;
        }

        RecreatePinchMarkersTable(connection);
    }

    private static void MigrateAxisTaggedPinchMarkers(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();

        ExecuteNonQuery(connection, transaction, "ALTER TABLE pinch_markers RENAME TO pinch_markers_axis_legacy");
        CreatePinchMarkersTable(connection, transaction);
        ExecuteNonQuery(
            connection,
            transaction,
            """
            INSERT INTO pinch_groups (
                id,
                floorplan_curation_id,
                name,
                axis_tag,
                sort_order
            )
            SELECT
                lower(hex(randomblob(16))),
                legacy.floorplan_curation_id,
                CASE legacy.axis_tag
                    WHEN 2 THEN 'Height'
                    ELSE 'Width'
                END,
                legacy.axis_tag,
                1
            FROM (
                SELECT DISTINCT floorplan_curation_id, axis_tag
                FROM pinch_markers_axis_legacy
            ) AS legacy
            WHERE NOT EXISTS (
                SELECT 1
                FROM pinch_groups AS existing
                WHERE existing.floorplan_curation_id = legacy.floorplan_curation_id
                  AND existing.axis_tag = legacy.axis_tag
            )
            """);
        ExecuteNonQuery(
            connection,
            transaction,
            """
            INSERT INTO pinch_markers (
                id,
                floorplan_curation_id,
                pinch_group_id,
                source_candidate_id,
                geometry_path_id,
                position_ratio,
                max_trim_mm,
                sort_order
            )
            SELECT
                legacy.id,
                legacy.floorplan_curation_id,
                groups.id,
                legacy.source_candidate_id,
                legacy.geometry_path_id,
                legacy.position_ratio,
                legacy.max_trim_mm,
                legacy.sort_order
            FROM pinch_markers_axis_legacy AS legacy
            INNER JOIN (
                SELECT
                    selected.id,
                    selected.floorplan_curation_id,
                    selected.axis_tag
                FROM pinch_groups AS selected
                WHERE selected.id = (
                    SELECT candidate.id
                    FROM pinch_groups AS candidate
                    WHERE candidate.floorplan_curation_id = selected.floorplan_curation_id
                      AND candidate.axis_tag = selected.axis_tag
                    ORDER BY candidate.sort_order ASC, candidate.id ASC
                    LIMIT 1
                )
            ) AS groups ON groups.floorplan_curation_id = legacy.floorplan_curation_id
                AND groups.axis_tag = legacy.axis_tag
            """);
        ExecuteNonQuery(connection, transaction, "DROP TABLE pinch_markers_axis_legacy");
        transaction.Commit();
    }

    private static void MigrateCuratedWallPinchMarkers(SqliteConnection connection)
    {
        if (!TableExists(connection, "pinch_groups") || !TableExists(connection, "curated_walls"))
        {
            RecreatePinchMarkersTable(connection);
            return;
        }

        using var transaction = connection.BeginTransaction();

        ExecuteNonQuery(connection, transaction, "ALTER TABLE pinch_markers RENAME TO pinch_markers_legacy");
        CreatePinchMarkersTable(connection, transaction);
        ExecuteNonQuery(
            connection,
            transaction,
            """
            INSERT INTO pinch_markers (
                id,
                floorplan_curation_id,
                pinch_group_id,
                source_candidate_id,
                geometry_path_id,
                position_ratio,
                max_trim_mm,
                sort_order
            )
            SELECT
                legacy.id,
                legacy.floorplan_curation_id,
                legacy.pinch_group_id,
                walls.source_candidate_id,
                COALESCE(legacy.geometry_path_id, walls.geometry_path_id),
                legacy.position_ratio,
                legacy.max_trim_mm,
                legacy.sort_order
            FROM pinch_markers_legacy AS legacy
            INNER JOIN curated_walls AS walls ON walls.id = legacy.curated_wall_id
            WHERE walls.source_candidate_id IS NOT NULL
            """);
        ExecuteNonQuery(connection, transaction, "DROP TABLE pinch_markers_legacy");
        transaction.Commit();
    }

    private static HashSet<string> GetColumnNames(SqliteConnection connection, string tableName)
    {
        var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName})";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            columnNames.Add(reader.GetString(1));
        }

        return columnNames;
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static void RecreatePinchMarkersTable(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        ExecuteNonQuery(connection, transaction, "DROP TABLE IF EXISTS pinch_markers");
        CreatePinchMarkersTable(connection, transaction);
        transaction.Commit();
    }

    private static void CreatePinchMarkersTable(SqliteConnection connection, SqliteTransaction transaction)
    {
        ExecuteNonQuery(
            connection,
            transaction,
            """
            CREATE TABLE pinch_markers (
                id TEXT PRIMARY KEY,
                floorplan_curation_id TEXT NOT NULL,
                pinch_group_id TEXT NOT NULL,
                source_candidate_id TEXT NOT NULL,
                geometry_path_id TEXT NOT NULL,
                position_ratio TEXT NOT NULL,
                max_trim_mm TEXT NOT NULL,
                sort_order INTEGER NOT NULL
            )
            """);
    }

    private static void ExecuteNonQuery(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
