---
project: floorplan-fit
type: inbox
date: 2026-05-04
tags:
  - loop-1
  - desktop
  - pinch
  - ux
---

# Pinch placement UX gotchas in prototype

- El pinch tool actual solo se arma si hay `SelectedCandidate` y `SelectedPinchGroup`.
- Para colocar un pinch, el usuario debe:
  1. seleccionar una l?nea/candidate
  2. seleccionar un pinch group
  3. armar la herramienta
  4. clickear **la misma l?nea** en el preview
- Si el click cae sobre otra l?nea, el sistema cambia la selecci?n y desarma el pinch placement.
- Si el click cae cerca del borde del preview, entra en modo edge-drag preview en vez de colocar pinch.
- Hoy no hay indicador visual fuerte de "pinch armado" fuera del `StatusMessage`, por eso la UX puede sentirse confusa.
