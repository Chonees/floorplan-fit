# Explicacion exhaustiva de archivos del Slice 1 ejecutable

## Big Picture

Este documento explica, archivo por archivo, el estado REAL del Slice 1 ejecutable despues de todos los cambios del **2026-04-29**.

Este slice pertenece a:

- **Loop 1** del producto
- capas **Desktop**, **Application**, **Infrastructure** y **Tests**
- una base compartida de desarrollo para correr siempre la ultima version de la app

El camino funcional actual es este:

`elegir DXF -> copiarlo al workspace de la app -> leer metadata real -> calcular hash real -> persistir en SQLite -> devolver item de Library -> rehidratar Library desde SQLite al abrir`

Y el camino operativo recomendado para desarrollar es este:

`abrir scripts/dev-desktop.bat -> dotnet watch run -> iterar siempre contra el codigo mas nuevo`

---

## 1. Application

### `src/FloorplanFit.Application/Abstractions/IManagedFileStorage.cs`

**Capa:** Application  
**Para que existe:** define la costura entre el caso de uso y el filesystem gestionado por la app.  
**Responsabilidad:** copiar un archivo externo elegido por el usuario hacia `library/raw-dxf/` y devolver la ruta gestionada final.  
**Por que importa:** separa dos problemas distintos:

- interpretar DXF
- poseer/copiar archivo

### `src/FloorplanFit.Application/Abstractions/IFloorPlanLibraryReader.cs`

**Capa:** Application  
**Para que existe:** define la costura de lectura de Library.  
**Responsabilidad:** devolver una lista de `FloorPlanLibraryItemDto` sin exponer SQL ni detalles de persistencia a Desktop.  
**Por que importa:** hace que la UI pida una capacidad del producto, no una consulta tecnica.

### `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanHandler.cs`

**Capa:** Application  
**Para que existe:** es el caso de uso principal de import real.  
**Que hace hoy:**

1. copia el DXF al storage gestionado
2. lee metadata desde la copia gestionada
3. calcula hash sobre la copia gestionada
4. crea `MeasurementContext`, `ImportedDocument`, `FloorPlanTemplate` y `FloorPlanVersion`
5. persiste todo en SQLite via repositorios
6. devuelve el item de Library

**Cambio conceptual clave:** la identidad logica del floor plan ya NO depende del nombre fisico de la copia gestionada.  
**Por que importa:** evita bugs como `santa-barbara-2` cuando el mismo DXF se reimporta.

### `src/FloorplanFit.Application/FloorPlans/Library/GetFloorPlanLibraryHandler.cs`

**Capa:** Application  
**Para que existe:** es el caso de uso de lectura de Library.  
**Responsabilidad:** pedirle al reader la lista actual y devolverla a la UI.  
**Por que importa:** deja a Desktop fuera de SQLite directo.

---

## 2. Desktop

### `src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj`

**Capa:** Desktop  
**Para que existe:** es el proyecto ejecutable de Avalonia.  
**Que define:**

- `OutputType = WinExe`
- compiled bindings de Avalonia
- referencias a `Application`, `Contracts` e `Infrastructure`
- dependencias de UI, MVVM y hosting

**Por que importa:** marca el paso de librerias internas a producto ejecutable real.

### `src/FloorplanFit.Desktop/Program.cs`

**Capa:** Desktop  
**Para que existe:** es el composition root del slice.  
**Que hace:**

1. calcula el `workspaceRoot`
2. construye DI/host
3. registra servicios con `AddDesktopSlice1`
4. crea directorios de workspace
5. inicializa el schema SQLite
6. arranca Avalonia

**Por que importa:** centraliza el armado de la app real.

### `src/FloorplanFit.Desktop/App.axaml`

**Capa:** Desktop  
**Para que existe:** define el `Application` de Avalonia y el theme base.  
**Rol:** bootstrap visual, sin logica de producto.

### `src/FloorplanFit.Desktop/App.axaml.cs`

**Capa:** Desktop  
**Para que existe:** conecta el ciclo de vida de Avalonia con el contenedor DI.  
**Que hace hoy:**

