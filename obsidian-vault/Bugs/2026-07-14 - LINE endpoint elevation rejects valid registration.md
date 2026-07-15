# LINE endpoint elevation rejects valid registration

- **Status:** RED regression added; production fix pending.
- **Loop / layer:** Loop 2 site-plan adaptation — Infrastructure DXF registration.

## Observed behavior

`DxfElectricalFloorRegistrationEstimator.AddLine` currently sends both `DxfLine` endpoints through `GetPlanarPoints`. That helper rejects finite endpoint Z differences as non-planar, even when the LINE's XY segment is unchanged and usable for 2D structural registration.

## Contract

The synthetic asymmetric structural fixture now serializes the Electrical geometry as raw structural `LINE` entities. The first line end has `Z = 3`; all XY coordinates match the Floor fixture. `EstimateAsync` must return one conclusive identity transform with scale `1`.

`3DFACE` non-planarity remains intentionally rejected by the existing unsupported-entity theory.

## Evidence boundary

Only static source inspection and `git diff --check` were performed. No `dotnet`, build, test, restore, watch, Desktop, or runtime command ran.

## Relevant code

- [[../Implementation/2026-07-14 - LINE endpoint elevation registration RED]]
- `src/FloorplanFit.Infrastructure/Dxf/DxfElectricalFloorRegistrationEstimator.cs` — current `AddLine`/`GetPlanarPoints` coupling.
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/DxfElectricalFloorRegistrationEstimatorTests.cs` — focused RED contract.
