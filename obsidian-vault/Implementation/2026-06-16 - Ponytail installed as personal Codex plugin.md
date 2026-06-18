---
type: implementation
date: 2026-06-16
topic: ponytail-plugin-install
status: installed
source: https://github.com/DietrichGebert/ponytail
---

# Ponytail installed as personal Codex plugin

## What changed

Installed `DietrichGebert/ponytail` as a local personal Codex plugin so it can be tested as a real plugin rather than only as a manual rule.

## Paths

- Source plugin copy: `C:\Users\lucas\plugins\ponytail`
- Personal marketplace: `C:\Users\lucas\.agents\plugins\marketplace.json`
- Installed Codex cache: `C:\Users\lucas\.codex\plugins\cache\personal\ponytail\4.7.0`
- Codex config entry: `[plugins."ponytail@personal"] enabled = true`

## Verification

- Plugin manifest validation passed with `validate_plugin.py`.
- `codex plugin list` shows `ponytail@personal` as `installed, enabled`, version `4.7.0`.
- Installed skills present:
  - `ponytail`
  - `ponytail-audit`
  - `ponytail-debt`
  - `ponytail-help`
  - `ponytail-review`

## Gotcha

PowerShell `Set-Content -Encoding UTF8` wrote the personal `marketplace.json` with a UTF-8 BOM. Codex rejected that file with `expected value at line 1 column 1`. Rewriting the JSON with `System.Text.UTF8Encoding(false)` fixed marketplace discovery.

## How to test

Start a new Codex thread after installation so the plugin skills are loaded into the session skill registry. Then try:

- `Use Ponytail mode for this task.`
- `Review this diff for over-engineering.`
- `/ponytail-review`
- `/ponytail-audit`