- resuelve `LibraryViewModel` desde `Program.Host`
- crea `MainWindow` con constructor vacio
- asigna `DataContext`
- publica la ventana principal

**Por que importa:** este cambio saco la inyeccion por constructor de `MainWindow` y elimino la causa de `AVLN3001`.

### `src/FloorplanFit.Desktop/MainWindow.axaml`

**Capa:** Desktop  
**Para que existe:** define la UI minima de Library.  
**Que muestra:**

- titulo
- boton `Import DXF`
- `StatusMessage`
- lista de items con `Code`, `Name`, `Status`, `ActiveVersionNumber` y `SourceUnit`

**Por que esta bien que sea minima:** este slice valida costuras reales, no una UI final linda.

### `src/FloorplanFit.Desktop/MainWindow.axaml.cs`

**Capa:** Desktop  
**Para que existe:** maneja la interaccion minima de ventana y file picker.  
**Que hace hoy:**

- constructor vacio que engancha el evento `Opened`
- `ImportButton_OnClick(...)` abre el file picker y delega en `LibraryViewModel.ImportAsync(...)`
- `MainWindow_OnOpened(...)` llama `LibraryViewModel.LoadAsync(...)`

**Cambio conceptual importante:** esta ventana ahora dispara la hidratacion inicial de Library al abrir.

### `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`

**Capa:** Desktop  
**Para que existe:** adapta la UI al caso de uso de import y al caso de uso de lectura de Library.  
**Estado expuesto:**

- `Items`
- `StatusMessage`

**Metodos clave:**

- `LoadAsync(...)`: carga la Library desde persistencia real
- `ImportAsync(...)`: importa el DXF y luego refresca la lista desde SQLite
- `RefreshItemsAsync(...)`: resuelve `GetFloorPlanLibraryHandler`, limpia `Items` y repuebla la lista

**Cambio conceptual importante:** paso de ser una pantalla session-first a una pantalla persisted-first.

### `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`

**Capa:** Desktop / composition  
**Para que existe:** concentra el wiring completo del Slice 1.  
**Que registra:**

- `AppWorkspace`
- `IManagedFileStorage`
- `IDxfGateway`
- `IFileHashService`
- `IClock`
- `ImportFloorPlanResultFactory`
- `SqliteSession`
- repositorios SQLite
- `IFloorPlanLibraryReader`
- `IUnitOfWork`
- `ImportFloorPlanHandler`
- `GetFloorPlanLibraryHandler`
- `LibraryViewModel`

**Por que importa:** deja claro el ciclo de vida correcto entre singletons de UI y servicios scoped de SQLite.

---

## 3. Infrastructure

### `src/FloorplanFit.Infrastructure/FloorplanFit.Infrastructure.csproj`

**Capa:** Infrastructure  
**Para que existe:** declara las dependencias concretas de DXF y SQLite.  
**Que agrega:** `IxMilia.Dxf`, `Microsoft.Data.Sqlite` y referencias internas.  
**Por que importa:** convierte Infrastructure en implementacion real.

### `src/FloorplanFit.Infrastructure/Runtime/AppWorkspace.cs`

**Capa:** Infrastructure  
**Para que existe:** modela el workspace fisico de runtime.  
**Que calcula:**

- `RootPath`
- `LibraryDirectory`
- `LibraryRawDxfDirectory`
- `DatabasePath`

**Metodo clave:** `EnsureCreated()` crea `workspace`, `library`, `library/raw-dxf`.

### `src/FloorplanFit.Infrastructure/Storage/ManagedFileStorage.cs`

**Capa:** Infrastructure  
**Para que existe:** implementa la politica de copiar primero al storage gestionado.  
**Que hace:**

- valida rutas
- asegura workspace
- copia el DXF
- usa sufijos `-2`, `-3`, etc. si el nombre ya existe

**Por que importa:** institucionaliza el archivo dentro de la app antes de operar sobre el.

### `src/FloorplanFit.Infrastructure/Security/Sha256FileHashService.cs`

**Capa:** Infrastructure  
**Para que existe:** calcula SHA-256 real de archivos.  
**Detalle:** devuelve hex en lowercase para evitar comparaciones por casing.

### `src/FloorplanFit.Infrastructure/Dxf/IxMiliaDxfGateway.cs`

