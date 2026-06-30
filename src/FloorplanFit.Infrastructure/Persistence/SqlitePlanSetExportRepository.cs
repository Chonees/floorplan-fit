using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqlitePlanSetExportRepository : IPlanSetExportRepository
{
    private readonly SqliteSession session;

    public SqlitePlanSetExportRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(PlanSetExport export, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using (var command = session.Connection.CreateCommand())
        {
            command.Transaction = session.Transaction;
            command.CommandText =
                """
                INSERT INTO plan_set_exports (
                    id,
                    plan_set_version_id,
                    canonical_adjustment_id,
                    status,
                    confidence_summary_json,
                    package_manifest_path,
                    created_at_utc)
                VALUES (
                    $id,
                    $plan_set_version_id,
                    $canonical_adjustment_id,
                    $status,
                    $confidence_summary_json,
                    $package_manifest_path,
                    $created_at_utc)
                """;
            command.Parameters.AddWithValue("$id", export.Id.ToString());
            command.Parameters.AddWithValue("$plan_set_version_id", export.PlanSetVersionId.ToString());
            command.Parameters.AddWithValue("$canonical_adjustment_id", export.CanonicalAdjustmentId.ToString());
            command.Parameters.AddWithValue("$status", export.Status.ToString());
            command.Parameters.AddWithValue("$confidence_summary_json", export.ConfidenceSummaryJson);
            command.Parameters.AddWithValue("$package_manifest_path", (object?)export.PackageManifestPath ?? DBNull.Value);
            command.Parameters.AddWithValue("$created_at_utc", export.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
            command.ExecuteNonQuery();
        }

        foreach (var sheet in export.Sheets)
        {
            InsertSheet(sheet);
        }

        return Task.CompletedTask;
    }

    private void InsertSheet(PlanSetExportedSheet sheet)
    {
        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText =
            """
            INSERT INTO plan_set_exported_sheets (
                id,
                plan_set_export_id,
                plan_sheet_id,
                sheet_projection_id,
                sheet_kind,
                storage_path,
                status,
                projection_method,
                confidence,
                warning,
                rule_summary)
            VALUES (
                $id,
                $plan_set_export_id,
                $plan_sheet_id,
                $sheet_projection_id,
                $sheet_kind,
                $storage_path,
                $status,
                $projection_method,
                $confidence,
                $warning,
                $rule_summary)
            """;
        command.Parameters.AddWithValue("$id", sheet.Id.ToString());
        command.Parameters.AddWithValue("$plan_set_export_id", sheet.PlanSetExportId.ToString());
        command.Parameters.AddWithValue("$plan_sheet_id", sheet.PlanSheetId.ToString());
        command.Parameters.AddWithValue("$sheet_projection_id", sheet.SheetProjectionId.HasValue ? (object)sheet.SheetProjectionId.Value.ToString() : DBNull.Value);
        command.Parameters.AddWithValue("$sheet_kind", sheet.SheetKind);
        command.Parameters.AddWithValue("$storage_path", (object?)sheet.StoragePath ?? DBNull.Value);
        command.Parameters.AddWithValue("$status", sheet.Status.ToString());
        command.Parameters.AddWithValue("$projection_method", (object?)sheet.ProjectionMethod ?? DBNull.Value);
        command.Parameters.AddWithValue("$confidence", sheet.Confidence.HasValue ? (object)sheet.Confidence.Value.ToString(CultureInfo.InvariantCulture) : DBNull.Value);
        command.Parameters.AddWithValue("$warning", (object?)sheet.Warning ?? DBNull.Value);
        command.Parameters.AddWithValue("$rule_summary", (object?)sheet.RuleSummary ?? DBNull.Value);
        command.ExecuteNonQuery();
    }
}
