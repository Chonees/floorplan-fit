using FloorplanFit.Domain.Measurement;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Tests.TestSupport;

namespace FloorplanFit.Infrastructure.Tests.Dxf;

public sealed class IxMiliaDxfGatewayTests
{
    [Fact]
    public async Task ReadFloorPlanAsync_reads_real_santa_barbara_metadata()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var fixturePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var gateway = new IxMiliaDxfGateway();

        var document = await gateway.ReadFloorPlanAsync(fixturePath, CancellationToken.None);

        Assert.Equal("SANTA-BARBARA.dxf", document.OriginalFileName);
        Assert.Equal("SANTA-BARBARA", document.SuggestedName);
        Assert.Equal(LengthUnit.Inch, document.SourceUnit);
        Assert.Equal(25.4m, document.ToMillimetersFactor);
        Assert.Equal("AC1032", document.DxfVersion);
        Assert.StartsWith("bbox:", document.GeometryFingerprint, StringComparison.Ordinal);

        var coordinates = document.GeometryFingerprint["bbox:".Length..]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();

        Assert.Equal(4, coordinates.Length);
        Assert.True(coordinates[2] > coordinates[0]);
        Assert.True(coordinates[3] > coordinates[1]);
    }
}
