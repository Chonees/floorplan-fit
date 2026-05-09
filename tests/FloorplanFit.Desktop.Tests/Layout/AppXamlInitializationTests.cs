using FloorplanFit.Desktop;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Layout;

public sealed class AppXamlInitializationTests
{
    [Fact]
    public void App_initialize_loads_application_xaml_without_throwing()
    {
        var app = new App();

        var exception = Record.Exception(app.Initialize);

        Assert.Null(exception);
    }
}
