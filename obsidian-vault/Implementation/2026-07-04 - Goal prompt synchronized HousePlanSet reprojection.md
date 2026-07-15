---
type: implementation
created: 2026-07-04
status: active
---

# Goal prompt - synchronized HousePlanSet reprojection

Prompt robusto para la próxima goal: implementar sincronización senior FloorPlan -> dependientes sin loop infinito.

## Core
- FloorPlan es fuente canónica geométrica.
- Electrical/Roof/Facade son dependientes registrados.
- Cambios del FloorPlan producen receta versionada y reproyección desde fuentes originales.
- Cambios locales de dependientes son overrides locales, no mutan el FloorPlan automáticamente.
- Stop condition: dependents marked stale, reprojected, audited, and verified with evidence.

## Anti-loop
- Max 3 attempts per same failing symptom.
- If the same symptom repeats, stop and report root cause / architecture gap.
- Never patch blindly; inspect code and prove with a minimal check.
