# Explicación de archivos tocados en Slice 1

## Objetivo

Este documento explica, en lenguaje simple, qué hace cada archivo que tocamos para arrancar el **Slice 1** del proyecto: pasar de un `DXF` real a un floor plan reconocido por el sistema.

---

## 1. Archivos raíz

### `FloorplanFit.sln`
Organiza toda la solución. Es el archivo que agrupa los proyectos principales y los tests para que Visual Studio o `dotnet` entiendan la estructura general.

### `global.json`
Fija la versión esperada del SDK de .NET. Sirve para que el proyecto no dependa de “en mi máquina funciona”.

### `Directory.Build.props`
Centraliza configuración compartida de todos los proyectos, por ejemplo:

- framework objetivo
- nullability
- implicit usings
- versión de lenguaje

Esto evita repetir la misma configuración en cada `.csproj`.

---

## 2. Documentación de diseño y plan

### `docs/superpowers/specs/2026-04-25-slice-1-import-foundation-design.md`
Explica **qué resuelve** el Slice 1, qué queda afuera y cuál es el flujo conceptual del import.

### `docs/superpowers/plans/2026-04-25-slice-1-import-foundation.md`
Baja ese diseño a una secuencia de implementación. Dice en qué orden conviene construir la base técnica.

---

## 3. Proyectos y archivos de contratos

### `src/FloorplanFit.Contracts/FloorplanFit.Contracts.csproj`
Proyecto para los contratos simples que usan las capas externas para hablar con la aplicación.

### `src/FloorplanFit.Contracts/FloorPlans/ImportFloorPlanRequest.cs`
Representa el pedido para importar un floor plan. Hoy solo necesita la ruta del archivo.

### `src/FloorplanFit.Contracts/FloorPlans/ImportFloorPlanResponse.cs`
Representa la respuesta del caso de uso de importación. Devuelve el item listo para mostrarse en library.

### `src/FloorplanFit.Contracts/FloorPlans/FloorPlanLibraryItemDto.cs`
Es el objeto simple que la pantalla puede mostrar en la biblioteca:

- identificador
- código
- nombre
- estado
- versión activa
- fecha de importación
- unidad fuente

---

## 4. Proyectos y archivos del dominio

### `src/FloorplanFit.Domain/FloorplanFit.Domain.csproj`
Proyecto para los objetos centrales del negocio.

### `src/FloorplanFit.Domain/Measurement/LengthUnit.cs`
Enum con las unidades de longitud que el sistema reconoce. Es la base para trabajar con escala y medidas consistentes.

### `src/FloorplanFit.Domain/Measurement/MeasurementContext.cs`
Guarda el contexto de medida del archivo importado:

- unidad detectada
- factor de conversión a milímetros
- tolerancias básicas
- fecha de creación

Sirve para no perder la trazabilidad de escala desde el minuto uno.

### `src/FloorplanFit.Domain/Documents/ImportedDocumentType.cs`
Enum para clasificar qué tipo de documento fue importado.

### `src/FloorplanFit.Domain/Documents/ImportedDocument.cs`
Representa el archivo original que entró al sistema:

- nombre
- ruta
- hash
- versión DXF
- vínculo al contexto de medida
- fecha de importación

La idea es que el sistema institucionalice el archivo, no que quede como algo suelto.

### `src/FloorplanFit.Domain/FloorPlans/FloorPlanTemplate.cs`
Representa la entrada reusable del floor plan dentro de la biblioteca:

- código
- nombre
- si está activo
- cuál es su versión actual

Es el objeto que después va a vivir más allá de una sola importación.

### `src/FloorplanFit.Domain/FloorPlans/FloorPlanVersion.cs`
Representa una versión concreta del floor plan:

- a qué floor plan pertenece
- qué documento la originó
- una huella geométrica base
- número de versión
- fecha de creación

Nos da versionado desde el comienzo.

---

## 5. Proyectos y archivos de aplicación

### `src/FloorplanFit.Application/FloorplanFit.Application.csproj`
Proyecto para los casos de uso y las interfaces que separan la lógica del negocio de la infraestructura concreta.

### `src/FloorplanFit.Application/Abstractions/DetectedFloorPlanDocument.cs`
Objeto simple que representa lo que devuelve el lector DXF:

- nombre original
- nombre sugerido
- unidad
- factor de conversión
- versión DXF
- huella geométrica base

### `src/FloorplanFit.Application/Abstractions/IDxfGateway.cs`
Interfaz para leer un floor plan DXF sin acoplar la lógica a una librería específica.

