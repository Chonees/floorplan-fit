namespace FloorplanFit.Application.PlanSets.Export;

public static class AtomicDirectoryPublisher
{
    public static async Task PublishAsync(
        string requestedFinalDirectory,
        Func<string, CancellationToken, Task> populateStagingAsync,
        CancellationToken cancellationToken)
    {
        await PublishAsync<object?>(
            requestedFinalDirectory,
            populateStagingAsync,
            async (publishAsync, publishCancellationToken) =>
            {
                await publishAsync(publishCancellationToken);
                return null;
            },
            cancellationToken);
    }

    public static async Task<TResult> PublishAsync<TResult>(
        string requestedFinalDirectory,
        Func<string, CancellationToken, Task> populateStagingAsync,
        Func<Func<CancellationToken, Task>, CancellationToken, Task<TResult>> completePublicationAsync,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestedFinalDirectory))
        {
            throw new ArgumentException("Final directory is required.", nameof(requestedFinalDirectory));
        }

        ArgumentNullException.ThrowIfNull(populateStagingAsync);
        ArgumentNullException.ThrowIfNull(completePublicationAsync);
        cancellationToken.ThrowIfCancellationRequested();

        var finalDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(requestedFinalDirectory));
        var parentDirectory = Path.GetDirectoryName(finalDirectory);
        var finalName = Path.GetFileName(finalDirectory);
        if (string.IsNullOrWhiteSpace(parentDirectory) || string.IsNullOrWhiteSpace(finalName))
        {
            throw new ArgumentException("Final directory must have a parent and name.", nameof(requestedFinalDirectory));
        }

        Directory.CreateDirectory(parentDirectory);
        if (Directory.Exists(finalDirectory) || File.Exists(finalDirectory)) throw new IOException($"Final directory already exists: '{finalDirectory}'.");

        var stagingDirectory = Path.Combine(
            parentDirectory,
            $".{finalName}.staging-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingDirectory);
        var published = false;

        try
        {
            await populateStagingAsync(stagingDirectory, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            Task PublishStagingAsync(CancellationToken publishCancellationToken)
            {
                publishCancellationToken.ThrowIfCancellationRequested();
                if (published) throw new InvalidOperationException("Staged directory was already published.");
                if (Directory.Exists(finalDirectory) || File.Exists(finalDirectory)) throw new IOException($"Final directory appeared while staging: '{finalDirectory}'.");

                Directory.Move(stagingDirectory, finalDirectory);
                published = true;
                return Task.CompletedTask;
            }

            var result = await completePublicationAsync(PublishStagingAsync, cancellationToken);
            if (!published)
            {
                throw new InvalidOperationException("Publication callback completed without publishing the staged directory.");
            }

            return result;
        }
        catch (Exception exception)
        {
            try
            {
                if (published)
                {
                    RollbackPublishedDirectory(finalDirectory);
                }
                else if (Directory.Exists(stagingDirectory))
                {
                    Directory.Delete(stagingDirectory, recursive: true);
                }
            }
            catch (Exception cleanupException)
            {
                exception.Data[published
                    ? "AtomicPublishedDirectoryRollbackFailure"
                    : "AtomicStagingCleanupFailure"] = cleanupException.Message;
            }

            throw;
        }
    }

    public static void RollbackPublishedDirectory(string requestedFinalDirectory)
    {
        var finalDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(requestedFinalDirectory));
        if (!Directory.Exists(finalDirectory))
        {
            return;
        }

        var parentDirectory = Path.GetDirectoryName(finalDirectory)
            ?? throw new InvalidOperationException("Published directory has no parent.");
        var rollbackDirectory = Path.Combine(
            parentDirectory,
            $".{Path.GetFileName(finalDirectory)}.rollback-{Guid.NewGuid():N}");
        Directory.Move(finalDirectory, rollbackDirectory);
        Directory.Delete(rollbackDirectory, recursive: true);
    }
}
