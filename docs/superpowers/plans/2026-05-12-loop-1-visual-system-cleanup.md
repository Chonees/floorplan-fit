# Loop 1 Visual System Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clean up the shared visual system so Desktop chrome and preview surface stop hiding color/styling decisions in inline hex values and local renderer constants.

**Architecture:** Keep two visual ownership zones only: `App.axaml` owns app-shell tokens/resources; `PreviewSemanticPalette` owns preview-surface tokens. Refactor renderers and view-facing defaults to consume those named tokens instead of local ad-hoc colors. This is a behavior-preserving cleanup slice that prepares later modularization loops.

**Tech Stack:** C#/.NET 10, Avalonia XAML resources, Avalonia `Color`/`Brush`, xUnit Desktop tests, PowerShell verification.

---

## File Structure

- Modify: `src/FloorplanFit.Desktop/App.axaml`
  - Purpose: centralize app-shell brushes/resources for buttons, chips, inputs, borders, and shadows.
- Modify: `src/FloorplanFit.Desktop/Controls/Preview/PreviewSemanticPalette.cs`
  - Purpose: centralize preview-only color tokens, including workspace grid and handle visuals.
- Modify: `src/FloorplanFit.Desktop/Controls/Preview/PreviewWorkspaceRenderer.cs`
  - Purpose: consume shared preview tokens instead of local hardcoded colors.
- Modify: `src/FloorplanFit.Desktop/Controls/Preview/CompressionHandlePreviewLayerRenderer.cs`
  - Purpose: consume shared preview tokens instead of local hardcoded colors.
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
  - Purpose: use a shared transparent/default ARGB token instead of a local fallback string.
- Create: `tests/FloorplanFit.Desktop.Tests/Controls/PreviewSemanticPaletteTests.cs`
  - Purpose: pin the preview token contract before refactoring.
- Modify: `tests/FloorplanFit.Desktop.Tests/Layout/AppXamlInitializationTests.cs`
  - Purpose: pin the XAML resource tokenization and prevent inline-style regressions.

---

## Tasks

### Task 1: Tokenize the app-shell styles in `App.axaml`

**Files:**
- Modify: `src/FloorplanFit.Desktop/App.axaml`
- Modify/Test: `tests/FloorplanFit.Desktop.Tests/Layout/AppXamlInitializationTests.cs`

- [x] **Step 1: Write the failing Desktop test for XAML tokenization**

Add a test like:

```csharp
[Fact]
public void AppAxaml_promotes_interactive_style_colors_to_named_resources()
{
    var solutionRoot = RepositoryPaths.FindSolutionRoot();
    var path = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "App.axaml");
    var xaml = File.ReadAllText(path);

    Assert.Contains("x:Key=\"MetricChipBrush\"", xaml, StringComparison.Ordinal);
    Assert.Contains("x:Key=\"ButtonBaseBrush\"", xaml, StringComparison.Ordinal);
    Assert.Contains("x:Key=\"ButtonPrimaryBorderBrush\"", xaml, StringComparison.Ordinal);
    Assert.Contains("x:Key=\"ToolActiveBrush\"", xaml, StringComparison.Ordinal);
    Assert.Contains("x:Key=\"InputBrush\"", xaml, StringComparison.Ordinal);

    Assert.DoesNotContain("Background\" Value=\"#3D36404C\"", xaml, StringComparison.Ordinal);
    Assert.DoesNotContain("Background\" Value=\"#33374250\"", xaml, StringComparison.Ordinal);
    Assert.DoesNotContain("Background\" Value=\"#5A4B5665\"", xaml, StringComparison.Ordinal);
    Assert.DoesNotContain("Background\" Value=\"#26313C48\"", xaml, StringComparison.Ordinal);
}
```

- [x] **Step 2: Run the targeted test to verify RED**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~AppXamlInitializationTests" --artifacts-path .\.artifacts-test\desktop-loop1-xaml-red
```

Expected: FAIL because the named resources do not exist yet and the inline hex values are still present.

- [x] **Step 3: Add the named resources and replace the targeted inline hex values**

In `App.axaml`, add named resources for the current inline values:

```xml
<SolidColorBrush x:Key="MetricChipBrush" Color="#3D36404C" />
<SolidColorBrush x:Key="ButtonBaseBrush" Color="#33374250" />
<SolidColorBrush x:Key="ButtonPrimaryBorderBrush" Color="#80FFFFFF" />
<SolidColorBrush x:Key="DangerButtonBrush" Color="#33414C5A" />
<SolidColorBrush x:Key="ToolActiveBrush" Color="#5A4B5665" />
<SolidColorBrush x:Key="ToolActiveCheckedBrush" Color="#5A4A5566" />
<SolidColorBrush x:Key="StrongBorderBrush" Color="#B3FFFFFF" />
<SolidColorBrush x:Key="InputBrush" Color="#26313C48" />
```

Then update these selectors to consume the resources instead of raw literals:

- `Border.metric-chip`
- `Button`
- `Button.primary`
- `Button.danger`
- `Button.tool`
- `Button.tool-active`
- `ToggleButton.folder-toggle:checked`
- `TextBox`
- `ComboBox`

- [x] **Step 4: Run the targeted test again to verify GREEN**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~AppXamlInitializationTests" --artifacts-path .\.artifacts-test\desktop-loop1-xaml-green
```

