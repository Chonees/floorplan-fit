# LINE endpoint elevation registration RED

- **Status:** focused RED regression authored; no production edit.
- **Loop / layer:** Loop 2 — Infrastructure.

## Changed file

`tests/FloorplanFit.Infrastructure.Tests/Dxf/DxfElectricalFloorRegistrationEstimatorTests.cs`

## Test contract

`EstimateAsync_accepts_structural_lines_with_different_finite_endpoint_z_elevations` writes matching asymmetric Floor geometry and raw Electrical `LINE` geometry. The first Electrical endpoint carries finite `Z = 3` while retaining the same XY geometry. It expects the scale-one identity registration to be `Estimated` and conclusive.

`CreateRawLinesWithElevatedEndpoint` reuses the synthetic line fixture and DXF raw-entity writer so the test contains no production coordinates or SEMINOLE-specific values.

## Why it is RED now

Production routes `DxfLine` through `GetPlanarPoints`, whose endpoint-Z equality rule is appropriate for face-like entities but rejects this 2D projection input as non-planar before registration.

## Preserved boundary

The theory case for `UnsupportedStructuralEntityKind.NonPlanar3dFace` is unchanged, so face non-planarity remains fail-closed.

## Verification

`git diff --check` passed. No executable command was run.
