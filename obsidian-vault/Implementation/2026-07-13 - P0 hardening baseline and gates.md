# 2026-07-13 - P0 hardening baseline and gates

## Phase
Active goal — Phase 4 complete; Phase 5 pending

## Baseline
- Branch: `STAGING`
- HEAD: `afebe7c`
- Tracked modified before P0 production work: `30`
- Untracked before the first P0 check: `73`
- Existing tracked diff: about `6501` insertions / `1346` deletions after documentation updates.
- No production file has been changed by the P0 goal yet.

Pre-existing dirty target files that must be preserved:
- `ConfirmSheetAdjustmentProjectionHandler.cs`
- `ExportMultiSheetPlanSetPackageHandler.cs`
- `CreateMultiSheetExportAuditHandler.cs`
- `PlanSetExportManifestWriter.cs`

The persistence, workspace, registration, deletion, and cleanup files targeted by Phases 1-2 were clean at baseline.

## P0 matrix

| P0 | Current evidence | First runnable red check | Minimum likely production files | Binary green gate |
|---|---|---|---|---|
| Unknown `pinch_markers` schema is dropped | `SqliteSchemaInitializer.cs:1141-1164,1314-1319` calls `DROP TABLE IF EXISTS pinch_markers` | `scripts/test-p0-pinch-marker-migration-safety-contract.ps1` | `SqliteSchemaInitializer.cs`; focused Infrastructure regression test | Unknown shape preserves rows and startup fails closed; current shape and supported legacy migrations remain idempotent |
| Workspace is executable-relative and has no coherent backup | `Program.cs:18`; `AppWorkspace.cs:12-18`; no backup implementation | `scripts/test-p0-workspace-durability-contract.ps1` | `Program.cs`, `AppWorkspace.cs`, minimal workspace migration/backup service, DI, focused tests | New writes use LocalApplicationData; legacy workspace is copied not deleted; DB + managed files have one backup boundary |
| Registration canonical ID/reference deletion/cleanup are unsafe | registration passes PlanSetVersionId as canonical ID; delete handler has no reference guard; cleanup deletes source rows before geometry lookup | `scripts/test-p0-canonical-identity-contract.ps1` | three registration handlers or one shared minimum workflow; PlanSetVersion repository port; remove handler; cleanup; schema migration/repositories; tests | Actual canonical ID persisted/repaired; cross-aggregate request rejected; referenced version deletion rejected; cleanup deletes owned geometry without orphans |
| Roof/Facade manual confirmation can promise unsupported compression | confirmation changes status; export recipe is Electrical-only | `scripts/test-p0-sheet-capability-contract.ps1` | typed capability/status contract; confirmation handler; export audit path; tests | Unsupported Roof/Facade compression never becomes Ready; compatible Electrical remains exportable |
| `ReadyForExport` is decided before final verification | `CreateMultiSheetExportAuditHandler.cs:111-150` decides status before writer builds final artifacts | `scripts/test-p0-verification-gate-contract.ps1` | typed verification DTO/report; handler; audit builder/writer; tests | pass/mismatch/missing/unsupported cases prove status derives from report before persistence |
| Package publication is partial/non-atomic | canonical/dependents/directories/manifest are written before final DB completion; manifest is written before audit files | `scripts/test-p0-atomic-package-contract.ps1` | package handler; storage writer; minimal staging helper; tests | success publishes once; failure/cancel removes staging; manifest is last and no partial final package exists |