**Capa:** Infrastructure  
**Para que existe:** adapta `IxMilia.Dxf` al puerto de Application.  
**Que resuelve:**

- carga de `DxfFile`
- deteccion de unidad (`$INSUNITS` con fallback a `$MEASUREMENT`)
- factor a milimetros
- bounding box
- fingerprint geometrico inicial

**Por que importa:** permite leer DXF real sin acoplar la capa Application a la libreria externa.

### `src/FloorplanFit.Infrastructure/Persistence/SqliteSession.cs`

**Capa:** Infrastructure  
**Para que existe:** encapsula `SqliteConnection`, `SqliteTransaction` y commit/rollback.  
**Por que importa:** centraliza el lifecycle de persistencia transaccional.

### `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`

**Capa:** Infrastructure  
**Para que existe:** crea el schema minimo del slice.  
**Tablas:**

- `measurement_contexts`
- `imported_documents`
- `floorplan_templates`
- `floorplan_versions`

### `src/FloorplanFit.Infrastructure/Persistence/SqliteMeasurementContextRepository.cs`

**Capa:** Infrastructure  
**Para que existe:** persiste `MeasurementContext`.  
**Detalle importante:** guarda decimales con `InvariantCulture`.

### `src/FloorplanFit.Infrastructure/Persistence/SqliteImportedDocumentRepository.cs`

**Capa:** Infrastructure  
**Para que existe:** persiste `ImportedDocument`.  
**Punto clave:** `storage_path` ya apunta a la ruta gestionada por la app.

### `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanTemplateRepository.cs`

**Capa:** Infrastructure  
**Para que existe:** persiste y recupera `FloorPlanTemplate`.  
**Metodos importantes:**

- `GetByCodeAsync(...)`
- `AddAsync(...)`
- `UpdateAsync(...)`

**Por que importa:** sostiene la idea de library reusable.

### `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanVersionRepository.cs`

**Capa:** Infrastructure  
**Para que existe:** persiste `FloorPlanVersion` y calcula el siguiente numero de version.  
**Metodo clave:** `GetNextVersionNumberAsync(...)`.

### `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanLibraryReader.cs`

**Capa:** Infrastructure  
**Para que existe:** reconstruye la Library actual desde SQLite real.  
**Que hace:**

- une `floorplan_templates`, `floorplan_versions`, `imported_documents` y `measurement_contexts`
- devuelve `FloorPlanLibraryItemDto`
- usa la `current_version` del template

**Por que importa:** es la pieza que permite reabrir la app y volver a ver la Library persistida.

### `src/FloorplanFit.Infrastructure/Persistence/SqliteUnitOfWork.cs`

**Capa:** Infrastructure  
**Para que existe:** conecta `IUnitOfWork` con el commit real de SQLite.

---

## 4. Tests

### `tests/FloorplanFit.Application.Tests/FloorPlans/Import/ImportFloorPlanHandlerTests.cs`

**Capa:** Tests  
**Para que existe:** protege la orquestacion del caso de uso de import.  
**Que cubre hoy:**

- happy path del import
- comportamiento sobre copia gestionada
- reimport del mismo source file como version 2 del mismo template

### `tests/FloorplanFit.Application.Tests/FloorPlans/Library/GetFloorPlanLibraryHandlerTests.cs`

**Capa:** Tests  
**Para que existe:** protege el caso de uso de lectura de Library a nivel Application puro.  
**Que verifica:** que `GetFloorPlanLibraryHandler` devuelve exactamente lo que entrega `IFloorPlanLibraryReader`.

### `tests/FloorplanFit.Infrastructure.Tests/TestSupport/RepositoryPaths.cs`

**Capa:** Tests  
**Para que existe:** encuentra la raiz de la solucion para localizar fixtures reales como `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`.

### `tests/FloorplanFit.Infrastructure.Tests/Imports/ManagedFileStorageTests.cs`

**Capa:** Tests  
**Para que existe:** prueba la politica de copia gestionada.  
**Que verifica:**

- copia al directorio correcto
- preservacion de nombre/contenido
- sufijos numericos ante colision

