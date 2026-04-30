---
project: floorplan-fit
repo: https://github.com/Chonees/floorplan-fit
status: active
updated: 2026-04-30
---

# Current State

## Project

Floorplan Fit es una herramienta desktop local-first para importar floor plans y site plans en DXF, curar walls, detectar el envelope construible y generar opciones determin?sticas de encaje.

## Canonical Sources

- `MVP-UX.md`
- `TECH-STACK-ARCHITECTURE-DATAFLOW.md`
- `AGENTS.md`

## Product Truth

- Runtime principal: **C# + .NET 10 LTS**
- UI: **Avalonia UI + MVVM**
- Persistencia de producto: **SQLite**
- Geometr?a: **NetTopologySuite**
- DXF: **IxMilia.Dxf** detr?s de interfaces
- Estilo arquitect?nico: **monolito modular local-first**

## Repository Truth

- Repo remoto creado: `Chonees/floorplan-fit`
- Licencia: **MIT**
- Estado actual del repo: documentaci?n base + skill `floorplan-fit-teaching-mode` promovido a gu?a default del repositorio
- La base t?cnica del Slice 1 ya empez?: existen `FloorplanFit.sln`, `global.json`, `Directory.Build.props`, proyectos `src/` para `Contracts`, `Domain`, `Application`, `Infrastructure`, y primer proyecto `tests/` para `Application.Tests`
- Inventario verificado el **2026-04-30** excluyendo `bin/` y `obj/`: el repo hoy tiene un corte chico y recorrible para onboarding exhaustivo, centrado en **Loop 1 / import + Library**, con **60 archivos fuente reales** (`49` en `src/` contando `.csproj`, XAML y C#; `11` en `tests/`). En los archivos verificados todav?a no aparecen m?dulos propios de **Loop 2** (`site plan`, `envelope`, `fit engine`), lo que confirma que la implementaci?n viva actual sigue enfocada en el vertical slice inicial
- Auditor?a de estado re-verificada el **2026-04-30**: `git log --oneline` ya muestra `330737a` (`feat: implement executable slice 1 import pipeline`) como commit vigente tanto en `main` como en `feat/loop1-wall-candidate-extraction`, y `git status --short` est? limpio
- Dataset inicial ya disponible en `PLANS/`: floor plans DXF originales, site plan DXF original y cat?logos JSON que sirven como fixtures reales para arrancar el primer vertical slice
- Floor plan elegido para el primer vertical slice can?nico: `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`
- Los cat?logos JSON en `PLANS/catalog/` pueden usarse como or?culo de verificaci?n inicial para TDD de importaci?n (unidad can?nica, bounding box y metadatos base)
- Los JSON de `PLANS/catalog/` parecen artefactos previos de extracci?n/catalogaci?n, no curaciones can?nicas: incluyen `rooms`, `source_layers`, `block_refs` y `readiness`, pero no campos de curado como `stable_wall_id`, `mobility_level` o `curated_walls`; adem?s sus `source_path` apuntan a `D:\PointAIData\PLANS\originalFloorPlans\...`, lo que refuerza que vienen de un pipeline/dataset externo previo y no de esta codebase actual
- Pol?tica de trabajo vigente: los `DXF` de `PLANS/originalFloorPlans/` son la fuente primaria de verdad para Loop 1; `PLANS/catalog/` queda clasificado como referencia legacy/comparativa y NO como fuente can?nica del dominio
- Decisi?n vigente al 2026-04-29: durante el Slice 1 ejecutable, `PLANS/catalog/*.json` se conserva como or?culo legacy/comparativo; no se elimina todav?a y cualquier artefacto propio generado desde DXF deber? ser secundario, no can?nico
- Verificaci?n al 2026-04-29: hoy no existe ninguna carpeta ni archivo JSON derivado/generado por nuestra pipeline actual; los ?nicos JSON del repo, aparte de config, siguen siendo `PLANS/catalog/santa-barbara.json` y `PLANS/catalog/seminole-2000.json`
- El plan `docs/superpowers/plans/2026-04-25-slice-1-import-foundation.md` fue sincronizado con el estado real del repo el **2026-04-29**; ahora distingue entre trabajo verificado, trabajo pendiente y evidencia hist?rica no demostrable desde git
- La nota `obsidian-vault/Implementation/2026-04-29 - Repository Audit Status.md` qued? **superseded** por la auditor?a del **2026-04-30** porque todav?a afirmaba “un solo commit”, “sin SDK”, “sin Desktop” y “sin adaptadores reales”, cosas que hoy contradicen git, c?digo y validaciones locales
- Decisi?n vigente al **2026-04-30**: para cerrar Loop 1 se aprob? este orden de implementaci?n: **WallCandidate/extracci?n -> review/curation persistida -> publish de versi?n activa -> UI de review/curado**
- Decisi?n vigente al **2026-04-30**: el primer cierre serio de Loop 1 usar? **core sem?ntico + canvas m?nimo real**, evitando tanto el editor CAD rico prematuro como el review ciego sin contexto espacial
- Hallazgo de producto al **2026-04-30**: el usuario aclar? que el MVP debe poder respetar reglas por ambiente (por ejemplo m?nimos de ba?o o qu? espacios no tocar) y captar medidas exactas de habitaciones; eso genera tensi?n con el scope actual `walls-only` y probablemente exige una capa m?nima de **espacios/ambientes + constraints**
- Decisi?n vigente al **2026-04-30**: el dise?o de cierre de Loop 1 queda ajustado a **walls exactas + curated spaces m?nimos + constraints b?sicas por ambiente**, mientras que la **creaci?n de paredes nuevas** nace primero en Loop 2 como propuesta de adaptaci?n

## Knowledge System Truth

- Vault Obsidian creado en `obsidian-vault/`
- Engram local alineado a **1.13.1** mediante shim
- Engram Cloud todav?a **no configurado**
- Antes de cloud, conviene migrar memorias viejas del nombre `floorplan adjustments-to site plan` a `floorplan-fit`
- `floorplan-fit-teaching-mode` debe cargarse junto con `superpowers:using-superpowers` como gu?a base de trabajo en este repo
- Preferencia activa del usuario: mientras implementamos, explicar qu? se hace, por qu? se hace y ense?ar el razonamiento paso a paso en la respuesta
- Preferencia activa del usuario al **2026-04-30**: antes de seguir empujando cambios, quiere entender el repo a nivel **exhaustivo** (`qu? hace cada archivo`, `qu? hace cada funci?n`, `c?mo se conecta todo`) y usar esa lectura como base de trabajo
- Preferencia activa del usuario: documentar cada paso importante en `docs/` y `obsidian-vault/` a medida que avancemos
- Preferencia activa del usuario: evitar terminolog?a avanzada no introducida todav?a (por ejemplo `aggregate`) y reemplazarla por lenguaje pedag?gico simple como `entidad principal`, `objeto central` o `grupo de datos relacionado`
- Documento formal del dise?o creado en `docs/superpowers/specs/2026-04-25-slice-1-import-foundation-design.md`
- Documento formal del dise?o ejecutable creado en `docs/superpowers/specs/2026-04-29-slice-1-executable-design.md`
- Documento formal del cierre de Loop 1 creado en `docs/superpowers/specs/2026-04-30-loop-1-curated-walls-and-spaces-design.md`
- Plan de implementaci?n creado en `docs/superpowers/plans/2026-04-25-slice-1-import-foundation.md`
- Plan ejecutable detallado creado en `docs/superpowers/plans/2026-04-29-slice-1-executable-implementation.md`
- Documento pedag?gico creado en `docs/explicacion del proyecto/2026-04-25 - explicacion de archivos tocados en slice 1.md` para explicar qu? hace cada archivo tocado
- Documento pedag?gico exhaustivo agregado en `docs/explicacion del proyecto/2026-04-29 - explicacion exhaustiva de archivos del slice 1 ejecutable.md` para cubrir archivo-por-archivo los componentes nuevos de `FloorplanFit.Desktop`, `FloorplanFit.Infrastructure` y `FloorplanFit.Infrastructure.Tests`
- Cambio de entorno aplicado el **2026-04-29**: se instal? `.NET SDK 10.0.100` y `dotnet --info` ya queda alineado con `global.json`; el bloqueo de “no SDK” ya no existe
- Cambio de entorno aplicado el **2026-04-29**: Smart App Control fue desactivado temporalmente en Windows para destrabar assemblies de test y tareas de Avalonia que estaban siendo bloqueados por Application Control (`0x800711C7`)
- Validaci?n real avanzada el **2026-04-29**: `FloorplanFit.Application.Tests` pasa (**4/4**), `FloorplanFit.Infrastructure.Tests` pasa (**6/6**), `FloorplanFit.Desktop` compila sin warnings en build de verificacion y el smoke test manual base de Desktop/import tambi?n fue validado en esta m?quina
- Estandarizacion de desarrollo aplicada el **2026-04-29**: el launcher recomendado para iterar Desktop ya no es el `.exe` de `bin/Debug/net10.0` sino `scripts/dev-desktop.bat`, que corre `dotnet watch run` sobre `src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj` y evita confundir binarios viejos con cambios nuevos
- Conveniencia local agregada el **2026-04-29**: existe un acceso directo `Floorplan Fit Dev Watch.lnk` en el escritorio que apunta al launcher `scripts/dev-desktop.bat`
- Sincronizacion documental aplicada el **2026-04-29**: `docs/explicacion del proyecto/2026-04-29 - explicacion exhaustiva de archivos del slice 1 ejecutable.md` fue reescrito para reflejar el estado final del dia, incluyendo query de Library, rehidratacion desde SQLite, bugfix de reimport y launcher dev `.bat`
- Pasada final de sincronizacion documental aplicada el **2026-04-29**: se corrigieron conteos de tests, referencias viejas a “no SDK”, estado abierto de `AVLN3001` y se marcaron como historicos los planes que ya no representan el codigo actual
- Riesgo operativo documentado al **2026-04-29**: el acceso directo de escritorio a `FloorplanFit.Desktop.exe` sigue sirviendo para smoke tests manuales puntuales, pero NO es la ruta recomendada para desarrollo porque puede apuntar a un output stale
- Decisi?n vigente al 2026-04-29: el Slice 1 ejecutable copiar? los DXF importados a un workspace administrado por la app (`library/raw-dxf/`) y persistir? esa ruta gestionada como `storage_path`
- Correcci?n de dise?o al 2026-04-29: despu?s de copiar el DXF al workspace gestionado, la app leer? metadata/fingerprint y calcular? hash sobre esa copia gestionada, no sobre la ruta externa original
- Implementaci?n parcial verificada al 2026-04-29: `ImportFloorPlanHandler` ya fue adaptado para depender de `IManagedFileStorage` y operar sobre la copia gestionada durante el flujo de import
- Implementaci?n verificada al 2026-04-29: ya existen `AppWorkspace`, `ManagedFileStorage`, `Sha256FileHashService`, `IxMiliaDxfGateway`, `SqliteSession`, `SqliteSchemaInitializer`, repositorios SQLite y `SqliteUnitOfWork`, adem?s de tests de infraestructura para storage, DXF real e integraci?n SQLite que ya corren en esta m?quina
- Costuras corregidas durante la validaci?n del **2026-04-29**: se agregaron `GlobalUsings` para `Xunit`, se sane? el test del gateway DXF para validar el raw DXF real en vez del bbox legacy, se limpiaron pools de `SqliteConnection` para liberar `app.db` en tests temporales y se agregaron `x:DataType` compilados en `MainWindow.axaml` para destrabar el build de Avalonia
- Fix verificado al **2026-04-29**: `AVLN3001` desaparecio del build de verificacion despues de mover el `DataContext` de `MainWindow` fuera del constructor inyectado y dejar la ventana con constructor publico sin parametros
- Bugfix verificado al **2026-04-29**: reimportar el mismo DXF ya no crea `santa-barbara-2` como template nuevo; ahora preserva la identidad l?gica desde `request.FilePath` y genera `version 2` del mismo template aunque la copia gestionada tenga sufijo f?sico
- Hallazgo de auditor?a al **2026-04-30**: el workspace manual en `src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/app.db` hoy contiene `santa-barbara`, `santa-barbara-2` y `santa-barbara-3` como templates separados; eso NO invalida el bugfix automatizado, porque la DB guarda `original_file_name` con esos mismos sufijos y por lo tanto refleja imports manuales de copias gestionadas/stale artifacts, no reimport del mismo `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`

## Immediate Next Steps

1. Reabrir la app desde `scripts/dev-desktop.bat` con un workspace limpio o controlado y confirmar visualmente que la Library reaparece desde SQLite al iniciar en una nueva sesi?n de UI
2. Revalidar manualmente el caso “reimport del mismo original” seleccionando siempre `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`, no las copias gestionadas dentro de `workspace/library/raw-dxf/`
3. Reusar `PLANS/` como fuente inicial de fixtures DXF y cat?logos para seguir TDD real sobre importaci?n, normalizaci?n y extracci?n
4. Construir el primer vertical slice operativo sobre `SANTA-BARBARA`: importaci?n real -> unidades -> persistencia real -> library entry -> base para extracci?n

## Recommended Implementation Start

Primero hay que construir la **base t?cnica + Loop 1**, no el fit engine ni el site plan.

Orden recomendado:

1. Crear la soluci?n `.NET` con las capas definidas en `TECH-STACK-ARCHITECTURE-DATAFLOW.md`
2. Implementar el flujo can?nico de floor plan: **import DXF -> normalizaci?n de unidades -> extracci?n de wall candidates -> review/curation -> publicaci?n persistida**
3. Persistir desde el inicio la curaci?n publicada como fuente can?nica reutilizable
4. Reci?n despu?s atacar Loop 2: envelope extraction, overlay 1:1 y propuestas de fit
5. Para bootstrap y validaci?n inicial, usar `SANTA-BARBARA` como caso conductor y `SEMINOLE2000` como fixture de robustez/regresi?n

## Why This Start

- `MVP-UX.md` define dos loops y deja claro que el **Loop 1 se hace una vez y habilita todos los casos futuros**
- `TECH-STACK-ARCHITECTURE-DATAFLOW.md` dice expl?citamente que el motor trabaja contra `FloorPlanCuration` y `CuratedWalls`, no contra DXF crudo
- Sin esa base can?nica, empezar por el fit engine o por el envelope ser?a construir sobre arena

## Relevant Notes

- [[Implementation/Vault Bootstrap]]
- [[Decisions/2026-04-25 - Project Identity and Knowledge Stack]]
- [[Decisions/2026-04-25 - Initial Implementation Order]]
- [[Decisions/2026-04-25 - Floorplan Teaching Skill Always On]]
- [[Decisions/2026-04-25 - Santa Barbara as First Canonical Fixture]]
- [[Decisions/2026-04-25 - DXF as Primary Truth and Catalog as Legacy Reference]]
- [[Decisions/2026-04-25 - Slice 1 Import Foundation Architecture]]
- [[Decisions/2026-04-30 - Loop 1 Completion Order]]
- [[Decisions/2026-04-30 - Loop 1 Uses Semantic Core Plus Minimal Review Canvas]]
- [[Decisions/2026-04-30 - Loop 1 Adds Minimal Curated Spaces and Defers New Walls To Loop 2]]
- [[Implementation/2026-04-25 - Slice 1 Files Explanation Map]]
- [[Implementation/2026-04-30 - Requirement Tension Between Walls-Only and Room Constraints]]
- [[Implementation/2026-04-30 - Loop 1 Curated Walls and Spaces Design]]
- [[Implementation/2026-04-29 - Repository Audit Status]]



