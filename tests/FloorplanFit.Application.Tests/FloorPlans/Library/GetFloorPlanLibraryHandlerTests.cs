using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Library;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Library;

public sealed class GetFloorPlanLibraryHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_items_from_the_library_reader()
    {
        var expectedItems = new[]
        {
            new FloorPlanLibraryItemDto(
                Guid.NewGuid(),
                "santa-barbara",
                "SANTA-BARBARA",
                "Imported",
                2,
                new DateTime(2026, 4, 29, 20, 0, 0, DateTimeKind.Utc),
                "inch")
        };

        var reader = new FakeFloorPlanLibraryReader(expectedItems);
        var handler = new GetFloorPlanLibraryHandler(reader);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.Equal(expectedItems, result);
    }

    private sealed class FakeFloorPlanLibraryReader : IFloorPlanLibraryReader
    {
        private readonly IReadOnlyList<FloorPlanLibraryItemDto> items;

        public FakeFloorPlanLibraryReader(IReadOnlyList<FloorPlanLibraryItemDto> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<FloorPlanLibraryItemDto>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
    }
}
