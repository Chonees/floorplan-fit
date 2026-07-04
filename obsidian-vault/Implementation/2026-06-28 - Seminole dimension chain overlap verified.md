# Seminole dimension chain overlap verified

Date: 2026-06-28
Type: Discovery
Scope: Loop 1/Loop 2 footprint verification

## What
The user-summed SEMINOLE chain `6'-10" + 66'-0" + 8'-0"` gives `80'-10"`, but those DXF dimensions are not end-to-end. They overlap by `3'-4"`, so the actual union is `77'-6"`.

## Evidence from extracted SEMINOLE dimensions
- `6'-10"` vertical candidate spans approximately `Y 95.379 -> 177.379` = `82"`.
- `66'-0"` vertical candidate spans approximately `Y 137.379 -> 929.379` = `792"`.
- `8'-0"` vertical candidate spans approximately `Y 929.379 -> 1025.379` = `96"`.

The first two overlap from `Y 137.379 -> 177.379` = `40"` = `3'-4"`.

Therefore:

```text
6'-10" + 66'-0" + 8'-0" - 3'-4" = 77'-6"
```

## Product implication
The app must not infer overall house footprint by blindly summing visible dimensions. It must reason about dimension endpoints/interval union or require curation confirmation for the official footprint.
