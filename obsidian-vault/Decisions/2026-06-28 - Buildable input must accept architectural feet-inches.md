# Buildable input must accept architectural feet-inches

Date: 2026-06-28
Type: Decision
Scope: Loop 2 Adjust-to-Site-Plan

## Decision
The Adjust-to-Site-Plan buildable-area input must accept architect-facing feet/inches notation, not only decimal feet.

## Why
Architects and CAD users think and document dimensions like `38'0"` x `65'7"`, not `38` x `65.583333333`.

Decimal feet is unsafe for exact construction-facing input because many inch values are repeating decimals in feet:

- `77'6"` = `77.5 ft` works cleanly.
- `65'7"` = `65.583333... ft` does not terminate.

## Product implication
The UI should allow exact entries such as:

- `38'0"` / `65'7"`
- or separate feet + inches fields

The app should convert those to exact source inches before writing the synthetic DXF.

## Not chosen
Do not solve this primarily by loosening tolerance. Tolerance can hide display noise, but the root problem is the wrong input format.