Expected: PASS.

---

### Task 2: Promote preview workspace/handle/default ARGB values into `PreviewSemanticPalette`

**Files:**
- Modify: `src/FloorplanFit.Desktop/Controls/Preview/PreviewSemanticPalette.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/Preview/PreviewWorkspaceRenderer.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/Preview/CompressionHandlePreviewLayerRenderer.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Create/Test: `tests/FloorplanFit.Desktop.Tests/Controls/PreviewSemanticPaletteTests.cs`

- [x] **Step 1: Write the failing preview-token test**

Create a test file with assertions like:

```csharp
public sealed class PreviewSemanticPaletteTests
{
    [Fact]
    public void PreviewSemanticPalette_exposes_workspace_handle_and_default_argb_tokens()
    {
        Assert.Equal("#00000000", PreviewSemanticPalette.TransparentArgb);
        Assert.Equal(Color.Parse("#FF0F172A"), PreviewSemanticPalette.WorkspaceBackground);
        Assert.Equal(Color.Parse("#FF1E293B"), PreviewSemanticPalette.MinorGrid);
        Assert.Equal(Color.Parse("#FF334155"), PreviewSemanticPalette.MajorGrid);
        Assert.Equal(Color.FromArgb(32, 0, 0, 0), PreviewSemanticPalette.HandleFill);
        Assert.Equal(Color.FromArgb(220, 0, 0, 0), PreviewSemanticPalette.HandleStroke);
    }
}
```

- [x] **Step 2: Run the targeted preview-token test to verify RED**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewSemanticPaletteTests" --artifacts-path .\.artifacts-test\desktop-loop1-palette-red
```

Expected: FAIL because those palette members do not exist yet.

- [x] **Step 3: Extend the palette and consume it from the renderers/ViewModel**

Add these members to `PreviewSemanticPalette`:

```csharp
public const string TransparentArgb = "#00000000";

public static readonly Color WorkspaceBackground = Color.Parse("#FF0F172A");
public static readonly Color WorkspaceDot = Color.FromArgb(92, 190, 190, 190);
public static readonly Color MinorGrid = Color.Parse("#FF1E293B");
public static readonly Color MajorGrid = Color.Parse("#FF334155");

public static readonly Color HandleFill = Color.FromArgb(32, 0, 0, 0);
public static readonly Color HandleStroke = Color.FromArgb(220, 0, 0, 0);
```

Then refactor:

- `PreviewWorkspaceRenderer` to use `PreviewSemanticPalette.WorkspaceBackground`, `WorkspaceDot`, `MinorGrid`, and `MajorGrid`
- `CompressionHandlePreviewLayerRenderer` to use `PreviewSemanticPalette.HandleFill` and `HandleStroke`
- `FloorPlanReviewViewModel.SelectedCuratedArtifactColorArgb` to fall back to `PreviewSemanticPalette.TransparentArgb`

- [x] **Step 4: Run the targeted preview-token test again to verify GREEN**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewSemanticPaletteTests" --artifacts-path .\.artifacts-test\desktop-loop1-palette-green
```

Expected: PASS.

---

### Task 3: Fresh verification for the visual-system slice

**Files:**
- Verify only

- [x] **Step 1: Run focused Desktop verification**

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~AppXamlInitializationTests|FullyQualifiedName~PreviewSemanticPaletteTests" --artifacts-path .\.artifacts-test\desktop-loop1-final-focused
```

Expected: PASS.

- [x] **Step 2: Run full Desktop verification**

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-loop1-full
```

Expected: PASS.

- [x] **Step 3: Run diff hygiene verification**

```powershell
git diff --check
git diff --stat
git status --short
```

Expected:

- `git diff --check` prints nothing
- the diff only touches the Loop 1 visual-system files and tests

- [x] **Step 4: Do not build**

This repo explicitly forbids builds after changes. Desktop test evidence is the valid completion proof.

---

## Self-review

- This plan intentionally does **not** split `FloorPlanPreviewControl.cs`; that belongs to Loop 2.
- This plan intentionally does **not** split `FloorPlanReviewViewModel.cs`; that belongs to Loop 3.
- This plan intentionally does **not** redesign product behavior; it only gives the repo a cleaner visual token boundary before deeper structural refactors.
