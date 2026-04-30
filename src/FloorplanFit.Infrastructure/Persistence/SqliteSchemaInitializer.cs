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

            CREATE TABLE IF NOT EXISTS curated_walls (
                id TEXT PRIMARY KEY,
                floorplan_curation_id TEXT NOT NULL,
                stable_wall_id TEXT NOT NULL,
                source_candidate_id TEXT NULL,
                source_entity_ref TEXT NULL,
                geometry_path_id TEXT NULL,
                wall_role INTEGER NOT NULL,
                mobility_level INTEGER NOT NULL,
                protection_level INTEGER NOT NULL,
                thickness_mm TEXT NULL,
                assembly_code TEXT NULL,
                height_mm TEXT NULL,
                is_exterior INTEGER NOT NULL,
                is_structural_hint INTEGER NOT NULL,
                wall_group_id TEXT NULL,
                sort_order INTEGER NOT NULL,
                notes TEXT NULL
            );
            """;

        command.ExecuteNonQuery();
        EnsureColumnExists(connection, "floorplan_templates", "active_published_curation_id", "TEXT NULL");
        return Task.CompletedTask;
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
}
