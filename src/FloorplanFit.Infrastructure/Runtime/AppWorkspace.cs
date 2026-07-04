namespace FloorplanFit.Infrastructure.Runtime;

public sealed class AppWorkspace
{
    public AppWorkspace(string rootPath, string? adjustedDxfDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Workspace root path is required.", nameof(rootPath));
        }

        RootPath = rootPath;
        LibraryDirectory = Path.Combine(rootPath, "library");
        LibraryRawDxfDirectory = Path.Combine(LibraryDirectory, "raw-dxf");
        LibraryAdjustedDxfDirectory = string.IsNullOrWhiteSpace(adjustedDxfDirectory)
            ? Path.Combine(LibraryDirectory, "adjusted-dxf")
            : adjustedDxfDirectory;
        DatabasePath = Path.Combine(rootPath, "app.db");
    }

    public string RootPath { get; }

    public string LibraryDirectory { get; }

    public string LibraryRawDxfDirectory { get; }

    public string LibraryAdjustedDxfDirectory { get; }

    public string DatabasePath { get; }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(RootPath);
        Directory.CreateDirectory(LibraryDirectory);
        Directory.CreateDirectory(LibraryRawDxfDirectory);
        Directory.CreateDirectory(LibraryAdjustedDxfDirectory);
    }
}
