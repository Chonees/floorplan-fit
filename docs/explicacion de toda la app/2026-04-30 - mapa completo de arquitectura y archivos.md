---

type: architecture-map

date: 2026-04-30

last_verified: 2026-05-04

status: active

scope: current-working-tree-files

file_count: 209

---



# Mapa completo de arquitectura y archivos de Floorplan Fit



## Propósito del documento



Este documento es el **mapa exhaustivo del repositorio**. Su misión es explicar **qué parte de la arquitectura toca cada archivo**, **qué responsabilidad tiene** y **cómo encaja en el objetivo del producto**.



La cobertura fue revalidada contra `git ls-files`, `git ls-files --deleted` y los archivos nuevos relevantes del working tree el **2026-05-04**. El total cubierto es **209 archivos relevantes del working tree**: **202 archivos versionados presentes** m?s **7 archivos nuevos de esta pasada**. Esto sigue excluyendo a prop?sito `bin/`, `obj/`, caches locales, procesos temporales y directorios no versionados irrelevantes, porque **no son fuente can?nica de la app**.



## Actualización 2026-05-03



- se incorporaron los archivos nuevos del fix de review UI: spec, plan, helper de preview, metadato de ensamblado, tests y bitácora de implementación

- el mapa ahora cubre **202 archivos versionados presentes** m?s **7 archivos nuevos relevantes del working tree**

- se mantuvo el mismo patrón de redacción de **misión + importancia + use case** para las piezas nuevas



## Actualización 2026-05-02



- se revalidó la cobertura real contra `git ls-files`

- se incorporaron los archivos agregados desde la creación inicial del mapa

- cada entrada ahora explica **misión + importancia + use case** para que el documento sirva no solo para navegar, sino también para entender por qué cada pieza importa



## Big Picture



Floorplan Fit es un monolito modular local-first en .NET 10 con Avalonia, SQLite e IxMilia DXF. Hoy la parte más desarrollada del producto es **Loop 1**: importar un floor plan, extraer walls, revisarlas, curarlas y publicar una versión reusable. Loop 2 (adaptación contra site plan, fit engine y propuestas) sigue mayormente como diseño.



## Mapa de arquitectura



- **Raíz del repo**: gobierno técnico, solución, fuentes canónicas y configuración global.

