# Loop 2 Selectable Auto-Fit Options Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate multiple valid Loop 2 auto-fit plans, display them as human-selectable options, and apply only the selected pinch reductions.

**Architecture:** Deterministic Application code enumerates valid exact-fit plans. OpenAI ranks/explains the deterministic candidates, never inventing geometry. Desktop renders plan cards and applies the selected plan to the preview using existing pinch-marker compression semantics.

**Tech Stack:** C#/.NET, Avalonia, CommunityToolkit.Mvvm, xUnit, OpenAI Responses API.

---

### Task 1: Deterministic multi-option generator

**Files:**
- Create: `src/FloorplanFit.Application/FloorPlans/SitePlanAdjustment/AutoFitSuggestionOptionGenerator.cs`
- Modify: `src/FloorplanFit.Application/FloorPlans/SitePlanAdjustment/AutoFitSuggestionDtos.cs`
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/SitePlanAdjustment/AutoFitSuggestionOptionGeneratorTests.cs`

- [ ] Write tests proving height deficit with two height candidates produces single-group options and a split option.
- [ ] Run focused Application tests and verify RED.
- [ ] Implement generator with exact validator filtering and de-duplication.
- [ ] Run focused Application tests and verify GREEN.

### Task 2: OpenAI ranks deterministic candidates

**Files:**
- Modify: `src/FloorplanFit.Application/Abstractions/IAutoFitPlanSuggester.cs`
- Modify: `src/FloorplanFit.Infrastructure/OpenAi/OpenAiAutoFitPlanSuggester.cs`
- Modify: `tests/FloorplanFit.Infrastructure.Tests/OpenAi/OpenAiAutoFitPlanSuggesterTests.cs`
- Modify: `tests/FloorplanFit.Infrastructure.Tests/Claude/ClaudeAutoFitPlanSuggesterTests.cs`

- [ ] Update tests so suggester receives candidate plans and returns a validated ranked list.
- [ ] Run focused Infrastructure tests and verify RED.
- [ ] Update OpenAI prompt/response parsing to use `plans` array.
- [ ] Keep failure/fallback behavior explicit.
- [ ] Run focused Infrastructure tests and verify GREEN.

### Task 3: Desktop option cards and selected apply

**Files:**
- Modify: `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
- Modify: `src/FloorplanFit.Desktop/SitePlanAdjustmentWindow.axaml`
- Modify: `tests/FloorplanFit.Desktop.Tests/ViewModels/SitePlanAdjustmentPreviewProjectorTests.cs`

- [ ] Write tests proving Suggest populates multiple option view models.
- [ ] Write tests proving Apply compresses only the selected group using current preview geometry.
- [ ] Run focused Desktop tests and verify RED.
- [ ] Add option collection, Apply command, and preview-only compression.
- [ ] Render options as cards/buttons in XAML.
- [ ] Run focused Desktop tests and verify GREEN.

### Task 4: Persist current state and verify focused suite

**Files:**
- Modify: `obsidian-vault/Current State.md`
- Create: `obsidian-vault/Implementation/2026-06-09 - Selectable auto-fit plan options.md`

- [ ] Save Obsidian implementation note.
- [ ] Run focused Application/Desktop/Infrastructure tests.
- [ ] Run `git diff --check`.
