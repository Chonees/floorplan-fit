using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Storage;

namespace FloorplanFit.Infrastructure.Tests.Imports;

public sealed class ManagedFileStorageTests
{
    [Fact]
    public async Task CopyIntoLibraryAsync_copies_the_source_file_into_the_managed_raw_dxf_directory()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-storage-{Guid.NewGuid():N}");
        var sourceDirectory = Path.Combine(tempRoot, "source");
        Directory.CreateDirectory(sourceDirectory);

        var sourcePath = Path.Combine(sourceDirectory, "sample-plan.dxf");
        await File.WriteAllTextAsync(sourcePath, "sample-dxf-content");

        try
        {
            var workspace = new AppWorkspace(Path.Combine(tempRoot, "workspace"));
            var storage = new ManagedFileStorage(workspace);

            var managedPath = await storage.CopyIntoLibraryAsync(sourcePath, CancellationToken.None);

            Assert.StartsWith(workspace.LibraryRawDxfDirectory, managedPath, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("sample-plan.dxf", Path.GetFileName(managedPath));
            Assert.True(File.Exists(managedPath));
            Assert.Equal("sample-dxf-content", await File.ReadAllTextAsync(managedPath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task CopyIntoLibraryAsync_uses_a_numbered_suffix_when_the_file_name_already_exists()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-storage-{Guid.NewGuid():N}");
        var sourceDirectory = Path.Combine(tempRoot, "source");
        Directory.CreateDirectory(sourceDirectory);

        var sourcePath = Path.Combine(sourceDirectory, "sample-plan.dxf");
        await File.WriteAllTextAsync(sourcePath, "first-version");

        try
        {
            var workspace = new AppWorkspace(Path.Combine(tempRoot, "workspace"));
            var storage = new ManagedFileStorage(workspace);

            var firstManagedPath = await storage.CopyIntoLibraryAsync(sourcePath, CancellationToken.None);
            await File.WriteAllTextAsync(sourcePath, "second-version");
            var secondManagedPath = await storage.CopyIntoLibraryAsync(sourcePath, CancellationToken.None);

            Assert.Equal("sample-plan.dxf", Path.GetFileName(firstManagedPath));
            Assert.Equal("sample-plan-2.dxf", Path.GetFileName(secondManagedPath));
            Assert.NotEqual(firstManagedPath, secondManagedPath);
            Assert.Equal("first-version", await File.ReadAllTextAsync(firstManagedPath));
            Assert.Equal("second-version", await File.ReadAllTextAsync(secondManagedPath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