## First red proof

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\test-p0-pinch-marker-migration-safety-contract.ps1"
```

Observed result: exit code `1` with:

```text
P0: unknown pinch_markers schema must never be dropped without preserving its rows.
```

This proves the Phase 1 first defect before production changes.

## Phase 0 gate
- Baseline captured: yes.
- Six P0s verified against current worktree: yes.
- P0 -> red check -> minimum files -> green gate matrix: yes.
- First runnable red check observed: yes.
- Production changes before first red check: none.

Phase 0 is green. Next work starts at Phase 1 and must not touch unrelated dirty HousePlanSet files.

## Phase 1 progress - destructive pinch-marker fallback removed
- Added `SqliteSchemaInitializerTests.InitializeAsync_preserves_unknown_pinch_marker_schema_and_fails_closed` before the production edit.
- Unknown `pinch_markers` column shapes now throw an explicit `InvalidOperationException` and report the observed columns; the existing table and rows are left untouched.
- The old `DROP TABLE IF EXISTS pinch_markers` fallback was removed, including the unsafe missing-dependency branch of the curated-wall migration.
- Added an idempotency regression for the current eight-column schema.
- Static contract is green: `scripts/test-p0-pinch-marker-migration-safety-contract.ps1` exits `0`.
- `git diff --check` is green for the touched implementation, regression test, and contract script.
- Per repository policy, no `dotnet build`, `dotnet test`, or `dotnet watch` was run; executable xUnit evidence remains for the allowed final verification environment.

## Phase 1 progress - SQLite version and connection policy
- Introduced the first explicit compatibility boundary through `PRAGMA user_version`; the transitional version `1` is superseded by Phase 2 schema version `2` below.
- Databases with a newer version fail closed before schema mutation.
- Centralized the three production connection entry points behind `SqliteConnectionPolicy`, which enables foreign keys and a 5-second busy timeout.
- Added regressions for version initialization/idempotency, newer-version preservation, and connection PRAGMAs.
- Both Phase 1 PowerShell contracts and `git diff --check` pass; executable xUnit evidence remains pending by repository policy.
- Deliberate tradeoff: the legacy version-0 bootstrap was not rewritten into one giant transaction in this goal. It remains restartable/idempotent, while each shape-changing pinch migration keeps its own transaction; the pre-migration workspace backup is the next safety layer.

## Phase 1 progress - stable workspace migration and pre-migration backup
- Desktop writes now target `%LOCALAPPDATA%/FloorplanFit/workspace`, not the executable output directory.
- On first use, the old `AppContext.BaseDirectory/workspace` tree is copied through a staging directory and atomically renamed; the legacy source is never deleted or merged over an existing non-empty target.
- Before an older database is migrated, the complete managed workspace is staged under `%LOCALAPPDATA%/FloorplanFit/backups/pre-schema-v{target}`; the current target is `pre-schema-v2`.
- The database portion uses SQLite `BackupDatabase`; all other files under the workspace root are copied in the same quiescent startup boundary. External user-chosen/desktop exports remain outputs, not managed source-of-truth state.
- The backup is published by directory rename, retained once per target schema version, and startup fails rather than migrating when backup creation fails.
- Regressions specify stable paths, copy-not-delete migration, no merge into an existing target, and one-time DB + managed-file snapshot behavior.
- All three Phase 1 PowerShell contracts and `git diff --check` pass; no .NET command was run.

## Phase 2 progress - canonical identity, reference guards, and owned cleanup
- Registration now resolves the owning `PlanSetVersion` and persists its real `CanonicalFloorPlanVersionId`; it no longer aliases `PlanSetVersionId` as canonical identity.
- The repository port gained `GetByIdAsync`, so Electrical, Roof, and Facade registration all verify the requested aggregate and the dependent sheet's ownership before writing.
- Schema version `2` repairs legacy registration IDs transactionally, rejects orphan/cross-version registrations, then installs SQLite triggers that enforce registration identity and block soft/hard deletion of a canonical HousePlanSet FloorPlan.
- The application delete handler performs the same canonical reference guard before soft deletion, producing a domain-facing error instead of relying only on a SQLite trigger.
- Startup cleanup now captures every owned geometry path before deleting its owner rows, deletes all measurement/binding curation dependents, and removes geometry only when no surviving row references it.
- Focused regressions specify canonical persistence, cross-version rejection, delete blocking, v1 identity repair/trigger activation, and orphan-free curation geometry cleanup.
- `scripts/test-p0-canonical-identity-contract.ps1` and all earlier Phase 1 contracts pass; `git diff --check` is green. Executable xUnit remains pending by repository policy.

## Phase 3 complete - honest sheet capabilities
- First red command: `powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\test-p0-sheet-capability-contract.ps1"`.
- Exact initial failure: `P0: SheetAdjustmentProjectionStatus does not expose the fail-closed Unsupported state.`
- Added `SheetAdjustmentProjectionStatus.Unsupported` and a small typed Domain capability policy keyed by `SheetAdjustmentProjectionMethod`; no summary text participates in gating.
- Electrical remains affine + canonical-compression capable. Roof and Facade remain affine-only.
- Roof/Facade projection handlers now classify any canonical compression as `Unsupported` and explain that manual confirmation cannot make it exportable.
- Confirmation enforces capability before its Ready early return; projected export enforces the same policy before status-based export, closing both manual-promotion and legacy-Ready bypasses.
- Focused regressions cover Roof and Facade classification, manual and legacy-Ready confirmation rejection, legacy-Ready export rejection, and retained Electrical confirmation/export behavior.
- Exact green output: `P0 sheet capability contract passed`.
- Earlier P0 contracts also remain green, and `git diff --check` exits `0` (line-ending warnings only).
- No `dotnet` command was executed; executable test evidence remains pending under repository policy.
- Deliberate boundary: this phase does not implement Roof/Facade deformation geometry, final verification gating, or atomic package publication.

