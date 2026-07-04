using System.Globalization;
using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.PlanSets;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteSheetAdjustmentProjectionRepository : ISheetAdjustmentProjectionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly SqliteSession session;

    public SqliteSheetAdjustmentProjectionRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO sheet_adjustment_projections (
                id,
                plan_set_version_id,
                dependent_sheet_id,
                sheet_registration_id,
                canonical_adjustment_id,
                method,
                transform_json,
                confidence,
                status,
                warning,
                rule_summary,
                canonical_compression_step_count,
                created_at_utc,
                recipe_handling_summary)
            VALUES (
                $id,
                $plan_set_version_id,
                $dependent_sheet_id,
                $sheet_registration_id,
                $canonical_adjustment_id,
                $method,
                $transform_json,
                $confidence,
                $status,
                $warning,
                $rule_summary,
                $canonical_compression_step_count,
                $created_at_utc,
                $recipe_handling_summary)
            """);
        command.Parameters.AddWithValue("$id", projection.Id.ToString());
        command.Parameters.AddWithValue("$plan_set_version_id", projection.PlanSetVersionId.ToString());
        command.Parameters.AddWithValue("$dependent_sheet_id", projection.DependentSheetId.ToString());
        command.Parameters.AddWithValue("$sheet_registration_id", projection.SheetRegistrationId.ToString());
        command.Parameters.AddWithValue("$canonical_adjustment_id", projection.CanonicalAdjustmentId.ToString());
        command.Parameters.AddWithValue("$method", projection.Method.ToString());
        command.Parameters.AddWithValue("$transform_json", SerializeTransform(projection.Transform));
        command.Parameters.AddWithValue("$confidence", projection.Confidence.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$status", projection.Status.ToString());
        command.Parameters.AddWithValue("$warning", (object?)projection.Warning ?? DBNull.Value);
        command.Parameters.AddWithValue("$rule_summary", (object?)projection.RuleSummary ?? DBNull.Value);
        command.Parameters.AddWithValue("$canonical_compression_step_count", projection.CanonicalCompressionStepCount);
        command.Parameters.AddWithValue("$created_at_utc", projection.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$recipe_handling_summary", (object?)projection.RecipeHandlingSummary ?? DBNull.Value);
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<SheetAdjustmentProjection?> GetByIdAsync(Guid projectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT id,
                   plan_set_version_id,
                   dependent_sheet_id,
                   sheet_registration_id,
                   canonical_adjustment_id,
                   method,
                   transform_json,
                   confidence,
                   status,
                   warning,
                   rule_summary,
                   canonical_compression_step_count,
                   created_at_utc,
                   recipe_handling_summary
            FROM sheet_adjustment_projections
            WHERE id = $id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$id", projectionId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<SheetAdjustmentProjection?>(null);
        }

        return Task.FromResult<SheetAdjustmentProjection?>(MapProjection(reader));
    }

    public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
        Guid planSetVersionId,
        Guid canonicalAdjustmentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT id,
                   plan_set_version_id,
                   dependent_sheet_id,
                   sheet_registration_id,
                   canonical_adjustment_id,
                   method,
                   transform_json,
                   confidence,
                   status,
                   warning,
                   rule_summary,
                   canonical_compression_step_count,
                   created_at_utc,
                   recipe_handling_summary
            FROM sheet_adjustment_projections
            WHERE plan_set_version_id = $plan_set_version_id
              AND canonical_adjustment_id = $canonical_adjustment_id
            ORDER BY created_at_utc ASC
            """);
        command.Parameters.AddWithValue("$plan_set_version_id", planSetVersionId.ToString());
        command.Parameters.AddWithValue("$canonical_adjustment_id", canonicalAdjustmentId.ToString());

        using var reader = command.ExecuteReader();
        var items = new List<SheetAdjustmentProjection>();
        while (reader.Read())
        {
            items.Add(MapProjection(reader));
        }

        return Task.FromResult<IReadOnlyList<SheetAdjustmentProjection>>(items);
    }

    public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAsync(
        Guid planSetVersionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT id,
                   plan_set_version_id,
                   dependent_sheet_id,
                   sheet_registration_id,
                   canonical_adjustment_id,
                   method,
                   transform_json,
                   confidence,
                   status,
                   warning,
                   rule_summary,
                   canonical_compression_step_count,
                   created_at_utc,
                   recipe_handling_summary
            FROM sheet_adjustment_projections
            WHERE plan_set_version_id = $plan_set_version_id
            ORDER BY created_at_utc ASC
            """);
        command.Parameters.AddWithValue("$plan_set_version_id", planSetVersionId.ToString());

        using var reader = command.ExecuteReader();
        var items = new List<SheetAdjustmentProjection>();
        while (reader.Read())
        {
            items.Add(MapProjection(reader));
        }

        return Task.FromResult<IReadOnlyList<SheetAdjustmentProjection>>(items);
    }

    public Task UpdateAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            UPDATE sheet_adjustment_projections
            SET status = $status
            WHERE id = $id
            """);
        command.Parameters.AddWithValue("$id", projection.Id.ToString());
        command.Parameters.AddWithValue("$status", projection.Status.ToString());

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("Sheet adjustment projection was not found.");
        }

        return Task.CompletedTask;
    }

    private static string SerializeTransform(SheetAdjustmentProjectionTransform transform)
    {
        return JsonSerializer.Serialize(
            new StoredTransform(
                transform.Scale,
                transform.RotationDegrees,
                transform.TranslateX,
                transform.TranslateY),
            JsonOptions);
    }

    private static SheetAdjustmentProjectionTransform DeserializeTransform(string json)
    {
        var transform = JsonSerializer.Deserialize<StoredTransform>(json, JsonOptions)
            ?? throw new InvalidOperationException("Stored sheet adjustment projection transform is invalid.");

        return new SheetAdjustmentProjectionTransform(
            transform.Scale,
            transform.RotationDegrees,
            transform.TranslateX,
            transform.TranslateY);
    }

    private static SheetAdjustmentProjection MapProjection(SqliteDataReader reader)
    {
        return new SheetAdjustmentProjection(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            Guid.Parse(reader.GetString(2)),
            Guid.Parse(reader.GetString(3)),
            Guid.Parse(reader.GetString(4)),
            Enum.Parse<SheetAdjustmentProjectionMethod>(reader.GetString(5)),
            DeserializeTransform(reader.GetString(6)),
            decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
            Enum.Parse<SheetAdjustmentProjectionStatus>(reader.GetString(8)),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.GetInt32(11),
            DateTime.Parse(reader.GetString(12), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.IsDBNull(13) ? null : reader.GetString(13));
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }

    private sealed record StoredTransform(
        decimal Scale,
        decimal RotationDegrees,
        decimal TranslateX,
        decimal TranslateY);
}
