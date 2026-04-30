namespace FloorplanFit.Infrastructure.Tests.TestSupport;

internal static class RepositoryPaths
{
    public static string FindSolutionRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "FloorplanFit.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate the FloorplanFit solution root from the current test base directory.");
    }
}