- **PLANS/**: fixtures DXF y referencias legacy usadas como verdad de entrada o comparación.

- **docs/**: documentación narrativa, specs y planes ejecutables.

- **obsidian-vault/**: conocimiento duradero del proyecto para estado, decisiones, bugs e implementación.

- **skills/**: skill local que obliga a explicar el repo con foco en loops y capas.

- **src/FloorplanFit.Domain/**: reglas puras del negocio.

- **src/FloorplanFit.Application/**: puertos y casos de uso.

- **src/FloorplanFit.Contracts/**: DTOs de intercambio para UI/read-models.

- **src/FloorplanFit.Infrastructure/**: SQLite, DXF, storage, hashing y runtime local.

- **src/FloorplanFit.Desktop/**: app Avalonia MVVM que orquesta la experiencia del usuario.

- **tests/**: evidencia automatizada por capa.



## Convención de lectura



- **Archivo**: path exacto trackeado por git.

- **Misión**: por qué existe y qué responsabilidad sostiene.

- **Importancia**: qué se rompería, se mezclaría o se volvería opaco si esta pieza no existiera.

- **Use case**: en qué situación concreta del producto, del runtime o del mantenimiento aparece el valor de este archivo.

- **Capa/Área**: se deduce por la sección donde aparece el archivo.



## Raíz del repositorio y gobierno técnico



Estos archivos definen la identidad del repositorio y la verdad global que todas las capas deben respetar.



- `.gitignore` — Misión: Define qué archivos locales, temporales o generados no deben entrar al control de versiones. Importancia: sin este archivo el repo se llenaría de residuos locales y artefactos generados. Use case: cuando ejecutás la app, corrés tests o usás herramientas que generan archivos efímeros.

- `AGENTS.md` — Misión: Fija las reglas operativas del agente en este repo: tono, verificación, skills obligatorias, TDD estricto y protocolos de documentación/memoria. Importancia: sin este archivo no habría un contrato operativo explícito para trabajar bien en este repo. Use case: cuando un agente o colaborador necesita saber reglas, tono, skills y protocolos obligatorios.

- `Directory.Build.props` — Misión: Centraliza propiedades compartidas de compilación/target para todos los proyectos .NET de la solución. Importancia: sin este archivo cada proyecto repetiría configuración compartida y sería más fácil desalinearlos. Use case: cuando la solución restaura o compila proyectos con propiedades comunes.

- `FloorplanFit.sln` — Misión: Agrupa todos los proyectos de la app, infraestructura, dominio, contratos y tests en una sola solución. Importancia: sin este archivo no se podría abrir y operar cómodamente la solución completa como unidad. Use case: cuando querés navegar, testear o trabajar todos los proyectos desde Visual Studio, Rider o CLI.

- `LICENSE` — Misión: Declara la licencia legal del repositorio. Importancia: sin este archivo quedaría ambigua la licencia legal del repositorio. Use case: cuando alguien necesita saber bajo qué términos se puede usar o compartir el código.

- `MVP-UX.md` — Misión: Fuente canónica del flujo de producto y de las pantallas esperadas para Loop 1 y Loop 2. Importancia: sin este archivo el producto correría el riesgo de diseñarse por intuición y no por experiencia objetivo. Use case: cuando querés validar si una pantalla o flujo respeta la promesa UX del MVP.

- `TECH-STACK-ARCHITECTURE-DATAFLOW.md` — Misión: Fuente canónica del stack, las capas, el modelo de datos y el flujo extremo a extremo de la app. Importancia: sin este archivo faltaría la verdad arquitectónica de referencia para capas, stack y flujo de datos. Use case: cuando querés decidir dónde pertenece un cambio o cómo debería viajar la información extremo a extremo.

- `global.json` — Misión: Ancla la versión del SDK .NET que debe usar el repo. Importancia: sin este archivo el repo podría ejecutarse con SDKs distintos y volverse menos reproducible. Use case: cuando otra máquina o CI necesita usar la misma versión de .NET que el proyecto espera.



## Fixtures catalogados y referencias legacy



Estos archivos son fixtures o insumos del producto usados para importar, comparar o más adelante adaptar.



- `PLANS/catalog/santa-barbara.json` — Misión: Referencia catalogada legacy para santa barbara; sirve como oracle comparativo mientras la verdad canónica sigue siendo el DXF original. Importancia: sin este archivo faltaría un oracle legacy útil para contrastar la evolución del flujo sobre Santa Barbara. Use case: cuando querés comparar la salida actual contra la referencia catalogada histórica de Santa Barbara.

- `PLANS/catalog/seminole-2000.json` — Misión: Referencia catalogada legacy para seminole 2000; sirve como oracle comparativo mientras la verdad canónica sigue siendo el DXF original. Importancia: sin este archivo faltaría un oracle legacy útil para contrastar la evolución del flujo sobre Seminole 2000. Use case: cuando querés comparar la salida actual contra la referencia catalogada histórica de Seminole 2000.



## Fixtures canónicos de floor plans



Estos archivos son fixtures o insumos del producto usados para importar, comparar o más adelante adaptar.



- `PLANS/originalFloorPlans/SANTA-BARBARA.dxf` — Misión: Fixture DXF canónico de floor plan usado para importar, extraer walls y validar el Loop 1 sobre SANTA-BARBARA.dxf. Importancia: sin este archivo faltaría el fixture canónico más importante para validar el Loop 1 real del repo. Use case: cuando importás y extraés walls del caso Santa Barbara para validar el slice ejecutable actual.

- `PLANS/originalFloorPlans/SEMINOLE2000.dxf` — Misión: Fixture DXF canónico de floor plan usado para importar, extraer walls y validar el Loop 1 sobre SEMINOLE2000.dxf. Importancia: sin este archivo faltaría un fixture canónico alternativo para validar que el flujo no depende de un solo plano. Use case: cuando querés probar el pipeline de importación y extracción sobre un floor plan distinto de Santa Barbara.



## Fixtures canónicos de site plans



Estos archivos son fixtures o insumos del producto usados para importar, comparar o más adelante adaptar.



- `PLANS/originalsSitePlans/158 DAWSON STREET.dxf` — Misión: Fixture DXF canónico de site plan reservado para el futuro Loop 2 sobre 158 DAWSON STREET.dxf. Importancia: sin este archivo faltaría el fixture base previsto para empezar a verificar el futuro Loop 2. Use case: cuando Loop 2 necesite importar y analizar un lote real de referencia.



## Documentación narrativa histórica del proyecto



Estos archivos documentan el diseño, el plan o la historia de implementación del repositorio.



- `docs/explicacion del proyecto/2026-04-25 - explicacion de archivos tocados en slice 1.md` — Misión: Documento explicativo histórico sobre explicacion de archivos tocados en slice 1; captura decisiones, validaciones o walkthroughs hechos durante la evolución del repo. Importancia: sin este archivo se perdería contexto verificable sobre cómo evolucionó el repo. Use case: cuando alguien necesita reconstruir por qué se hizo, validó o corrigió algo en esa etapa.


- `docs/explicacion del proyecto/2026-04-29 - bugfix de versionado al reimportar el mismo dxf.md` — Misión: Documento explicativo histórico sobre bugfix de versionado al reimportar el mismo dxf; captura decisiones, validaciones o walkthroughs hechos durante la evolución del repo. Importancia: sin este archivo se perdería contexto verificable sobre cómo evolucionó el repo. Use case: cuando alguien necesita reconstruir por qué se hizo, validó o corrigió algo en esa etapa.

- `docs/explicacion del proyecto/2026-04-29 - checklist manual del slice 1 ejecutable.md` — Misión: Documento explicativo histórico sobre checklist manual del slice 1 ejecutable; captura decisiones, validaciones o walkthroughs hechos durante la evolución del repo. Importancia: sin este archivo se perdería contexto verificable sobre cómo evolucionó el repo. Use case: cuando alguien necesita reconstruir por qué se hizo, validó o corrigió algo en esa etapa.

- `docs/explicacion del proyecto/2026-04-29 - explicacion exhaustiva de archivos del slice 1 ejecutable.md` — Misión: Documento explicativo histórico sobre explicacion exhaustiva de archivos del slice 1 ejecutable; captura decisiones, validaciones o walkthroughs hechos durante la evolución del repo. Importancia: sin este archivo se perdería contexto verificable sobre cómo evolucionó el repo. Use case: cuando alguien necesita reconstruir por qué se hizo, validó o corrigió algo en esa etapa.

- `docs/explicacion del proyecto/2026-04-29 - hidratacion de library desde sqlite y fix de avln3001.md` — Misión: Documento explicativo histórico sobre hidratacion de library desde sqlite y fix de avln3001; captura decisiones, validaciones o walkthroughs hechos durante la evolución del repo. Importancia: sin este archivo se perdería contexto verificable sobre cómo evolucionó el repo. Use case: cuando alguien necesita reconstruir por qué se hizo, validó o corrigió algo en esa etapa.


- `docs/explicacion del proyecto/2026-04-29 - validacion real del slice 1 ejecutable.md` — Misión: Documento explicativo histórico sobre validacion real del slice 1 ejecutable; captura decisiones, validaciones o walkthroughs hechos durante la evolución del repo. Importancia: sin este archivo se perdería contexto verificable sobre cómo evolucionó el repo. Use case: cuando alguien necesita reconstruir por qué se hizo, validó o corrigió algo en esa etapa.



## Mapa exhaustivo y navegación total del repositorio



Estos archivos documentan el diseño, el plan o la historia de implementación del repositorio.



- `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md` — Misión: Mapa exhaustivo del repositorio creado para explicar la arquitectura y la misión de cada archivo existente de la app. Importancia: sin este archivo navegar el repo completo sería mucho más lento y propenso a malentendidos. Use case: cuando alguien necesita orientarse rápido en capas, áreas y archivos del sistema.



## Planes de implementación ejecutables



Estos archivos documentan el diseño, el plan o la historia de implementación del repositorio.



- `docs/superpowers/plans/2026-04-25-slice-1-import-foundation.md` — Misión: Plan ejecutable de implementación para 2026 04 25 slice 1 import foundation; descompone milestones y orden de trabajo. Importancia: sin este archivo el trabajo se haría con más improvisación y menos orden. Use case: cuando hay que ejecutar un cambio grande siguiendo milestones concretos.

- `docs/superpowers/plans/2026-04-29-slice-1-executable-implementation.md` — Misión: Plan ejecutable de implementación para 2026 04 29 slice 1 executable implementation; descompone milestones y orden de trabajo. Importancia: sin este archivo el trabajo se haría con más improvisación y menos orden. Use case: cuando hay que ejecutar un cambio grande siguiendo milestones concretos.

- `docs/superpowers/plans/2026-04-30-loop-1-curated-walls-and-spaces-implementation.md` — Misión: Plan ejecutable de implementación para 2026 04 30 loop 1 curated walls and spaces implementation; descompone milestones y orden de trabajo. Importancia: sin este archivo el trabajo se haría con más improvisación y menos orden. Use case: cuando hay que ejecutar un cambio grande siguiendo milestones concretos.

- `docs/superpowers/plans/2026-05-03-review-preview-layout-and-selection-fix.md` — Misión: Plan ejecutable de implementación del ajuste de preview, layout y selección en la review UI; descompone el fix para centrar el plano, repintar el highlight y ordenar el scroll. Importancia: sin este archivo el trabajo se haría con más improvisación y menos orden. Use case: cuando hay que ejecutar un ajuste UI multiarchivo sin perder el objetivo de UX real.



## Diseños y especificaciones técnicas



Estos archivos documentan el diseño, el plan o la historia de implementación del repositorio.



- `docs/superpowers/specs/2026-04-25-slice-1-import-foundation-design.md` — Misión: Especificación o diseño técnico de 2026 04 25 slice 1 import foundation design; define modelo, alcance y decisiones de arquitectura. Importancia: sin este archivo habría más riesgo de implementar sin respetar el diseño acordado. Use case: cuando querés implementar o auditar un cambio con la arquitectura correcta en mente.

- `docs/superpowers/specs/2026-04-29-slice-1-executable-design.md` — Misión: Especificación o diseño técnico de 2026 04 29 slice 1 executable design; define modelo, alcance y decisiones de arquitectura. Importancia: sin este archivo habría más riesgo de implementar sin respetar el diseño acordado. Use case: cuando querés implementar o auditar un cambio con la arquitectura correcta en mente.

- `docs/superpowers/specs/2026-04-30-loop-1-curated-walls-and-spaces-design.md` — Misión: Especificación o diseño técnico de 2026 04 30 loop 1 curated walls and spaces design; define modelo, alcance y decisiones de arquitectura. Importancia: sin este archivo habría más riesgo de implementar sin respetar el diseño acordado. Use case: cuando querés implementar o auditar un cambio con la arquitectura correcta en mente.

- `docs/superpowers/specs/2026-05-03-review-preview-layout-and-selection-design.md` — Misión: Especificación o diseño técnico del ajuste de preview, layout y selección en la review UI; define causa raíz, alcance y criterios de aceptación visual. Importancia: sin este archivo habría más riesgo de implementar sin respetar el diseño acordado. Use case: cuando querés implementar o auditar el fix de UX de review con la arquitectura correcta en mente.



## Configuración interna del vault



Estos archivos forman parte del conocimiento persistente del proyecto dentro del vault de Obsidian.




- `obsidian-vault/.obsidian/core-plugins.json` — Misión: Configuración de plugins core habilitados dentro del vault de Obsidian. Importancia: sin este archivo los plugins core activos del vault quedarían menos definidos. Use case: cuando Obsidian necesita saber qué capacidades base del vault deben estar encendidas.






## Registro de bugs



Estos archivos forman parte del conocimiento persistente del proyecto dentro del vault de Obsidian.



- `obsidian-vault/Bugs/2026-05-02 - Open Review crashes right after extraction because committed SQLite transaction is reused.md` — Misión: Nota de bug que documenta el crash del primer Open Review después de extracción, su causa raíz transaccional y el fix aplicado. Importancia: sin este archivo se perdería la memoria exacta del síntoma, la causa raíz y la forma correcta de evitar su regreso. Use case: cuando reaparece un fallo parecido en SQLite, drafts de curación o apertura de review y necesitás ir directo a la raíz.

- `obsidian-vault/Bugs/2026-04-29 - Reimport creates new template from managed filename suffix.md` — Misión: Nota de bug sobre Reimport creates new template from managed filename suffix; documenta síntoma, causa raíz o fix relacionado. Importancia: sin este archivo se perdería la memoria exacta del síntoma, la causa raíz y la forma correcta de evitar su regreso. Use case: cuando reaparece un síntoma o querés entender qué fix ya se hizo.

- `obsidian-vault/Bugs/README.md` — Misión: Nota de bug sobre README; documenta síntoma, causa raíz o fix relacionado. Importancia: sin este archivo se perdería la memoria exacta del síntoma, la causa raíz y la forma correcta de evitar su regreso. Use case: cuando reaparece un síntoma o querés entender qué fix ya se hizo.



## Decisiones arquitectónicas persistentes



Estos archivos forman parte del conocimiento persistente del proyecto dentro del vault de Obsidian.



- `obsidian-vault/Decisions/2026-04-25 - DXF as Primary Truth and Catalog as Legacy Reference.md` — Misión: Decisión persistente del proyecto sobre DXF as Primary Truth and Catalog as Legacy Reference; explica el porqué arquitectónico o de workflow. Importancia: sin este archivo la decisión quedaría implícita y sería mucho más fácil rediscutirla sin contexto. Use case: cuando hay que recordar por qué se eligió una dirección arquitectónica o de workflow.

- `obsidian-vault/Decisions/2026-04-25 - Floorplan Teaching Skill Always On.md` — Misión: Decisión persistente del proyecto sobre Floorplan Teaching Skill Always On; explica el porqué arquitectónico o de workflow. Importancia: sin este archivo la decisión quedaría implícita y sería mucho más fácil rediscutirla sin contexto. Use case: cuando hay que recordar por qué se eligió una dirección arquitectónica o de workflow.

- `obsidian-vault/Decisions/2026-04-25 - Initial Implementation Order.md` — Misión: Decisión persistente del proyecto sobre Initial Implementation Order; explica el porqué arquitectónico o de workflow. Importancia: sin este archivo la decisión quedaría implícita y sería mucho más fácil rediscutirla sin contexto. Use case: cuando hay que recordar por qué se eligió una dirección arquitectónica o de workflow.

- `obsidian-vault/Decisions/2026-04-25 - Project Identity and Knowledge Stack.md` — Misión: Decisión persistente del proyecto sobre Project Identity and Knowledge Stack; explica el porqué arquitectónico o de workflow. Importancia: sin este archivo la decisión quedaría implícita y sería mucho más fácil rediscutirla sin contexto. Use case: cuando hay que recordar por qué se eligió una dirección arquitectónica o de workflow.

- `obsidian-vault/Decisions/2026-04-25 - Santa Barbara as First Canonical Fixture.md` — Misión: Decisión persistente del proyecto sobre Santa Barbara as First Canonical Fixture; explica el porqué arquitectónico o de workflow. Importancia: sin este archivo la decisión quedaría implícita y sería mucho más fácil rediscutirla sin contexto. Use case: cuando hay que recordar por qué se eligió una dirección arquitectónica o de workflow.

- `obsidian-vault/Decisions/2026-04-25 - Slice 1 Import Foundation Architecture.md` — Misión: Decisión persistente del proyecto sobre Slice 1 Import Foundation Architecture; explica el porqué arquitectónico o de workflow. Importancia: sin este archivo la decisión quedaría implícita y sería mucho más fácil rediscutirla sin contexto. Use case: cuando hay que recordar por qué se eligió una dirección arquitectónica o de workflow.

- `obsidian-vault/Decisions/2026-04-29 - Copy Imported DXFs Into Managed Workspace.md` — Misión: Decisión persistente del proyecto sobre Copy Imported DXFs Into Managed Workspace; explica el porqué arquitectónico o de workflow. Importancia: sin este archivo la decisión quedaría implícita y sería mucho más fácil rediscutirla sin contexto. Use case: cuando hay que recordar por qué se eligió una dirección arquitectónica o de workflow.

- `obsidian-vault/Decisions/2026-04-29 - Keep Legacy Catalog JSONs as Oracle During Slice 1.md` — Misión: Decisión persistente del proyecto sobre Keep Legacy Catalog JSONs as Oracle During Slice 1; explica el porqué arquitectónico o de workflow. Importancia: sin este archivo la decisión quedaría implícita y sería mucho más fácil rediscutirla sin contexto. Use case: cuando hay que recordar por qué se eligió una dirección arquitectónica o de workflow.

- `obsidian-vault/Decisions/2026-04-29 - Parse And Hash Managed DXF Copy.md` — Misión: Decisión persistente del proyecto sobre Parse And Hash Managed DXF Copy; explica el porqué arquitectónico o de workflow. Importancia: sin este archivo la decisión quedaría implícita y sería mucho más fácil rediscutirla sin contexto. Use case: cuando hay que recordar por qué se eligió una dirección arquitectónica o de workflow.

- `obsidian-vault/Decisions/2026-04-29 - Thin Desktop Included In Executable Slice 1.md` — Misión: Decisión persistente del proyecto sobre Thin Desktop Included In Executable Slice 1; explica el porqué arquitectónico o de workflow. Importancia: sin este archivo la decisión quedaría implícita y sería mucho más fácil rediscutirla sin contexto. Use case: cuando hay que recordar por qué se eligió una dirección arquitectónica o de workflow.

- `obsidian-vault/Decisions/2026-04-30 - Loop 1 Adds Minimal Curated Spaces and Defers New Walls To Loop 2.md` — Misión: Decisión persistente del proyecto sobre Loop 1 Adds Minimal Curated Spaces and Defers New Walls To Loop 2; explica el porqué arquitectónico o de workflow. Importancia: sin este archivo la decisión quedaría implícita y sería mucho más fácil rediscutirla sin contexto. Use case: cuando hay que recordar por qué se eligió una dirección arquitectónica o de workflow.

- `obsidian-vault/Decisions/2026-04-30 - Loop 1 Completion Order.md` — Misión: Decisión persistente del proyecto sobre Loop 1 Completion Order; explica el porqué arquitectónico o de workflow. Importancia: sin este archivo la decisión quedaría implícita y sería mucho más fácil rediscutirla sin contexto. Use case: cuando hay que recordar por qué se eligió una dirección arquitectónica o de workflow.

- `obsidian-vault/Decisions/2026-04-30 - Loop 1 Uses Semantic Core Plus Minimal Review Canvas.md` — Misión: Decisión persistente del proyecto sobre Loop 1 Uses Semantic Core Plus Minimal Review Canvas; explica el porqué arquitectónico o de workflow. Importancia: sin este archivo la decisión quedaría implícita y sería mucho más fácil rediscutirla sin contexto. Use case: cuando hay que recordar por qué se eligió una dirección arquitectónica o de workflow.



## Área de experimentos del vault



Estos archivos forman parte del conocimiento persistente del proyecto dentro del vault de Obsidian.



- `obsidian-vault/Experiments/README.md` — Misión: README contenedor del área de experimentos del vault. Importancia: sin este archivo el área de experimentos quedaría huérfana y menos entendible. Use case: cuando alguien necesita saber cómo usar o poblar el espacio de experimentación.



## Bitácora de implementación



Estos archivos forman parte del conocimiento persistente del proyecto dentro del vault de Obsidian.



- `obsidian-vault/Implementation/2026-05-02 - Fixed first-open review transaction crash.md` — Misión: Bitácora de implementación del fix que evita el crash al abrir Review por primera vez después de extraer walls. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando necesitás auditar el arreglo del bug transaccional de Open Review y entender por qué el fix fue en los readers y no en SqliteSession.

- `obsidian-vault/Implementation/2026-05-03 - Review preview layout and highlight fix.md` — Misión: Bitácora de implementación del arreglo de layout, highlight y scroll de la review UI. Importancia: sin este archivo se perdería trazabilidad concreta de cómo se resolvió el problema visual del curated draft y con qué evidencia quedó respaldado. Use case: cuando necesitás auditar por qué la selección del preview ahora repinta en tiempo real y cómo se reorganizó la ventana.

- `obsidian-vault/Implementation/2026-05-02 - Revalidacion del estado actual y drift del checklist manual.md` — Misión: Bitácora de revalidación del estado real del repo y del drift detectado entre documentación histórica y runtime actual. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés distinguir entre notas históricas y verdad actual verificada del repositorio.


- `obsidian-vault/Implementation/2026-04-29 - .NET 10 SDK Installed.md` — Misión: Bitácora de implementación o validación sobre .NET 10 SDK Installed; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-29 - Desktop Dev Watch Launcher.md` — Misión: Bitácora de implementación o validación sobre Desktop Dev Watch Launcher; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-29 - Desktop Shortcut Created.md` — Misión: Bitácora de implementación o validación sobre Desktop Shortcut Created; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-29 - Documentation Sync Final Pass.md` — Misión: Bitácora de implementación o validación sobre Documentation Sync Final Pass; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.



- `obsidian-vault/Implementation/2026-04-29 - Library Startup Hydration and MainWindow Loader Fix.md` — Misión: Bitácora de implementación o validación sobre Library Startup Hydration and MainWindow Loader Fix; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-29 - Manual Validation Checklist for Slice 1 Executable.md` — Misión: Bitácora de implementación o validación sobre Manual Validation Checklist for Slice 1 Executable; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-29 - Reimport Versioning Bug Fixed.md` — Misión: Bitácora de implementación o validación sobre Reimport Versioning Bug Fixed; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-29 - Repository Audit Status.md` — Misión: Bitácora de implementación o validación sobre Repository Audit Status; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-29 - Slice 1 Executable Design.md` — Misión: Bitácora de implementación o validación sobre Slice 1 Executable Design; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-29 - Slice 1 Executable Implementation Plan.md` — Misión: Bitácora de implementación o validación sobre Slice 1 Executable Implementation Plan; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-29 - Slice 1 Executable Kickoff.md` — Misión: Bitácora de implementación o validación sobre Slice 1 Executable Kickoff; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-29 - Slice 1 Infrastructure Import Pipeline.md` — Misión: Bitácora de implementación o validación sobre Slice 1 Infrastructure Import Pipeline; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-29 - Validation Results and App Control Blockers.md` — Misión: Bitácora de implementación o validación sobre Validation Results and App Control Blockers; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-30 - Loop 1 Curated Walls and Spaces Design.md` — Misión: Bitácora de implementación o validación sobre Loop 1 Curated Walls and Spaces Design; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-30 - Loop 1 Implementation Plan.md` — Misión: Bitácora de implementación o validación sobre Loop 1 Implementation Plan; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-30 - Requirement Tension Between Walls-Only and Room Constraints.md` — Misión: Bitácora de implementación o validación sobre Requirement Tension Between Walls Only and Room Constraints; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-30 - Status Audit and Documentation Drift.md` — Misión: Bitácora de implementación o validación sobre Status Audit and Documentation Drift; registra progreso, checks y hallazgos concretos del repo. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/Vault Bootstrap.md` — Misión: Nota base que describe cómo quedó inicializado el vault y cómo se organiza el conocimiento persistente. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.

- `obsidian-vault/Implementation/2026-04-30 - Mapa completo de arquitectura y archivos del repo.md` — Misión: Nota de implementación que registra la creación del mapa exhaustivo de arquitectura y archivos del repositorio. Importancia: sin este archivo se perdería trazabilidad concreta de qué se hizo, cuándo y con qué evidencia quedó respaldado. Use case: cuando querés auditar progreso real, verificaciones o hallazgos de una fecha puntual.



## Inbox del vault



Estos archivos forman parte del conocimiento persistente del proyecto dentro del vault de Obsidian.



- `obsidian-vault/Inbox/README.md` — Misión: README contenedor del inbox del vault para capturas rápidas. Importancia: sin este archivo el inbox del vault quedaría menos claro para capturas rápidas. Use case: cuando alguien necesita volcar una nota rápida sin romper la organización del vault.



## Raíz del conocimiento en Obsidian



Estos archivos forman parte del conocimiento persistente del proyecto dentro del vault de Obsidian.



- `obsidian-vault/Current State.md` — Misión: Resumen vivo del estado real del repo, verificación, próximos pasos y verdad canónica del proyecto. Importancia: sin este archivo sería mucho más difícil saber cuál es la verdad actual verificada del repo. Use case: cuando necesitás entender rápido dónde estamos de verdad, qué funciona y qué sigue pendiente.

- `obsidian-vault/Home.md` — Misión: Home del vault: punto de entrada humano para navegar decisiones, implementación, bugs y estado actual. Importancia: sin este archivo el vault perdería su punto de entrada humano más claro. Use case: cuando abrís Obsidian y querés saltar a estado, decisiones, bugs o implementación sin perderte.



## Automatización local operativa



- `scripts/dev-desktop.bat` — Misión: Atajo local para desarrollo continuo de la app desktop con dotnet watch; hoy no se usa como verificación de coding por la regla never build after changes. Importancia: sin este archivo levantar el desktop en modo watch sería más manual y propenso a errores operativos. Use case: cuando querés iterar la app localmente con dotnet watch sin recordar el comando completo.



## Activos del skill local



Estos archivos enseñan al agente cómo explicar y trabajar correctamente dentro de este repositorio.



- `skills/floorplan-fit-teaching-mode/assets/teaching-response-template.md` — Misión: Template base de la respuesta pedagógica que se usa al explicar cambios en este repo. Importancia: sin este archivo teaching mode perdería una estructura concreta para respuestas profundas y consistentes. Use case: cuando hace falta responder con big picture, archivos tocados, walkthrough y tradeoffs.



## Referencias del skill local



Estos archivos enseñan al agente cómo explicar y trabajar correctamente dentro de este repositorio.



- `skills/floorplan-fit-teaching-mode/references/pressure-scenarios.md` — Misión: Escenarios de presión para verificar que la enseñanza siga siendo rigurosa incluso bajo pedidos ambiguos o apurados. Importancia: sin este archivo teaching mode tendría menos material para probarse contra casos exigentes. Use case: cuando querés tensar o verificar que una explicación siga siendo útil bajo presión o ambigüedad.



## Skill local del repositorio



Estos archivos enseñan al agente cómo explicar y trabajar correctamente dentro de este repositorio.



- `skills/floorplan-fit-teaching-mode/SKILL.md` — Misión: Skill local del repo que obliga a explicar cambios anclados a loops de producto y capas de arquitectura. Importancia: sin este archivo se perdería la regla explícita de enseñar cambios por loop, capa y tradeoff. Use case: cuando el agente tiene que explicar trabajo en Floorplan Fit sin caer en respuestas superficiales.



## Puertos y contratos internos de Application



Acá vive la **orquestación de casos de uso**. No debería haber detalles concretos de SQLite, Avalonia o IxMilia.



- `src/FloorplanFit.Application/Abstractions/DetectedFloorPlanDocument.cs` — Misión: DTO técnico que representa el resultado de leer un DXF de floor plan: versión DXF, unidad, factor a milímetros y fingerprint geométrico. Importancia: sin este archivo Application perdería una forma explícita de representar datos intermedios sin depender de infraestructura. Use case: cuando el runtime necesita leer un DXF real o extraerle información útil.

- `src/FloorplanFit.Application/Abstractions/DetectedWallCandidate.cs` — Misión: DTO técnico de salida del extractor de walls antes de convertirlo en entidad de dominio persistible. Importancia: sin este archivo Application perdería una forma explícita de representar datos intermedios sin depender de infraestructura. Use case: cuando una capa necesita transportar datos claros hacia la UI o entre boundaries sin exponer entidades internas.

- `src/FloorplanFit.Application/Abstractions/FloorPlanExtractionSource.cs` — Misión: Describe desde qué versión y qué archivo gestionado hay que correr la extracción de walls. Importancia: sin este archivo Application perdería una forma explícita de representar datos intermedios sin depender de infraestructura. Use case: cuando esa parte del sistema entra en juego.

- `src/FloorplanFit.Application/Abstractions/GeometryPoint.cs` — Misión: Valor simple 2D usado por extractores para describir geometría sin acoplarse a Infrastructure. Importancia: sin este archivo Application perdería una forma explícita de representar datos intermedios sin depender de infraestructura. Use case: cuando esa parte del sistema entra en juego.

- `src/FloorplanFit.Application/Abstractions/IClock.cs` — Misión: Puerto para abstraer el tiempo actual y volver testeables las operaciones temporales. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando esa parte del sistema entra en juego.

- `src/FloorplanFit.Application/Abstractions/ICuratedWallRepository.cs` — Misión: Puerto de escritura/lectura para curated walls. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.

- `src/FloorplanFit.Application/Abstractions/IDxfGateway.cs` — Misión: Puerto para leer DXF de floor plans sin acoplar Application a IxMilia. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando el runtime necesita leer un DXF real o extraerle información útil.

- `src/FloorplanFit.Application/Abstractions/IExtractedWallCandidateRepository.cs` — Misión: Puerto para persistir y consultar wall candidates extraídos. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando el pipeline necesita convertir geometría DXF en wall candidates revisables.

- `src/FloorplanFit.Application/Abstractions/IFileHashService.cs` — Misión: Puerto para calcular hashes de archivos importados. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando el pipeline necesita mover, ubicar o identificar técnicamente archivos del workspace.

- `src/FloorplanFit.Application/Abstractions/IFloorPlanCurationRepository.cs` — Misión: Puerto para manejar draft/published de curaciones de floor plans. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando abrir Review exige crear o retomar un draft antes de seguir.

- `src/FloorplanFit.Application/Abstractions/IFloorPlanExtractionSourceReader.cs` — Misión: Puerto de lectura optimizado para resolver la versión actual y el path DXF gestionado usado por la extracción. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.

- `src/FloorplanFit.Application/Abstractions/IFloorPlanLibraryReader.cs` — Misión: Puerto de lectura optimizado para hidratar la Library desde persistencia. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando la pantalla principal necesita cargar, refrescar o derivar el estado visible de la Library.

- `src/FloorplanFit.Application/Abstractions/IFloorPlanReviewSessionReader.cs` — Misión: Puerto de lectura optimizado para construir una sesión de review completa desde SQLite. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando la Review necesita abrir, leer, mostrar o validar su estado actual.

- `src/FloorplanFit.Application/Abstractions/IFloorPlanTemplateRepository.cs` — Misión: Puerto para crear, actualizar y consultar floor plan templates. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.

- `src/FloorplanFit.Application/Abstractions/IFloorPlanVersionRepository.cs` — Misión: Puerto para persistir versiones de floor plans y calcular el próximo número de versión. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.

- `src/FloorplanFit.Application/Abstractions/IImportedDocumentRepository.cs` — Misión: Puerto para persistir los documentos DXF importados. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.

- `src/FloorplanFit.Application/Abstractions/IManagedFileStorage.cs` — Misión: Puerto para copiar archivos importados al workspace gestionado de la app. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando el pipeline necesita mover, ubicar o identificar técnicamente archivos del workspace.

- `src/FloorplanFit.Application/Abstractions/IMeasurementContextRepository.cs` — Misión: Puerto para persistir el contexto de unidades/tolerancias de cada import. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando el sistema detecta unidades del DXF y necesita normalizar medidas y tolerancias.

- `src/FloorplanFit.Application/Abstractions/IUnitOfWork.cs` — Misión: Puerto transaccional para confirmar cambios coordinados entre varios repositorios. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando el sistema detecta unidades del DXF y necesita normalizar medidas y tolerancias.

- `src/FloorplanFit.Application/Abstractions/IWallExtractionRunRepository.cs` — Misión: Puerto para persistir cada corrida automática de extracción. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.

- `src/FloorplanFit.Application/Abstractions/IWallExtractor.cs` — Misión: Puerto del extractor real de walls a partir de un DXF gestionado. Importancia: sin este archivo Application quedaría más acoplada a implementaciones concretas y perdería claridad de boundary. Use case: cuando el runtime necesita leer un DXF real o extraerle información útil.



## Casos de uso del curado Loop 1



Acá vive la **orquestación de casos de uso**. No debería haber detalles concretos de SQLite, Avalonia o IxMilia.



- `src/FloorplanFit.Application/FloorPlans/Curation/AcceptWallCandidateHandler.cs` — Misión: Caso de uso que acepta un candidate pendiente y lo convierte en CuratedWall inicial dentro de un draft. Importancia: sin este archivo este caso de uso quedaría mezclado en otra capa. Use case: cuando abrir Review exige crear o retomar un draft antes de seguir.

- `src/FloorplanFit.Application/FloorPlans/Curation/PublishFloorPlanCurationHandler.cs` — Misión: Caso de uso que valida y publica una curación draft, activándola en el template. Importancia: sin este archivo este caso de uso quedaría mezclado en otra capa. Use case: cuando el usuario publica una curación y deja una versión activa reutilizable.

- `src/FloorplanFit.Application/FloorPlans/Curation/RejectWallCandidateHandler.cs` — Misión: Caso de uso que rechaza un candidate y elimina la CuratedWall derivada en el draft actual. Importancia: sin este archivo este caso de uso quedaría mezclado en otra capa. Use case: cuando abrir Review exige crear o retomar un draft antes de seguir.

- `src/FloorplanFit.Application/FloorPlans/Curation/StartOrResumeCurationHandler.cs` — Misión: Caso de uso que crea o retoma el draft de curación de una versión de floor plan. Importancia: sin este archivo este caso de uso quedaría mezclado en otra capa. Use case: cuando abrir Review exige crear o retomar un draft antes de seguir.

- `src/FloorplanFit.Application/FloorPlans/Curation/UpdateCuratedWallMetadataHandler.cs` — Misión: Caso de uso que actualiza la metadata semántica de una wall ya curada. Importancia: sin este archivo este caso de uso quedaría mezclado en otra capa. Use case: cuando el usuario cambia role, mobility, protection u otra metadata desde el inspector de review.



## Casos de uso de extracción automática



Acá vive la **orquestación de casos de uso**. No debería haber detalles concretos de SQLite, Avalonia o IxMilia.



- `src/FloorplanFit.Application/FloorPlans/Extraction/ExtractWallCandidatesHandler.cs` — Misión: Caso de uso que corre el extractor, crea la corrida de extracción y persiste wall candidates + geometría. Importancia: sin este archivo este caso de uso quedaría mezclado en otra capa. Use case: cuando el pipeline necesita convertir geometría DXF en wall candidates revisables.



## Casos de uso de importación



Acá vive la **orquestación de casos de uso**. No debería haber detalles concretos de SQLite, Avalonia o IxMilia.



- `src/FloorplanFit.Application/FloorPlans/Import/FloorPlanCodeNormalizer.cs` — Misión: Normaliza nombres de archivos en códigos estables de templates para evitar duplicados semánticos. Importancia: sin este archivo este caso de uso quedaría mezclado en otra capa. Use case: cuando el usuario elige un DXF y la app lo incorpora a la Library con su metadata y versión.

- `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanHandler.cs` — Misión: Caso de uso de importación: copia DXF, lo lee, crea template/version/document/measurement context y persiste todo. Importancia: sin este archivo este caso de uso quedaría mezclado en otra capa. Use case: cuando el usuario elige un DXF y la app lo incorpora a la Library con su metadata y versión.

- `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanResultFactory.cs` — Misión: Fábrica que traduce el resultado de importación a DTOs de Contracts para la UI. Importancia: sin este archivo este caso de uso quedaría mezclado en otra capa. Use case: cuando el usuario elige un DXF y la app lo incorpora a la Library con su metadata y versión.



## Casos de uso de lectura de Library



Acá vive la **orquestación de casos de uso**. No debería haber detalles concretos de SQLite, Avalonia o IxMilia.



- `src/FloorplanFit.Application/FloorPlans/Library/GetFloorPlanLibraryHandler.cs` — Misión: Caso de uso de lectura que devuelve la Library de floor plans desde el read-model. Importancia: sin este archivo este caso de uso quedaría mezclado en otra capa. Use case: cuando la pantalla principal necesita cargar, refrescar o derivar el estado visible de la Library.



## Casos de uso de review



Acá vive la **orquestación de casos de uso**. No debería haber detalles concretos de SQLite, Avalonia o IxMilia.



- `src/FloorplanFit.Application/FloorPlans/Review/GetFloorPlanReviewSessionHandler.cs` — Misión: Caso de uso de lectura que devuelve la review session ya hidratada para un template. Importancia: sin este archivo este caso de uso quedaría mezclado en otra capa. Use case: cuando la app necesita mostrar la sesión completa de review con summary, candidates y curated walls.

- `src/FloorplanFit.Application/FloorPlans/Review/OpenFloorPlanReviewSessionHandler.cs` — Misión: Caso de uso que asegura draft activo y luego abre la sesión de review completa. Importancia: sin este archivo este caso de uso quedaría mezclado en otra capa. Use case: cuando el usuario abre Review por primera vez o reabre una sesión existente y la app tiene que hidratar draft, candidates y geometry.



## Proyecto Application



Acá vive la **orquestación de casos de uso**. No debería haber detalles concretos de SQLite, Avalonia o IxMilia.



- `src/FloorplanFit.Application/FloorplanFit.Application.csproj` — Misión: Proyecto de Application: define el ensamblado que contiene puertos y casos de uso. Importancia: sin este archivo no se podría compilar, restaurar ni referenciar correctamente este proyecto dentro de la solución. Use case: cuando la solución necesita restaurar dependencias, compilar este proyecto o usarlo como referencia desde otra capa.



## DTOs de Contracts



- `src/FloorplanFit.Contracts/FloorPlans/CuratedWallDto.cs` — Misión: DTO plano que la UI usa para mostrar y editar walls curadas sin tocar entidades de dominio. Importancia: sin este archivo el intercambio entre capas sería más frágil o demasiado acoplado a entidades internas. Use case: cuando una capa necesita transportar datos claros hacia la UI o entre boundaries sin exponer entidades internas.

- `src/FloorplanFit.Contracts/FloorPlans/FloorPlanLibraryItemDto.cs` — Misión: DTO de cada fila de la Library con estado derivado, versión activa y unidad fuente. Importancia: sin este archivo el intercambio entre capas sería más frágil o demasiado acoplado a entidades internas. Use case: cuando la pantalla principal necesita cargar, refrescar o derivar el estado visible de la Library.

- `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs` — Misión: DTO agregado que empaqueta resumen del template, geometría, candidates y curated walls para la review UI. Importancia: sin este archivo el intercambio entre capas sería más frágil o demasiado acoplado a entidades internas. Use case: cuando la Review necesita abrir, leer, mostrar o validar su estado actual.

- `src/FloorplanFit.Contracts/FloorPlans/GeometryPathDto.cs` — Misión: DTO de una trayectoria geométrica compuesta por segmentos ordenados. Importancia: sin este archivo el intercambio entre capas sería más frágil o demasiado acoplado a entidades internas. Use case: cuando una capa necesita transportar datos claros hacia la UI o entre boundaries sin exponer entidades internas.

- `src/FloorplanFit.Contracts/FloorPlans/GeometrySegmentDto.cs` — Misión: DTO de un segmento lineal individual dentro de un geometry path. Importancia: sin este archivo el intercambio entre capas sería más frágil o demasiado acoplado a entidades internas. Use case: cuando una capa necesita transportar datos claros hacia la UI o entre boundaries sin exponer entidades internas.

- `src/FloorplanFit.Contracts/FloorPlans/ImportFloorPlanRequest.cs` — Misión: Contrato de entrada para solicitar importación de un DXF. Importancia: sin este archivo el intercambio entre capas sería más frágil o demasiado acoplado a entidades internas. Use case: cuando el usuario elige un DXF y la app lo incorpora a la Library con su metadata y versión.

- `src/FloorplanFit.Contracts/FloorPlans/ImportFloorPlanResponse.cs` — Misión: Contrato de salida de la importación para refrescar la UI. Importancia: sin este archivo el intercambio entre capas sería más frágil o demasiado acoplado a entidades internas. Use case: cuando el usuario elige un DXF y la app lo incorpora a la Library con su metadata y versión.

- `src/FloorplanFit.Contracts/FloorPlans/OpenFloorPlanReviewSessionResponse.cs` — Misión: Contrato de salida que devuelve draft activo + review session al abrir review. Importancia: sin este archivo el intercambio entre capas sería más frágil o demasiado acoplado a entidades internas. Use case: cuando el usuario abre Review por primera vez o reabre una sesión existente y la app tiene que hidratar draft, candidates y geometry.

- `src/FloorplanFit.Contracts/FloorPlans/WallCandidateDto.cs` — Misión: DTO de cada wall candidate extraída con estado, confianza y referencia geométrica. Importancia: sin este archivo el intercambio entre capas sería más frágil o demasiado acoplado a entidades internas. Use case: cuando el pipeline necesita convertir geometría DXF en wall candidates revisables.



## Proyecto Contracts



- `src/FloorplanFit.Contracts/FloorplanFit.Contracts.csproj` — Misión: Proyecto de Contracts: define los DTOs que desacoplan UI/read-models de las entidades de dominio. Importancia: sin este archivo no se podría compilar, restaurar ni referenciar correctamente este proyecto dentro de la solución. Use case: cuando la solución necesita restaurar dependencias, compilar este proyecto o usarlo como referencia desde otra capa.



## Composición DI del desktop



Acá vive la **experiencia de usuario desktop** sobre Avalonia + MVVM.



- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs` — Misión: Punto de composición DI del slice desktop: registra infrastructure, handlers, readers y viewmodels necesarios. Importancia: sin este archivo la experiencia desktop perdería una pieza concreta de arranque, wiring, visualización o interacción. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.



## Controles visuales custom



Acá vive la **experiencia de usuario desktop** sobre Avalonia + MVVM.



- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs` — Misión: Canvas mínimo custom que dibuja geometry paths y resalta la selección actual en review. Importancia: sin este archivo la experiencia desktop perdería una pieza concreta de arranque, wiring, visualización o interacción. Use case: cuando la Review necesita abrir, leer, mostrar o validar su estado actual.

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewGeometry.cs` — Misión: Helper de geometría del preview que centraliza viewport, proyección y estilo visual del highlight. Importancia: sin este archivo la lógica visual del canvas quedaría más acoplada, más difícil de testear y más frágil ante cambios de render. Use case: cuando la Review necesita recentrar el plano o resaltar en tiempo real la línea o path seleccionado.



## ViewModels MVVM



Acá vive la **experiencia de usuario desktop** sobre Avalonia + MVVM.



- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs` — Misión: ViewModel del flujo de review: abre sesión, maneja selección, accept/reject, edición de metadata y publish. Importancia: sin este archivo la experiencia desktop perdería una pieza concreta de arranque, wiring, visualización o interacción. Use case: cuando la Review necesita abrir, leer, mostrar o validar su estado actual.

- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs` — Misión: ViewModel de la Library: carga items, importa DXF, dispara extracción y abre la review session. Importancia: sin este archivo la experiencia desktop perdería una pieza concreta de arranque, wiring, visualización o interacción. Use case: cuando la app necesita mostrar la sesión completa de review con summary, candidates y curated walls.



## Proyecto Avalonia desktop



Acá vive la **experiencia de usuario desktop** sobre Avalonia + MVVM.



- `src/FloorplanFit.Desktop/App.axaml` — Misión: Define recursos y arranque visual global de la app Avalonia. Importancia: sin este archivo la experiencia desktop perdería una pieza concreta de arranque, wiring, visualización o interacción. Use case: cuando el usuario interactúa con la UI desktop y espera ver o disparar acciones de Loop 1.

- `src/FloorplanFit.Desktop/App.axaml.cs` — Misión: Bootstrap code-behind del Application de Avalonia. Importancia: sin este archivo la experiencia desktop perdería una pieza concreta de arranque, wiring, visualización o interacción. Use case: cuando el usuario interactúa con la UI desktop y espera ver o disparar acciones de Loop 1.

- `src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj` — Misión: Proyecto de la app Avalonia desktop: composición, vistas, viewmodels y arranque. Importancia: sin este archivo no se podría compilar, restaurar ni referenciar correctamente este proyecto dentro de la solución. Use case: cuando la solución necesita restaurar dependencias, compilar este proyecto o usarlo como referencia desde otra capa.

- `src/FloorplanFit.Desktop/Properties/AssemblyInfo.cs` — Misión: Metadatos de ensamblado del proyecto Desktop, incluyendo la exposición controlada de internos hacia los tests. Importancia: sin este archivo ciertos tests de helpers internos obligarían a volver pública lógica que debería seguir encapsulada o directamente no podrían compilar. Use case: cuando querés testear piezas internas del preview desktop sin romper el diseño de la API productiva.

- `src/FloorplanFit.Desktop/MainWindow.axaml` — Misión: Pantalla principal Library: listado de floor plans y acciones de import/extract/review. Importancia: sin este archivo la experiencia desktop perdería una pieza concreta de arranque, wiring, visualización o interacción. Use case: cuando la pantalla principal necesita cargar, refrescar o derivar el estado visible de la Library.

- `src/FloorplanFit.Desktop/MainWindow.axaml.cs` — Misión: Code-behind mínimo que conecta clicks de la Library con el LibraryViewModel y abre la review window. Importancia: sin este archivo la experiencia desktop perdería una pieza concreta de arranque, wiring, visualización o interacción. Use case: cuando la pantalla principal necesita cargar, refrescar o derivar el estado visible de la Library.

- `src/FloorplanFit.Desktop/Program.cs` — Misión: Entry point real de la app desktop; construye y lanza Avalonia. Importancia: sin este archivo la experiencia desktop perdería una pieza concreta de arranque, wiring, visualización o interacción. Use case: cuando el usuario abre la aplicación y navega el flujo principal de Loop 1.

- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml` — Misión: Pantalla de review mínima con lista de candidates, preview geométrico e inspector de curated walls. Importancia: sin este archivo la experiencia desktop perdería una pieza concreta de arranque, wiring, visualización o interacción. Use case: cuando la Review necesita abrir, leer, mostrar o validar su estado actual.

- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs` — Misión: Code-behind mínimo que delega acciones de review al FloorPlanReviewViewModel. Importancia: sin este archivo la experiencia desktop perdería una pieza concreta de arranque, wiring, visualización o interacción. Use case: cuando la Review necesita abrir, leer, mostrar o validar su estado actual.



## Entidades de documentos



Acá vive la **verdad del negocio**. Son entidades, enums y reglas que deberían sobrevivir aunque cambie la infraestructura.



- `src/FloorplanFit.Domain/Documents/ImportedDocument.cs` — Misión: Entidad de dominio del documento DXF importado y almacenado por la app. Importancia: sin este archivo el dominio no podría representar esta parte del negocio de forma explícita y reusable. Use case: cuando el runtime necesita leer un DXF real o extraerle información útil.

- `src/FloorplanFit.Domain/Documents/ImportedDocumentType.cs` — Misión: Enum del tipo de documento importado. Importancia: sin este archivo faltaría vocabulario cerrado para expresar reglas de negocio sin ambigüedad. Use case: cuando un DXF entra al sistema y hay que tratarlo como documento de negocio versionable.



## Entidades y enums de floor plans



Acá vive la **verdad del negocio**. Son entidades, enums y reglas que deberían sobrevivir aunque cambie la infraestructura.



- `src/FloorplanFit.Domain/FloorPlans/ConstraintIntentNote.cs` — Misión: Entidad prevista para notas humanas de constraints todavía no estructuradas. Importancia: sin este archivo el dominio no podría representar esta parte del negocio de forma explícita y reusable. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/ConstraintKind.cs` — Misión: Enum de tipos de constraints estructuradas posibles. Importancia: sin este archivo faltaría vocabulario cerrado para expresar reglas de negocio sin ambigüedad. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/ConstraintStrength.cs` — Misión: Enum de fuerza o prioridad de una constraint. Importancia: sin este archivo faltaría vocabulario cerrado para expresar reglas de negocio sin ambigüedad. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/CuratedSpace.cs` — Misión: Entidad prevista para representar ambientes/espacios curados; hoy existe en dominio pero todavía no recorre el flujo completo. Importancia: sin este archivo el dominio no podría representar esta parte del negocio de forma explícita y reusable. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/CuratedWall.cs` — Misión: Entidad canónica de una wall ya validada por humano y enriquecida con metadata reusable. Importancia: sin este archivo el dominio no podría representar esta parte del negocio de forma explícita y reusable. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/CuratedWallGroup.cs` — Misión: Entidad prevista para agrupar walls curadas con significado conjunto. Importancia: sin este archivo el dominio no podría representar esta parte del negocio de forma explícita y reusable. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/CuratedWallJoin.cs` — Misión: Entidad prevista para registrar joins o uniones entre walls curadas. Importancia: sin este archivo el dominio no podría representar esta parte del negocio de forma explícita y reusable. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/ExtractedWallCandidate.cs` — Misión: Entidad de dominio de una wall candidata detectada automáticamente y pendiente/aceptada/rechazada. Importancia: sin este archivo el dominio no podría representar esta parte del negocio de forma explícita y reusable. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/ExtractedWallCandidateStatus.cs` — Misión: Enum de estados de un wall candidate. Importancia: sin este archivo faltaría vocabulario cerrado para expresar reglas de negocio sin ambigüedad. Use case: cuando el pipeline necesita convertir geometría DXF en wall candidates revisables.

- `src/FloorplanFit.Domain/FloorPlans/FloorPlanCuration.cs` — Misión: Entidad que modela una versión draft/published del curado humano de un floor plan. Importancia: sin este archivo el dominio no podría representar esta parte del negocio de forma explícita y reusable. Use case: cuando abrir Review exige crear o retomar un draft antes de seguir.

- `src/FloorplanFit.Domain/FloorPlans/FloorPlanCurationStatus.cs` — Misión: Enum del ciclo de vida de la curación. Importancia: sin este archivo faltaría vocabulario cerrado para expresar reglas de negocio sin ambigüedad. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/FloorPlanTemplate.cs` — Misión: Raíz de agregado de la tipología reusable: separa current version importada de active published curation. Importancia: sin este archivo el dominio no podría representar esta parte del negocio de forma explícita y reusable. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/FloorPlanVersion.cs` — Misión: Entidad que representa una versión importada concreta del template. Importancia: sin este archivo el dominio no podría representar esta parte del negocio de forma explícita y reusable. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/SpaceType.cs` — Misión: Enum de tipos de ambientes previstos para curated spaces. Importancia: sin este archivo faltaría vocabulario cerrado para expresar reglas de negocio sin ambigüedad. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/StructuredConstraint.cs` — Misión: Entidad prevista para constraints tipadas aplicables a walls/spaces. Importancia: sin este archivo el dominio no podría representar esta parte del negocio de forma explícita y reusable. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/WallExtractionRun.cs` — Misión: Entidad que registra una corrida automática del extractor sobre una versión. Importancia: sin este archivo el dominio no podría representar esta parte del negocio de forma explícita y reusable. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/WallMobilityLevel.cs` — Misión: Enum que indica cuánto se puede mover/estirar una wall. Importancia: sin este archivo faltaría vocabulario cerrado para expresar reglas de negocio sin ambigüedad. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/WallProtectionLevel.cs` — Misión: Enum que indica nivel de protección o intocabilidad de una wall. Importancia: sin este archivo faltaría vocabulario cerrado para expresar reglas de negocio sin ambigüedad. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.

- `src/FloorplanFit.Domain/FloorPlans/WallRole.cs` — Misión: Enum semántico del rol de una wall (partition, exterior, etc.). Importancia: sin este archivo faltaría vocabulario cerrado para expresar reglas de negocio sin ambigüedad. Use case: cuando Loop 1 necesita representar candidates, curations, walls publicadas o constraints futuras.



## Valores de medición y unidades



Acá vive la **verdad del negocio**. Son entidades, enums y reglas que deberían sobrevivir aunque cambie la infraestructura.



- `src/FloorplanFit.Domain/Measurement/LengthUnit.cs` — Misión: Enum de unidades lineales soportadas. Importancia: sin este archivo faltaría vocabulario cerrado para expresar reglas de negocio sin ambigüedad. Use case: cuando el sistema detecta unidades del DXF y necesita normalizar medidas y tolerancias.

- `src/FloorplanFit.Domain/Measurement/MeasurementContext.cs` — Misión: Entidad que encapsula unidad fuente, factor a milímetros y tolerancias geométricas. Importancia: sin este archivo el dominio no podría representar esta parte del negocio de forma explícita y reusable. Use case: cuando el sistema detecta unidades del DXF y necesita normalizar medidas y tolerancias.



## Proyecto Domain



Acá vive la **verdad del negocio**. Son entidades, enums y reglas que deberían sobrevivir aunque cambie la infraestructura.



- `src/FloorplanFit.Domain/FloorplanFit.Domain.csproj` — Misión: Proyecto de Domain: núcleo puro del negocio sin dependencias de infraestructura. Importancia: sin este archivo no se podría compilar, restaurar ni referenciar correctamente este proyecto dentro de la solución. Use case: cuando la solución necesita restaurar dependencias, compilar este proyecto o usarlo como referencia desde otra capa.



## Adaptadores DXF



Acá viven los **adaptadores reales**: DXF, SQLite, filesystem y servicios técnicos.



- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaDxfGateway.cs` — Misión: Adaptador real del puerto IDxfGateway para leer metadata de DXF con IxMilia. Importancia: sin este archivo no habría integración real con DXF para esta responsabilidad concreta. Use case: cuando el runtime necesita leer un DXF real o extraerle información útil.

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaWallExtractor.cs` — Misión: Adaptador real del extractor de walls; filtra capas wall-like y descompone line/polyline en candidates. Importancia: sin este archivo no habría integración real con DXF para esta responsabilidad concreta. Use case: cuando el runtime necesita leer un DXF real o extraerle información útil.



## Persistencia SQLite y read-models



Acá viven los **adaptadores reales**: DXF, SQLite, filesystem y servicios técnicos.



- `src/FloorplanFit.Infrastructure/Persistence/SqliteCuratedWallRepository.cs` — Misión: Repositorio SQLite de curated walls. Importancia: sin este archivo no se podría persistir o rehidratar esta parte del modelo local en SQLite con una responsabilidad clara. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedWallCandidateRepository.cs` — Misión: Repositorio SQLite de wall candidates; también persiste su geometría en geometry_paths/segments. Importancia: sin este archivo no se podría persistir o rehidratar esta parte del modelo local en SQLite con una responsabilidad clara. Use case: cuando el pipeline necesita convertir geometría DXF en wall candidates revisables.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanCurationRepository.cs` — Misión: Repositorio SQLite de curations draft/published y su versionado. Importancia: sin este archivo no se podría persistir o rehidratar esta parte del modelo local en SQLite con una responsabilidad clara. Use case: cuando abrir Review exige crear o retomar un draft antes de seguir.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanExtractionSourceReader.cs` — Misión: Reader SQLite que resuelve qué archivo/version actual usar para extracción desde la Library. Importancia: sin este archivo no se podría persistir o rehidratar esta parte del modelo local en SQLite con una responsabilidad clara. Use case: cuando la pantalla principal necesita cargar, refrescar o derivar el estado visible de la Library.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanLibraryReader.cs` — Misión: Read-model SQLite de la Library; deriva Imported/Extracted/Curated Draft/Published. Importancia: sin este archivo no se podría persistir o rehidratar esta parte del modelo local en SQLite con una responsabilidad clara. Use case: cuando abrir Review exige crear o retomar un draft antes de seguir.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs` — Misión: Read-model SQLite de review; junta template summary, candidates, curated walls y geometry paths. Importancia: sin este archivo no se podría persistir o rehidratar esta parte del modelo local en SQLite con una responsabilidad clara. Use case: cuando la Review necesita abrir, leer, mostrar o validar su estado actual.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanTemplateRepository.cs` — Misión: Repositorio SQLite de floor plan templates y active published curation. Importancia: sin este archivo no se podría persistir o rehidratar esta parte del modelo local en SQLite con una responsabilidad clara. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanVersionRepository.cs` — Misión: Repositorio SQLite de versiones de floor plans. Importancia: sin este archivo no se podría persistir o rehidratar esta parte del modelo local en SQLite con una responsabilidad clara. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteImportedDocumentRepository.cs` — Misión: Repositorio SQLite de documentos importados. Importancia: sin este archivo no se podría persistir o rehidratar esta parte del modelo local en SQLite con una responsabilidad clara. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteMeasurementContextRepository.cs` — Misión: Repositorio SQLite de measurement contexts. Importancia: sin este archivo no se podría persistir o rehidratar esta parte del modelo local en SQLite con una responsabilidad clara. Use case: cuando el sistema detecta unidades del DXF y necesita normalizar medidas y tolerancias.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs` — Misión: Inicializador del schema SQLite; crea tablas y columnas necesarias del producto. Importancia: sin este archivo no se podría persistir o rehidratar esta parte del modelo local en SQLite con una responsabilidad clara. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteSession.cs` — Misión: Encapsula conexión y transacción activas de SQLite para que los repos compartan contexto. Importancia: sin este archivo no se podría persistir o rehidratar esta parte del modelo local en SQLite con una responsabilidad clara. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteUnitOfWork.cs` — Misión: Implementación SQLite del Unit of Work. Importancia: sin este archivo no se podría persistir o rehidratar esta parte del modelo local en SQLite con una responsabilidad clara. Use case: cuando el sistema detecta unidades del DXF y necesita normalizar medidas y tolerancias.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteWallExtractionRunRepository.cs` — Misión: Repositorio SQLite de corridas de extracción. Importancia: sin este archivo no se podría persistir o rehidratar esta parte del modelo local en SQLite con una responsabilidad clara. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.



## Workspace y runtime local



Acá viven los **adaptadores reales**: DXF, SQLite, filesystem y servicios técnicos.



- `src/FloorplanFit.Infrastructure/Runtime/AppWorkspace.cs` — Misión: Resuelve la carpeta workspace local de la app y sus subdirectorios gestionados. Importancia: sin este archivo el runtime local perdería una convención importante para ubicarse y operar. Use case: cuando el pipeline necesita mover, ubicar o identificar técnicamente archivos del workspace.



## Servicios de seguridad técnica



Acá viven los **adaptadores reales**: DXF, SQLite, filesystem y servicios técnicos.



- `src/FloorplanFit.Infrastructure/Security/Sha256FileHashService.cs` — Misión: Implementación real del cálculo SHA-256 sobre archivos. Importancia: sin este archivo faltaría una pieza técnica clave para identidad e integridad de archivos. Use case: cuando el pipeline necesita mover, ubicar o identificar técnicamente archivos del workspace.



## Storage gestionado



Acá viven los **adaptadores reales**: DXF, SQLite, filesystem y servicios técnicos.



- `src/FloorplanFit.Infrastructure/Storage/ManagedFileStorage.cs` — Misión: Implementación real del storage gestionado que copia DXF al library/raw-dxf del workspace. Importancia: sin este archivo no habría una implementación real y consistente del storage gestionado. Use case: cuando la pantalla principal necesita cargar, refrescar o derivar el estado visible de la Library.



## Proyecto Infrastructure



Acá viven los **adaptadores reales**: DXF, SQLite, filesystem y servicios técnicos.



- `src/FloorplanFit.Infrastructure/FloorplanFit.Infrastructure.csproj` — Misión: Proyecto de Infrastructure: implementaciones reales de DXF, SQLite, storage, hashing y runtime. Importancia: sin este archivo no se podría compilar, restaurar ni referenciar correctamente este proyecto dentro de la solución. Use case: cuando la solución necesita restaurar dependencias, compilar este proyecto o usarlo como referencia desde otra capa.

- `src/FloorplanFit.Infrastructure/InfrastructureAssemblyMarker.cs` — Misión: Marcador simple del ensamblado de Infrastructure útil para composición o referencias. Importancia: sin este archivo perderías un ancla mínima y segura para referenciar el ensamblado de Infrastructure sin arrastrar tipos con más responsabilidad. Use case: cuando la composición o alguna reflexión liviana necesita apuntar al ensamblado de Infrastructure de forma explícita.



## Tests de Application sobre curado



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/AcceptWallCandidateHandlerTests.cs` — Misión: Verifica que aceptar candidates actualice estado y cree curated walls correctamente. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando el usuario acepta una wall candidate propuesta por la extracción.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/FloorPlanCurationTests.cs` — Misión: Verifica invariantes de la entidad FloorPlanCuration. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando tocás esa zona del sistema y querés saber rápido si una regresión se coló.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/PublishFloorPlanCurationHandlerTests.cs` — Misión: Verifica reglas de publicación y activación de curaciones. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando el usuario publica una curación y deja una versión activa reutilizable.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/RejectWallCandidateHandlerTests.cs` — Misión: Verifica que rechazar candidates actualice estado y remueva curated walls derivadas. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando el usuario descarta una candidate que la extracción detectó mal o no quiere conservar.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/StartOrResumeCurationHandlerTests.cs` — Misión: Verifica creación/retoma de drafts de curación. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando abrir Review exige crear o retomar un draft antes de seguir.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/UpdateCuratedWallMetadataHandlerTests.cs` — Misión: Verifica persistencia de metadata semántica de curated walls. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando el usuario cambia role, mobility, protection u otra metadata desde el inspector de review.



## Tests de Application sobre extracción



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Application.Tests/FloorPlans/Extraction/ExtractWallCandidatesHandlerTests.cs` — Misión: Verifica orquestación del caso de uso de extracción sin depender del extractor real. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando el pipeline necesita convertir geometría DXF en wall candidates revisables.



## Tests de Application sobre importación



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Application.Tests/FloorPlans/Import/ImportFloorPlanHandlerTests.cs` — Misión: Verifica el flujo Application de importación de floor plans. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando el usuario elige un DXF y la app lo incorpora a la Library con su metadata y versión.



## Tests de Application sobre Library



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Application.Tests/FloorPlans/Library/GetFloorPlanLibraryHandlerTests.cs` — Misión: Verifica que el handler de Library devuelva el read-model esperado. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando la pantalla principal necesita cargar, refrescar o derivar el estado visible de la Library.



## Tests de Application sobre review



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/GetFloorPlanReviewSessionHandlerTests.cs` — Misión: Verifica la lectura de review sessions desde Application. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando la app necesita mostrar la sesión completa de review con summary, candidates y curated walls.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/OpenFloorPlanReviewSessionHandlerTests.cs` — Misión: Verifica que abrir review asegure draft activo y devuelva la sesión completa. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando el usuario abre Review por primera vez o reabre una sesión existente y la app tiene que hidratar draft, candidates y geometry.



## Proyecto de tests de Application



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj` — Misión: Proyecto de tests unitarios de Application. Importancia: sin este archivo no se podría compilar, restaurar ni referenciar correctamente este proyecto dentro de la solución. Use case: cuando la solución necesita restaurar dependencias, compilar este proyecto o usarlo como referencia desde otra capa.

- `tests/FloorplanFit.Application.Tests/GlobalUsings.cs` — Misión: Importaciones globales para simplificar los tests de Application. Importancia: sin este archivo habría más ruido y repetición en imports compartidos, sobre todo en tests. Use case: cuando esa suite necesita imports compartidos para que los tests sean más legibles y menos repetitivos.



## Tests de wiring Desktop



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Desktop.Tests/Composition/DesktopServiceRegistrationTests.cs` — Misión: Verifica que el contenedor DI desktop resuelva el slice principal sin faltantes. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando tocás esa zona del sistema y querés saber rápido si una regresión se coló.



## Tests de ViewModels Desktop



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs` — Misión: Verifica el comportamiento principal del ViewModel de review. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando la Review necesita abrir, leer, mostrar o validar su estado actual.

- `tests/FloorplanFit.Desktop.Tests/ViewModels/LibraryViewModelTests.cs` — Misión: Verifica el comportamiento principal del ViewModel de Library. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando la pantalla principal necesita cargar, refrescar o derivar el estado visible de la Library.



## Tests del preview Desktop



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewGeometryTests.cs` — Misión: Verifica centrado, proyección y estilo del highlight en el preview geométrico de review. Importancia: sin este archivo una regresión visual clave del preview podría pasar desapercibida hasta el test manual. Use case: cuando tocás el canvas de review y querés saber rápido si sigue centrando y resaltando bien.



## Tests de layout Desktop



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs` — Misión: Verifica que la ventana de review no vuelva a caer en alturas rígidas para preview y curated walls. Importancia: sin este archivo el layout podría degradarse silenciosamente y volver al problema de mostrar solo unas pocas líneas. Use case: cuando tocás la composición visual de la review y querés blindar sizing y scroll contra regresiones.



## Proyecto de tests de Desktop



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj` — Misión: Proyecto de tests del wiring y viewmodels de Desktop. Importancia: sin este archivo no se podría compilar, restaurar ni referenciar correctamente este proyecto dentro de la solución. Use case: cuando la solución necesita restaurar dependencias, compilar este proyecto o usarlo como referencia desde otra capa.



## Tests de persistencia de curación



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanCurationPersistenceIntegrationTests.cs` — Misión: Verifica round-trips reales de persistencia SQLite para curations y curated walls. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.



## Tests de adaptadores DXF



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Infrastructure.Tests/Dxf/IxMiliaDxfGatewayTests.cs` — Misión: Verifica lectura real de metadata DXF a través del gateway IxMilia. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando el runtime necesita leer un DXF real o extraerle información útil.



## Tests del extractor real



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaWallExtractorTests.cs` — Misión: Verifica extracción real de wall candidates desde fixtures DXF. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando el pipeline necesita convertir geometría DXF en wall candidates revisables.



## Tests del pipeline de importación



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Infrastructure.Tests/Imports/FloorPlanLibraryReaderIntegrationTests.cs` — Misión: Verifica que el read-model de Library derive los estados correctos desde SQLite. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando el usuario elige un DXF y la app lo incorpora a la Library con su metadata y versión.

- `tests/FloorplanFit.Infrastructure.Tests/Imports/ImportFloorPlanIntegrationTests.cs` — Misión: Verifica el pipeline real de importación con SQLite, storage y DXF. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando el usuario elige un DXF y la app lo incorpora a la Library con su metadata y versión.

- `tests/FloorplanFit.Infrastructure.Tests/Imports/ManagedFileStorageTests.cs` — Misión: Verifica la copia gestionada de DXF al workspace. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando el usuario elige un DXF y la app lo incorpora a la Library con su metadata y versión.



## Tests del source reader



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Infrastructure.Tests/Library/FloorPlanExtractionSourceReaderIntegrationTests.cs` — Misión: Verifica la resolución del source de extracción para la versión actual. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando la pantalla principal necesita cargar, refrescar o derivar el estado visible de la Library.



## Tests del read-model de review



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Infrastructure.Tests/Review/OpenFloorPlanReviewSessionIntegrationTests.cs` — Misión: Test de integración que verifica que el primer Open Review después de extracción no reviente por reutilizar una transacción SQLite ya commiteada. Importancia: sin este archivo el bug del primer Open Review después de extraer podía reaparecer sin alarma automática. Use case: cuando alguien vuelve a tocar transacciones SQLite, apertura de drafts o el read-model de review.

- `tests/FloorplanFit.Infrastructure.Tests/Review/FloorPlanReviewSessionReaderIntegrationTests.cs` — Misión: Verifica que el read-model de review hidrate candidates, curated walls y geometría. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando la Review necesita abrir, leer, mostrar o validar su estado actual.



## Soporte común de tests



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Infrastructure.Tests/TestSupport/RepositoryPaths.cs` — Misión: Centraliza paths del repo/fixtures para los tests de Infrastructure. Importancia: sin este archivo una regresión en este comportamiento podría pasar desapercibida hasta bastante tarde. Use case: cuando la app persiste estado real en SQLite o rehidrata vistas desde la base local.



## Proyecto de tests de Infrastructure



Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.



- `tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj` — Misión: Proyecto de tests de integración/unidad de Infrastructure. Importancia: sin este archivo no se podría compilar, restaurar ni referenciar correctamente este proyecto dentro de la solución. Use case: cuando la solución necesita restaurar dependencias, compilar este proyecto o usarlo como referencia desde otra capa.

- `tests/FloorplanFit.Infrastructure.Tests/GlobalUsings.cs` — Misión: Importaciones globales compartidas por los tests de Infrastructure. Importancia: sin este archivo habría más ruido y repetición en imports compartidos, sobre todo en tests. Use case: cuando esa suite necesita imports compartidos para que los tests sean más legibles y menos repetitivos.



## Verificación de cobertura



- Conteo total cubierto: **209**

- Base f?sica actual: **202 archivos versionados presentes**

- Diferencia contra la versión inicial del mapa: **8 archivos nuevos incorporados desde la última cobertura completa**

- Método de verificación: `git ls-files` + contraste manual de faltantes contra esta versión del mapa.

- Criterio de completitud: cada path relevante presente hoy en el working tree aparece exactamente una vez en este documento.



## Qué NO cubre este mapa



- `bin/`, `obj/`, `.vs/`, caches de herramientas y otros artefactos generados.

- Directorios no versionados como residuos operativos locales.

- Estado runtime efímero del workspace `src/FloorplanFit.Desktop/bin/.../workspace/` porque cambia ejecución a ejecución.



## Cómo usar este documento



1. Si querés entender **producto**, arrancá por `MVP-UX.md` y `TECH-STACK-ARCHITECTURE-DATAFLOW.md`.

2. Si querés entender **flujo Loop 1**, seguí: Desktop Library -> Import handler -> Extract handler -> Review session reader -> Review UI.

3. Si querés entender **persistencia**, recorré `SqliteSchemaInitializer` y luego cada repo/read-model de `src/FloorplanFit.Infrastructure/Persistence/`.

4. Si querés entender **evidencia**, terminá en `tests/` por capa.
