# Santa Barbara exact feet-inches input loses precision

Date: 2026-06-28
Type: Bug
Scope: Loop 2 Adjust-to-Site-Plan simulation

## What
Entering the Santa Barbara exact height as a shortened decimal feet value can make the simulated site plan report a fit deficit even though the architect-facing size is `38'0" x 65'7"`.

## Evidence
- `SANTA-BARBARA` structural fit footprint: `456"` x `787"` = `38'0"` x `65'7"`.
- The Adjust setup dialog parses only decimal feet via `decimal.TryParse(...)`.
- The synthetic writer multiplies decimal feet by `12`.
- `65.583 ft * 12 = 786.996"`, which is `0.004"` short of exact `787"`.
- App deficit rounding keeps that as `0.004"`, so the fit reports not fitting.
- `65.5833 ft * 12 = 786.9996"`, which rounds to `0.000"` deficit and fits.

## Root cause
The UX asks for decimal feet but the product truth is architectural feet+inches. A value like `65'7"` is exact in architectural notation but repeating in decimal feet (`65.583333...`).

## Direction
Accept feet+inches input (`65'7"`, `65 7`, or separate ft/in fields) or display enough decimal precision when giving values intended for direct input.