### `tests/FloorplanFit.Infrastructure.Tests/Dxf/IxMiliaDxfGatewayTests.cs`

**Capa:** Tests  
**Para que existe:** prueba el gateway DXF real contra `SANTA-BARBARA.dxf`.  
**Que verifica:** metadatos reales, unidad, version DXF y fingerprint valido.

### `tests/FloorplanFit.Infrastructure.Tests/Imports/ImportFloorPlanIntegrationTests.cs`

**Capa:** Tests  
**Para que existe:** prueba la costura end-to-end del import real.  
**Que cubre hoy:**

- persistencia real en SQLite
- copia fisica del DXF al workspace
- DTO de respuesta correcto
- reimport del mismo DXF como version 2 del mismo template

### `tests/FloorplanFit.Infrastructure.Tests/Imports/FloorPlanLibraryReaderIntegrationTests.cs`

**Capa:** Tests  
**Para que existe:** prueba que la Library se reconstruya desde SQLite despues de imports reales.  
**Que verifica:**

- un solo template logico para `santa-barbara`
- `ActiveVersionNumber = 2`
- `SourceUnit = inch`

---

## 5. Archivos operativos de desarrollo

### `scripts/dev-desktop.bat`

**Capa:** tooling / soporte operativo  
**Para que existe:** es el launcher recomendado para desarrollar Desktop siempre contra el codigo mas nuevo.  
**Que hace:**

- se posiciona en la raiz del repo
- muestra un header claro
- detecta si `FloorplanFit.Desktop.exe` ya esta corriendo
- avisa por posibles locks
- ejecuta `dotnet watch --non-interactive run --project "src\FloorplanFit.Desktop\FloorplanFit.Desktop.csproj"`

**Por que importa:** evita confundir un `.exe` viejo con el estado real del codigo.

### `C:\Users\lucas\OneDrive\Escritorio\Floorplan Fit Dev Watch.lnk`

**Que es:** shortcut de escritorio al `.bat` anterior.  
**Para que sirve:** abrir rapido el flujo dev correcto sin depender del `.exe` compilado.

### `C:\Users\lucas\OneDrive\Escritorio\Floorplan Fit.lnk`

**Que es:** shortcut viejo al `.exe` compilado.  
**Para que sirve:** smoke test manual puntual.  
**Riesgo conocido:** puede apuntar a un output stale si no se regenero despues del ultimo cambio.

---

## 6. Donde quedo toda la documentacion

### Documentacion tecnica / narrativa en `docs/explicacion del proyecto/`

- `2026-04-25 - explicacion de archivos tocados en slice 1.md`
- `2026-04-29 - explicacion exhaustiva de archivos del slice 1 ejecutable.md`
- `2026-04-29 - validacion real del slice 1 ejecutable.md`
- `2026-04-29 - bugfix de versionado al reimportar el mismo dxf.md`
- `2026-04-29 - hidratacion de library desde sqlite y fix de avln3001.md`
- `2026-04-29 - launcher de desarrollo continuo para desktop.md`
- `2026-04-29 - checklist manual del slice 1 ejecutable.md`
- `2026-04-29 - acceso directo local para abrir floorplan fit.md`

### Estado y trazabilidad en `obsidian-vault/`

- `Current State.md`
- `Implementation/2026-04-29 - Desktop Dev Watch Launcher.md`
- `Implementation/2026-04-29 - Library Startup Hydration and MainWindow Loader Fix.md`
- `Implementation/2026-04-29 - Reimport Versioning Bug Fixed.md`
- `Implementation/2026-04-29 - Validation Results and App Control Blockers.md`
- `Implementation/2026-04-29 - Manual Validation Checklist for Slice 1 Executable.md`
- y el resto de notas de implementacion previas del mismo dia

---

## Estado real despues de esta sincronizacion

Con este documento, la explicacion archivo por archivo ya queda alineada con:

- import real
- reimport como version 2
- hidratacion de Library desde SQLite
- fix de `AVLN3001`
- launcher dev recomendado con `.bat`

La deuda que habia antes era documental: el documento exhaustivo se habia quedado en una foto intermedia del dia.  
Eso YA no es cierto: esta version queda sincronizada con el estado actual del repo.
