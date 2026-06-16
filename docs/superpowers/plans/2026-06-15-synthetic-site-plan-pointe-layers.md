# Synthetic Site Plan Pointe Layers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Regenerate our synthetic site-plan DXFs so they live with the site-plan corpus and use the same Pointe Homes layer appearance as `158 DAWSON STREET.dxf`, changing only setback/buildable geometry per scenario.

**Architecture:** Keep the existing Python generator as the fixture authoring boundary, but make the layer table source-compatible with the real Pointe DXF: names, colors, linetypes, lineweights, plot style, material, and shadow metadata. Add a local Python verifier that reads DXF group-code pairs and proves generated files match the reference layer table and target output directory.

**Tech Stack:** Python DXF group-code text generation, PowerShell invocation, existing DXF fixture corpus under `D:\PointAIData\PLANS\originalsSitePlans`.

---

### Task 1: RED layer fidelity verifier

**Files:**
- Create: `verify_synthetic_siteplans.py`

- [ ] **Step 1: Write the failing verifier**

Create a Python script that parses `D:\PointAIData\PLANS\originalsSitePlans\158 DAWSON STREET.dxf`, then checks every generated `SYNTH SITE PLAN *.dxf` in that same directory for the same five Pointe layers and required values:

- `0`: color `7`, linetype `Continuous`, lineweight `-3`, plotstyle `F`, material `98`, shadow `0`
- `TEXT`: color `6`, linetype `Continuous`, lineweight `-3`, plotstyle `F`, material `98`, shadow `0`
- `E`: color `4`, linetype `Continuous`, lineweight `20`, plotstyle `F`, material `98`, shadow `0`
- `SETBACKS`: color `31`, linetype `Continuous`, lineweight `13`, plotstyle `F`, material `98`, shadow `0`
- `2312-001-BM$0$C-PROP-SUBD`: color `7`, linetype `2312-001-BM$0$PHANTOM2`, lineweight `-3`, plotstyle `F`, material `98`, shadow `0`

- [ ] **Step 2: Run verifier before generator changes**

Run: `python verify_synthetic_siteplans.py`
Expected RED: fails because no `SYNTH SITE PLAN *.dxf` files exist in `D:\PointAIData\PLANS\originalsSitePlans` yet, and/or existing synth exports omit `370/390/347/348` layer metadata.

### Task 2: Update generator

**Files:**
- Modify: `generate_synthetic_siteplans.py`

- [ ] **Step 1: Change output root**

Set output to `D:\PointAIData\PLANS\originalsSitePlans` so generated synths sit beside real site plans.

- [ ] **Step 2: Emit Pointe-compatible layer records**

Update the layer table to include the same metadata tail as the real sample: `370`, `390`, `347`, `348`. Keep `SETBACKS` as the only variable semantic layer for buildable geometry; all core layer names/appearance match Pointe.

- [ ] **Step 3: Preserve existing scenario geometry**

Keep the six existing synthetic cases and exact deficit math. Do not change deficit semantics while fixing layer fidelity.

### Task 3: Generate and verify

**Files:**
- Runtime output: `D:\PointAIData\PLANS\originalsSitePlans\SYNTH SITE PLAN *.dxf`

- [ ] **Step 1: Run generator**

Run: `python generate_synthetic_siteplans.py`
Expected: six `SYNTH SITE PLAN *.dxf` files are generated in `D:\PointAIData\PLANS\originalsSitePlans`.

- [ ] **Step 2: Run verifier GREEN**

Run: `python verify_synthetic_siteplans.py`
Expected: PASS; every generated synth has the same required Pointe layer appearance metadata and contains `SETBACKS` geometry.

### Task 4: Document current truth

**Files:**
- Modify: `obsidian-vault/Current State.md`
- Modify/Create: `obsidian-vault/Implementation/2026-06-15 - Synthetic site plans use Pointe layer appearance.md`

- [ ] **Step 1: Record implementation**

Document that synthetic site plans now use Pointe layer appearance and live in the site-plan corpus.

- [ ] **Step 2: Shadow-save to Engram**

Save the implementation discovery/config change to Engram after Obsidian is updated.
