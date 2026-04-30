using System.Security.Cryptography;
using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Infrastructure.Security;

public sealed class Sha256FileHashService : IFileHashService
{
    public async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A file path is required.", nameof(filePath));
        }

        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