## Related
- [[2026-07-13 - Finite P0 hardening goal]]
- [[2026-07-13 - Whole app robustness and modularization audit]]

## Fase 4 - typed verification gate

### RED

`P0: the typed PlanSetVerificationReportDto contract is missing. Missing file: src/FloorplanFit.Contracts/PlanSets/PlanSetVerificationReportDto.cs`

### GREEN

`P0 typed verification gate contract passed`

### Flujo cerrado

1. Application arma evidencia de sheets/exports sin decidir status.
2. Infrastructure agrega los audits existentes en `PlanSetVerificationReportDto` schema v1.
3. Recién entonces Application deriva y persiste `PlanSetExportStatus` desde `verificationReport.IsGreen`.
4. Manifest schema v2 incluye el mismo reporte; writer rechaza status/summary contradictorios.
5. Desktop sólo anuncia listo con reporte verde y PowerShell rechaza Ready sin esa decisión.

### Checks permitidos

- Fases 1-3 P0: verdes.
- Fase 4: verde.
- `git diff --check`: exit 0; sólo warnings LF/CRLF.
- Cero comandos `dotnet`.

### Pendiente

- xUnit no ejecutado por restricción explícita.
- Publicación atómica pertenece exclusivamente a Fase 5 y no fue tocada.
## Fase 5 - publicacion atomica

> Superseded by [[2026-07-13 - Fase 5 package rename preceded verification gate]]: el primer patch hacia el rename del paquete de usuario antes de verificar y publicar el manifest del workspace.

- RED inicial: `P0 atomic package contract is missing 'src/FloorplanFit.Contracts/PlanSets/PlanSetExportFailureDto.cs'.`
- GREEN final: `P0 atomic package contract passed`.
- `AtomicDirectoryPublisher` genera en staging sibling, rechaza finales existentes, publica con un unico rename y permite compensar un paquete nuevo si una etapa posterior falla.
- `ExportMultiSheetPlanSetPackageHandler` exporta dependientes al staging y persiste solamente sus paths finales; exporter failure y cancelacion limpian staging y registran `Failed` best-effort.
- `PlanSetExportManifestWriter` escribe nueve audits antes de `manifest.json` y publica el paquete workspace solo al completar todos los archivos.
- `CreateMultiSheetExportAuditHandler` conserva el gate tipado de Fase 4, registra failure JSON antes del commit cuando es posible y revierte el workspace package ante una falla de persistencia.
- Pruebas xUnit focalizadas cubren success, exporter failure, writer failure, cancelacion y preservacion de un final previo; no fueron ejecutadas por la regla de cero `dotnet`.

