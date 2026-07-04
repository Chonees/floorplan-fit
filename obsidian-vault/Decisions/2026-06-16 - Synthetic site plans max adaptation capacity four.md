---
type: decision
date: 2026-06-16
project: floorplan-fit
status: accepted
replaces:
  - Implementation/2026-06-15 - Synthetic site plans use Pointe layer appearance.md#Generated files
related:
  - ../Current State.md
  - ../Implementation/2026-06-15 - Synthetic site plans use Pointe layer appearance.md
tags:
  - floorplan-fit
  - loop2
  - site-plan
  - synthetic-fixtures
  - adaptation-capacity
---

# Synthetic site plans max adaptation capacity four

## Decision

All generated synthetic site-plan DXFs must stay within the current Loop 2 adaptation capacity ceiling: **no synthetic plan may require more than 4 inches of adaptation**.

Shape variation is still required. Synthetic lots may use rectangles, chamfers, fillets, curved fronts, trapezoids, and combined shapes, but those shapes must not turn the fixture into an over-capacity case unless it is explicitly marked as a negative/torture fixture.

## Why

The synthetic DXFs are product fixtures for validating normal `Adjust to Site Plan` behavior. If a shape-bearing synthetic plan requires more than the allowed adaptation capacity, the operator can misread the failure as an AI/fit problem when the fixture itself is invalid for the intended capacity envelope.

## Verified current gap

`generate_synthetic_siteplans.py` currently defines these over-capacity cases in `CASES`:

- `SETBACK CON FILLETS` uses `ancho` deficit `5.0`.
- `CHAFLAN Y FRENTE CURVO` uses `alto` deficit `5.0`.

Both exceed the clarified capacity ceiling of `4`.

## Operational rule

- Positive/normal synthetic site plans: required adaptation must be `<= 4` inches.
- If we ever need `> 4` examples, they must be named and documented as invalid/negative cases, not mixed into the normal synthetic corpus.
- The verifier should eventually fail normal synth generation if any generated case exceeds the `4` inch ceiling.

## Next implementation direction

Reduce or redesign the current `5.0` deficit cases to `<= 4.0`, then update `verify_synthetic_siteplans.py` so this rule becomes executable evidence instead of tribal knowledge. Es asi de facil: si es regla de producto, tiene que vivir en un test/verifier, no en nuestra memoria.

## Implemented

Implemented by `../Implementation/2026-06-16 - Synthetic site plans use descriptive deficit titles.md`.

Normal generated synths now use descriptive filenames and `1"`/`2"` width/height deficits. The former shaped `5.0` cases were reduced to `2.0`, and the verifier now enforces expected deficits within the `4"` capacity ceiling.
