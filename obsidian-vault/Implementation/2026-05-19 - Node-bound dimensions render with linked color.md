# Node-bound dimensions render with linked color

## What
Las cotas que ya tienen una relacion manual con nodos (`DimensionIntervalBindingDto`) ahora se renderizan con un color semantico celeste en el preview.

## Why
El usuario necesitaba distinguir rapidamente que medidas ya estan relacionadas con nodos, sin tener que inspeccionar una por una desde el panel lateral.

## Where
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewSemanticPalette.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderComposer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/DimensionPreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/CadTextPreviewLayerRenderer.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewRenderComposerTests.cs`

## Learned
La fuente correcta para este estado visual es `DimensionIntervalBindingDto`, no `DimensionBindingDto`: el usuario se refiere explicitamente a medidas ya vinculadas con nodos. La prioridad visual queda seleccion verde > vinculada celeste > normal negra.

## Verified
- `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewRenderComposerTests|FullyQualifiedName~FloorPlanPreviewControlTests|FullyQualifiedName~NativeDimensionPreviewControlTests|FullyQualifiedName~MeasurementBindingPreviewLayerRendererTests" --nologo --artifacts-path .testartifacts\dotnet-test-artifacts`
- Resultado: 72/72 tests pasan.
