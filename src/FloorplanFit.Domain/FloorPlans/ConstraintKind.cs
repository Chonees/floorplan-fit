namespace FloorplanFit.Domain.FloorPlans;

public enum ConstraintKind
{
    LockWall = 1,
    ProtectWall = 2,
    MaxWallMove = 3,
    PreserveGroup = 4,
    PreserveFacade = 5,
    PreserveSpace = 6,
    MinSpaceArea = 7,
    MinSpaceWidth = 8,
    MinSpaceDepth = 9
}