## Fase 5 corregida - el paquete final permanece oculto hasta verificar

`replaces`: [[2026-07-13 - Fase 5 package rename preceded verification gate]] y la primera implementacion de Fase 5 documentada arriba.

### Problema verificado

La primera version hacia visible el `PackageDirectory` del usuario antes de construir el reporte tipado y publicar los audits del workspace. Era un rename atomico, pero el orden global seguia siendo incorrecto: un observador podia encontrar un paquete que todavia no habia superado el gate.

### Orden vigente

1. Exportar los DXF dependientes a un staging sibling.
2. Leer ese staging mediante `VerificationPath`, que no se serializa.
3. Construir `PlanSetVerificationReportDto`.
4. Publicar atomicamente los audits del workspace y escribir `manifest.json` ultimo.
5. Renombrar una sola vez el staging del usuario a `PackageDirectory`.
6. Persistir el resultado exitoso en SQLite.

`StoragePath` siempre representa el path final que va al manifest y a la base. Ningun `.staging-*` forma parte del contrato persistido.

### Compensacion y limites

- Error o cancelacion limpia staging.
- Si falla una etapa posterior al rename, se retira solamente el final nuevo y se conserva cualquier destino preexistente.
- `Failed` se persiste best-effort con causa tipada sin ocultar la excepcion original.
- Filesystem y SQLite no comparten una transaccion distribuida: la garantia es publicacion atomica por directorio mas rollback compensatorio.

### Evidencia estatica

- RED del orden: `P0 RED: atomic publication still has no callback boundary between staging and final rename.`
- RED adicional de inspeccion: `P0: the atomic publication result is not returned by the package handler.`
- GREEN: `P0 atomic package contract passed`.
- Los contratos anteriores, el self-check del verifier y `git diff --check` permanecieron verdes.
- Los tests xUnit fueron escritos pero no ejecutados por la prohibicion explicita de comandos `dotnet`.

## Fase 6 - cierre estatico verificable

### Evidencia final permitida

| Gate | Resultado |
| --- | --- |
| Pinch-marker migration safety | PASS |
| SQLite durability | PASS |
| Workspace durability | PASS |
| Canonical identity | PASS |
| Sheet capability policy | PASS |
| Typed verification gate | PASS |
| Atomic package publication | PASS |
| Manifest verifier self-check | PASS |
| `git diff --check` | PASS; solo avisos LF/CRLF |

La inspeccion de cierre encontro `FloorplanFit.sln` y un worktree compartido muy sucio (`58 files changed, 9162 insertions, 1509 deletions` en el diff tracked). No se intento limpiar ni atribuir ese conjunto entero a esta goal.

### Prueba externa pendiente, no ejecutada aqui

```powershell
cd "C:\Users\lucas\OneDrive\Escritorio\floorplan adjustments-to site plan"
dotnet test .\FloorplanFit.sln
```

Para la prueba runtime, el usuario o CI debe iniciar la aplicacion y generar una exportacion con nombre/path nuevo:

```powershell
.\scripts\dev-desktop.bat
```

Despues de exportar un HousePlanSet fresco:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify-latest-plan-set-recipe-manifest.ps1" -RequireAutomatic
```

El comando debe confirmar manifest schema v2, verification schema v1, decision `ReadyForExport` y congruencia final verde. Ninguna de esas pruebas `.NET`/runtime se ejecuta ni se presume en este cierre.

### Limite de la goal

Esta goal cierra los P0 enumerados, no la modularizacion general de la app. Tampoco implementa deformacion Roof/Facade. Esos temas requieren una propuesta separada con sus propios contratos y gates.