### `src/FloorplanFit.Application/Abstractions/IFloorPlanTemplateRepository.cs`
Interfaz para guardar y buscar floor plans de library.

### `src/FloorplanFit.Application/Abstractions/IFloorPlanVersionRepository.cs`
Interfaz para guardar versiones y calcular el siguiente número de versión.

### `src/FloorplanFit.Application/Abstractions/IImportedDocumentRepository.cs`
Interfaz para persistir el documento importado.

### `src/FloorplanFit.Application/Abstractions/IMeasurementContextRepository.cs`
Interfaz para persistir el contexto de medición.

### `src/FloorplanFit.Application/Abstractions/IUnitOfWork.cs`
Interfaz para guardar todos los cambios de una sola vez, como una operación coherente.

### `src/FloorplanFit.Application/Abstractions/IFileHashService.cs`
Interfaz para calcular el hash del archivo sin meter lógica de filesystem dentro del dominio.

### `src/FloorplanFit.Application/Abstractions/IClock.cs`
Interfaz para obtener la hora actual de forma testeable.

### `src/FloorplanFit.Application/FloorPlans/Import/FloorPlanCodeNormalizer.cs`
Toma un nombre y lo convierte en un código consistente para la biblioteca.  
Ejemplo: `SANTA-BARBARA` -> `santa-barbara`.

### `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanResultFactory.cs`
Arma la respuesta final que la pantalla puede consumir.  
Su trabajo es separar la lógica interna del formato de salida.

### `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanHandler.cs`
Es la pieza principal del Slice 1. Coordina el flujo:

1. leer metadatos del DXF
2. crear contexto de medida
3. calcular hash
4. crear documento importado
5. crear o recuperar el floor plan de library
6. crear la versión
7. guardar todo
8. devolver el item visible en library

---

## 6. Proyecto y archivo de infraestructura

### `src/FloorplanFit.Infrastructure/FloorplanFit.Infrastructure.csproj`
Proyecto reservado para la infraestructura concreta: DXF real, SQLite real, filesystem real.

### `src/FloorplanFit.Infrastructure/InfrastructureAssemblyMarker.cs`
Archivo mínimo para marcar la existencia del proyecto de infraestructura.  
Hoy no hace lógica de negocio; deja listo el lugar correcto donde después pondremos adaptadores reales.

---

## 7. Proyecto y archivo de tests

### `tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj`
Proyecto de pruebas del caso de uso de aplicación.

### `tests/FloorplanFit.Application.Tests/FloorPlans/Import/ImportFloorPlanHandlerTests.cs`
Primer test del Slice 1.

Su objetivo es verificar, con dobles simples en memoria, que el caso de uso:

- genere código y nombre correctos
- cree contexto de medida
- cree documento importado
- cree floor plan reusable
- cree versión 1
- y llame al guardado final

Todavía no prueba DXF real ni SQLite real.  
Eso viene después, cuando conectemos infraestructura concreta.

---

## 8. Archivos de memoria del proyecto que también tocamos

### `obsidian-vault/Current State.md`
Resume el estado actual del proyecto. Lo fuimos actualizando para que la verdad vigente del repo no quede solo en la conversación.

### `obsidian-vault/Decisions/2026-04-25 - Santa Barbara as First Canonical Fixture.md`
Explica por qué `SANTA-BARBARA.dxf` es el primer caso conductor.

### `obsidian-vault/Decisions/2026-04-25 - DXF as Primary Truth and Catalog as Legacy Reference.md`
Deja asentado que los `DXF` son la verdad primaria y que `PLANS/catalog/` queda solo como referencia legacy.

### `obsidian-vault/Decisions/2026-04-25 - Slice 1 Import Foundation Architecture.md`
Explica la arquitectura mínima elegida para este primer slice.

---

## 9. Cómo leer todo esto sin perderte

Si querés entender el arranque del proyecto en orden, te conviene leer así:

1. `docs/superpowers/specs/2026-04-25-slice-1-import-foundation-design.md`
2. `docs/superpowers/plans/2026-04-25-slice-1-import-foundation.md`
3. `src/FloorplanFit.Contracts/FloorPlans/*`
4. `src/FloorplanFit.Domain/**/*`
5. `src/FloorplanFit.Application/**/*`
6. `tests/FloorplanFit.Application.Tests/FloorPlans/Import/ImportFloorPlanHandlerTests.cs`

Ese orden va de la idea general al comportamiento concreto.
