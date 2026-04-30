using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.Documents;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteImportedDocumentRepository : IImportedDocumentRepository
{
    private readonly SqliteSession session;

    public SqliteImportedDocumentRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(ImportedDocument document, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO imported_documents (
                id,
                document_type,
                original_file_name,
                storage_path,
                sha256,
                dxf_version,
                measurement_context_id,
                imported_at_utc)
            VALUES (
                $id,
                $document_type,
                $original_file_name,
                $storage_path,
                $sha256,
                $dxf_version,
                $measurement_context_id,
                $imported_at_utc)
            """);

        command.Parameters.AddWithValue("$id", document.Id.ToString());
        command.Parameters.AddWithValue("$document_type", (int)document.DocumentType);
        command.Parameters.AddWithValue("$original_file_name", document.OriginalFileName);
        command.Parameters.AddWithValue("$storage_path", document.StoragePath);
        command.Parameters.AddWithValue("$sha256", document.Sha256);
        command.Parameters.AddWithValue("$dxf_version", (object?)document.DxfVersion ?? DBNull.Value);
        command.Parameters.AddWithValue("$measurement_context_id", document.MeasurementContextId.ToString());
        command.Parameters.AddWithValue("$imported_at_utc", document.ImportedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
