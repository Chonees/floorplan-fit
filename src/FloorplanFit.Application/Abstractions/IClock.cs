namespace FloorplanFit.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
