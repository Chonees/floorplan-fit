using FloorplanFit.Domain.PlanSets;
using FloorplanFit.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Persistence;

public sealed class SqliteSchemaInitializerTests
{
    [Fact]
    public async Task InitializeAsync_sets_and_preserves_current_user_version()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-schema-version-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(tempRoot, "floorplan-fit.db");

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None);
            await SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None);

            await using var connection = new SqliteConnection($"Data Source={databasePath}");
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version";

            Assert.Equal(
                SqliteSchemaInitializer.CurrentSchemaVersion,
                Convert.ToInt32(await command.ExecuteScalarAsync()));
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_repairs_current_v2_database_missing_whole_plan_proof_column()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-schema-v2-proof-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(tempRoot, "floorplan-fit.db");
        var registrationId = Guid.NewGuid();
        var planSetVersionId = Guid.NewGuid();
        var dependentSheetId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();

        try
        {
            Directory.CreateDirectory(tempRoot);
            await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                // A raw interpolated string with a single $ treats { as the start of an
                // interpolation and offers no doubled-brace escape, so the transform JSON
                // braces are supplied through a plain, uninterpolated raw string.
                const string transformJson =
                    """{"Scale":1,"RotationDegrees":0,"TranslateX":0,"TranslateY":0}""";
                command.CommandText =
                    $"""
                    CREATE TABLE sheet_registrations (
                        id TEXT PRIMARY KEY,
                        plan_set_version_id TEXT NOT NULL,
                        dependent_sheet_id TEXT NOT NULL,
                        canonical_floor_plan_version_id TEXT NOT NULL,
                        method TEXT NOT NULL,
                        transform_json TEXT NOT NULL,
                        confidence TEXT NOT NULL,
                        status TEXT NOT NULL,
                        warning TEXT NULL,
                        rule_summary TEXT NULL,
                        created_at_utc TEXT NOT NULL,
                        confirmed_at_utc TEXT NULL
                    );
                    INSERT INTO sheet_registrations (
                        id,
                        plan_set_version_id,
                        dependent_sheet_id,
                        canonical_floor_plan_version_id,
                        method,
                        transform_json,
                        confidence,
                        status,
                        created_at_utc,
                        confirmed_at_utc)
                    VALUES (
                        '{registrationId}',
                        '{planSetVersionId}',
                        '{dependentSheetId}',
                        '{canonicalFloorPlanVersionId}',
                        'WholeSheetSimilarity',
                        '{transformJson}',
                        '0.9',
                        'Confirmed',
                        '2026-07-15T12:00:00.0000000Z',
                        '2026-07-15T12:05:00.0000000Z');
                    PRAGMA user_version = 2;
                    """;
                await command.ExecuteNonQueryAsync();
            }

            await SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None);
            await SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None);

            await using var verificationConnection = new SqliteConnection($"Data Source={databasePath}");
            await verificationConnection.OpenAsync();
            await using var verificationCommand = verificationConnection.CreateCommand();
            verificationCommand.CommandText =
                "SELECT COUNT(*) FROM pragma_table_info('sheet_registrations') WHERE name = 'whole_plan_registration_proof_json'";
            Assert.Equal(1, Convert.ToInt32(await verificationCommand.ExecuteScalarAsync()));

            await using var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None);
            var repository = new SqliteSheetRegistrationRepository(session);
            Assert.Equal(registrationId, (await repository.GetByIdAsync(registrationId, CancellationToken.None))!.Id);
            Assert.Single(await repository.ListByPlanSetVersionAsync(planSetVersionId, CancellationToken.None));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_rejects_newer_user_version_without_mutating_database()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-schema-newer-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(tempRoot, "floorplan-fit.db");
        var newerVersion = SqliteSchemaInitializer.CurrentSchemaVersion + 1;

        try
        {
            Directory.CreateDirectory(tempRoot);

            await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText =
                    $"""
                    CREATE TABLE sentinel (value TEXT NOT NULL);
                    INSERT INTO sentinel (value) VALUES ('preserve-me');
                    PRAGMA user_version = {newerVersion};
                    """;
                await command.ExecuteNonQueryAsync();
            }

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None));

            Assert.Contains("newer than supported", exception.Message, StringComparison.Ordinal);

            await using var verificationConnection = new SqliteConnection($"Data Source={databasePath}");
            await verificationConnection.OpenAsync();

            await using var verificationCommand = verificationConnection.CreateCommand();
            verificationCommand.CommandText = "SELECT value FROM sentinel";
            Assert.Equal("preserve-me", Convert.ToString(await verificationCommand.ExecuteScalarAsync()));

            verificationCommand.CommandText = "PRAGMA user_version";
            Assert.Equal(newerVersion, Convert.ToInt32(await verificationCommand.ExecuteScalarAsync()));
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_repairs_legacy_registration_identity_before_enforcing_guards()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-schema-registration-v1-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(tempRoot, "floorplan-fit.db");
        var floorPlanVersionId = Guid.NewGuid();
        var planSetVersionId = Guid.NewGuid();
        var dependentSheetId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        try
        {
            Directory.CreateDirectory(tempRoot);

            await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText =
                    $"""
                    CREATE TABLE floorplan_versions (
                        id TEXT PRIMARY KEY,
                        deleted_at_utc TEXT NULL
                    );
                    CREATE TABLE plan_set_versions (
                        id TEXT PRIMARY KEY,
                        house_plan_set_id TEXT NOT NULL,
                        canonical_floor_plan_version_id TEXT NOT NULL,
                        version_number INTEGER NOT NULL,
                        created_at_utc TEXT NOT NULL
                    );
                    CREATE TABLE plan_sheets (
                        id TEXT PRIMARY KEY,
                        plan_set_version_id TEXT NOT NULL
                    );
                    CREATE TABLE sheet_registrations (
                        id TEXT PRIMARY KEY,
                        plan_set_version_id TEXT NOT NULL,
                        dependent_sheet_id TEXT NOT NULL,
                        canonical_floor_plan_version_id TEXT NOT NULL
                    );
                    INSERT INTO floorplan_versions (id, deleted_at_utc)
                    VALUES ('{floorPlanVersionId}', NULL);
                    INSERT INTO plan_set_versions (
                        id,
                        house_plan_set_id,
                        canonical_floor_plan_version_id,
                        version_number,
                        created_at_utc
                    ) VALUES (
                        '{planSetVersionId}',
                        '{Guid.NewGuid()}',
                        '{floorPlanVersionId}',
                        1,
                        '2026-07-13T12:00:00.0000000Z'
                    );
                    INSERT INTO plan_sheets (id, plan_set_version_id)
                    VALUES ('{dependentSheetId}', '{planSetVersionId}');
                    INSERT INTO sheet_registrations (
                        id,
                        plan_set_version_id,
                        dependent_sheet_id,
                        canonical_floor_plan_version_id
                    ) VALUES (
                        '{registrationId}',
                        '{planSetVersionId}',
                        '{dependentSheetId}',
                        '{planSetVersionId}'
                    );
                    PRAGMA user_version = 1;
                    """;
                await command.ExecuteNonQueryAsync();
            }

            await SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None);

            await using var verificationConnection = new SqliteConnection($"Data Source={databasePath}");
            await verificationConnection.OpenAsync();

            await using var verificationCommand = verificationConnection.CreateCommand();
            verificationCommand.CommandText =
                $"SELECT canonical_floor_plan_version_id FROM sheet_registrations WHERE id = '{registrationId}'";
            Assert.Equal(
                floorPlanVersionId.ToString(),
                Convert.ToString(await verificationCommand.ExecuteScalarAsync()));

            verificationCommand.CommandText = "PRAGMA user_version";
            Assert.Equal(
                SqliteSchemaInitializer.CurrentSchemaVersion,
                Convert.ToInt32(await verificationCommand.ExecuteScalarAsync()));

            verificationCommand.CommandText =
                "SELECT COUNT(*) FROM pragma_table_info('sheet_registrations') WHERE name = 'whole_plan_registration_proof_json'";
            Assert.Equal(1, Convert.ToInt32(await verificationCommand.ExecuteScalarAsync()));

            var proof = new WholePlanRegistrationProof(
                WholePlanRegistrationProof.CurrentVersion,
                Passed: true,
                floorPlanVersionId,
                dependentSheetId,
                new string('a', 64),
                new string('b', 64),
                HorizontalCoverage: 0.94m,
                VerticalCoverage: 0.91m,
                RootMeanSquareResidual: 0.01m,
                MaximumResidual: 0.02m);
            var roundTrip = new SheetRegistration(
                Guid.NewGuid(),
                planSetVersionId,
                dependentSheetId,
                floorPlanVersionId,
                SheetRegistrationMethod.WholeSheetSimilarity,
                new SheetRegistrationTransform(1m, 0m, 0m, 0m),
                0.9m,
                SheetRegistrationStatus.PendingConfirmation,
                new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc),
                confirmedAtUtc: null,
                warning: null,
                ruleSummary: null,
                wholePlanRegistrationProof: proof);

            await using (var addSession = await SqliteSession.OpenAsync(databasePath, CancellationToken.None))
            {
                await new SqliteSheetRegistrationRepository(addSession).AddAsync(roundTrip, CancellationToken.None);
                await new SqliteUnitOfWork(addSession).SaveChangesAsync(CancellationToken.None);
            }

            await using (var updateSession = await SqliteSession.OpenAsync(databasePath, CancellationToken.None))
            {
                var loaded = await new SqliteSheetRegistrationRepository(updateSession).GetByIdAsync(
                    roundTrip.Id,
                    CancellationToken.None);
                Assert.Equal(proof, loaded!.WholePlanRegistrationProof);
                var confirmed = new SheetRegistration(
                    loaded.Id,
                    loaded.PlanSetVersionId,
                    loaded.DependentSheetId,
                    loaded.CanonicalFloorPlanVersionId,
                    loaded.Method,
                    loaded.Transform,
                    loaded.Confidence,
                    SheetRegistrationStatus.Confirmed,
                    loaded.CreatedAtUtc,
                    new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
                    loaded.Warning,
                    loaded.RuleSummary,
                    loaded.WholePlanRegistrationProof);
                await new SqliteSheetRegistrationRepository(updateSession).UpdateAsync(confirmed, CancellationToken.None);
                await new SqliteUnitOfWork(updateSession).SaveChangesAsync(CancellationToken.None);
            }

            await using (var readSession = await SqliteSession.OpenAsync(databasePath, CancellationToken.None))
            {
                var loaded = await new SqliteSheetRegistrationRepository(readSession).GetByIdAsync(
                    roundTrip.Id,
                    CancellationToken.None);
                Assert.Equal(SheetRegistrationStatus.Confirmed, loaded!.Status);
                Assert.Equal(proof, loaded.WholePlanRegistrationProof);
            }

            verificationCommand.CommandText =
                $"UPDATE floorplan_versions SET deleted_at_utc = '2026-07-13T13:00:00Z' WHERE id = '{floorPlanVersionId}'";
            await Assert.ThrowsAsync<SqliteException>(() => verificationCommand.ExecuteNonQueryAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task OpenAsync_applies_foreign_keys_and_busy_timeout()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-sqlite-policy-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(tempRoot, "floorplan-fit.db");

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None);
            await using var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None);

            await using var command = session.Connection.CreateCommand();
            command.CommandText = "PRAGMA foreign_keys";
            Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync()));

            command.CommandText = "PRAGMA busy_timeout";
            Assert.True(Convert.ToInt64(await command.ExecuteScalarAsync()) >= 5000L);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_migrates_axis_tagged_pinch_markers_once()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-schema-axis-legacy-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(tempRoot, "floorplan-fit.db");

        try
        {
            Directory.CreateDirectory(tempRoot);

            await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    CREATE TABLE pinch_markers (
                        id TEXT PRIMARY KEY,
                        floorplan_curation_id TEXT NOT NULL,
                        axis_tag INTEGER NOT NULL,
                        source_candidate_id TEXT NOT NULL,
                        geometry_path_id TEXT NOT NULL,
                        position_ratio TEXT NOT NULL,
                        max_trim_mm TEXT NOT NULL,
                        sort_order INTEGER NOT NULL
                    );
                    INSERT INTO pinch_markers (
                        id,
                        floorplan_curation_id,
                        axis_tag,
                        source_candidate_id,
                        geometry_path_id,
                        position_ratio,
                        max_trim_mm,
                        sort_order
                    ) VALUES (
                        'marker-1',
                        'curation-1',
                        2,
                        'candidate-1',
                        'geometry-1',
                        '0.5',
                        '100',
                        1
                    );
                    """;
                await command.ExecuteNonQueryAsync();
            }

            await SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None);
            await SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None);

            await using var verificationConnection = new SqliteConnection($"Data Source={databasePath}");
            await verificationConnection.OpenAsync();

            await using var verificationCommand = verificationConnection.CreateCommand();
            verificationCommand.CommandText =
                """
                SELECT marker.source_candidate_id, marker.geometry_path_id, groups.axis_tag
                FROM pinch_markers AS marker
                INNER JOIN pinch_groups AS groups ON groups.id = marker.pinch_group_id
                WHERE marker.id = 'marker-1'
                """;

            await using var reader = await verificationCommand.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("candidate-1", reader.GetString(0));
            Assert.Equal("geometry-1", reader.GetString(1));
            Assert.Equal(2, reader.GetInt32(2));
            Assert.False(await reader.ReadAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_preserves_curated_wall_legacy_rows_when_dependencies_are_unavailable()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-schema-wall-legacy-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(tempRoot, "floorplan-fit.db");

        try
        {
            Directory.CreateDirectory(tempRoot);

            await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    CREATE TABLE pinch_markers (
                        id TEXT PRIMARY KEY,
                        floorplan_curation_id TEXT NOT NULL,
                        pinch_group_id TEXT NOT NULL,
                        curated_wall_id TEXT NOT NULL,
                        geometry_path_id TEXT NULL,
                        position_ratio TEXT NOT NULL,
                        max_trim_mm TEXT NOT NULL,
                        sort_order INTEGER NOT NULL
                    );
                    INSERT INTO pinch_markers (
                        id,
                        floorplan_curation_id,
                        pinch_group_id,
                        curated_wall_id,
                        geometry_path_id,
                        position_ratio,
                        max_trim_mm,
                        sort_order
                    ) VALUES (
                        'legacy-marker',
                        'curation-1',
                        'group-1',
                        'wall-1',
                        NULL,
                        '0.5',
                        '100',
                        1
                    );
                    """;
                await command.ExecuteNonQueryAsync();
            }

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None));

            await using var verificationConnection = new SqliteConnection($"Data Source={databasePath}");
            await verificationConnection.OpenAsync();

            await using var verificationCommand = verificationConnection.CreateCommand();
            verificationCommand.CommandText =
                "SELECT curated_wall_id FROM pinch_markers WHERE id = 'legacy-marker'";
            Assert.Equal("wall-1", Convert.ToString(await verificationCommand.ExecuteScalarAsync()));

            verificationCommand.CommandText = "PRAGMA user_version";
            Assert.Equal(0, Convert.ToInt32(await verificationCommand.ExecuteScalarAsync()));
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_preserves_unknown_pinch_marker_schema_and_fails_closed()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-schema-unknown-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(tempRoot, "floorplan-fit.db");

        try
        {
            Directory.CreateDirectory(tempRoot);

            await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    CREATE TABLE pinch_markers (
                        id TEXT PRIMARY KEY,
                        unknown_payload TEXT NOT NULL
                    );
                    INSERT INTO pinch_markers (id, unknown_payload)
                    VALUES ('preserve-me', 'original-value');
                    """;
                await command.ExecuteNonQueryAsync();
            }

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None));

            Assert.Contains("Unsupported pinch_markers schema", exception.Message, StringComparison.Ordinal);

            await using var verificationConnection = new SqliteConnection($"Data Source={databasePath}");
            await verificationConnection.OpenAsync();

            await using var verificationCommand = verificationConnection.CreateCommand();
            verificationCommand.CommandText =
                "SELECT unknown_payload FROM pinch_markers WHERE id = 'preserve-me'";

            Assert.Equal("original-value", Convert.ToString(await verificationCommand.ExecuteScalarAsync()));
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_is_idempotent_for_current_pinch_marker_schema()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-schema-current-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(tempRoot, "floorplan-fit.db");

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None);
            await SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None);

            await using var connection = new SqliteConnection($"Data Source={databasePath}");
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('pinch_markers')";

            Assert.Equal(8L, Convert.ToInt64(await command.ExecuteScalarAsync()));
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
