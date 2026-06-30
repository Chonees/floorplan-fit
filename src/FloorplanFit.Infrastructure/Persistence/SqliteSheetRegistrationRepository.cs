using System.Globalization;
using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.PlanSets;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteSheetRegistrationRepository : ISheetRegistrationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly SqliteSession session;

    public SqliteSheetRegistrationRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO sheet_registrations (
                id,
                plan_set_version_id,
                dependent_sheet_id,
                canonical_floor_plan_version_id,
                method,
                transform_json,
                confidence,
                status,
                warning,
                created_at_utc,
                confirmed_at_utc)
            VALUES (
                $id,
                $plan_set_version_id,
                $dependent_sheet_id,
                $canonical_floor_plan_version_id,
                $method,
                $transform_json,
                $confidence,
                $status,
                $warning,
                $created_at_utc,
                $confirmed_at_utc)
            """);
        command.Parameters.AddWithValue("$id", registration.Id.ToString());
        command.Parameters.AddWithValue("$plan_set_version_id", registration.PlanSetVersionId.ToString());
        command.Parameters.AddWithValue("$dependent_sheet_id", registration.DependentSheetId.ToString());
        command.Parameters.AddWithValue("$canonical_floor_plan_version_id", registration.CanonicalFloorPlanVersionId.ToString());
        command.Parameters.AddWithValue("$method", registration.Method.ToString());
        command.Parameters.AddWithValue("$transform_json", SerializeTransform(registration.Transform));
        command.Parameters.AddWithValue("$confidence", registration.Confidence.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$status", registration.Status.ToString());
        command.Parameters.AddWithValue("$warning", (object?)registration.Warning ?? DBNull.Value);
        command.Parameters.AddWithValue("$created_at_utc", registration.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$confirmed_at_utc", FormatNullableDateTime(registration.ConfirmedAtUtc));
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT id,
                   plan_set_version_id,
                   dependent_sheet_id,
                   canonical_floor_plan_version_id,
                   method,
                   transform_json,
                   confidence,
                   status,
                   warning,
                   created_at_utc,
                   confirmed_at_utc
            FROM sheet_registrations
            WHERE id = $id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$id", registrationId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<SheetRegistration?>(null);
        }

        return Task.FromResult<SheetRegistration?>(MapRegistration(reader));
    }

    private static string SerializeTransform(SheetRegistrationTransform transform)
    {
        return JsonSerializer.Serialize(
            new StoredTransform(
                transform.Scale,
                transform.RotationDegrees,
                transform.TranslateX,
                transform.TranslateY),
            JsonOptions);
    }

    private static SheetRegistrationTransform DeserializeTransform(string json)
    {
        var transform = JsonSerializer.Deserialize<StoredTransform>(json, JsonOptions)
            ?? throw new InvalidOperationException("Stored sheet registration transform is invalid.");

        return new SheetRegistrationTransform(
            transform.Scale,
            transform.RotationDegrees,
            transform.TranslateX,
            transform.TranslateY);
    }

    private static SheetRegistration MapRegistration(SqliteDataReader reader)
    {
        return new SheetRegistration(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            Guid.Parse(reader.GetString(2)),
            Guid.Parse(reader.GetString(3)),
            Enum.Parse<SheetRegistrationMethod>(reader.GetString(4)),
            DeserializeTransform(reader.GetString(5)),
            decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
            Enum.Parse<SheetRegistrationStatus>(reader.GetString(7)),
            DateTime.Parse(reader.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.IsDBNull(10)
                ? null
                : DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.IsDBNull(8) ? null : reader.GetString(8));
    }

    private static object FormatNullableDateTime(DateTime? value)
    {
        return value.HasValue
            ? value.Value.ToString("O", CultureInfo.InvariantCulture)
            : DBNull.Value;
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
