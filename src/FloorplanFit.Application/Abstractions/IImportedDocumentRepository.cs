using FloorplanFit.Domain.Documents;

namespace FloorplanFit.Application.Abstractions;

public interface IImportedDocumentRepository
{
    Task AddAsync(ImportedDocument document, CancellationToken cancellationToken);
}
