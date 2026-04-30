# 2026-04-29 - launcher de desarrollo continuo para Desktop

## Problema que estabamos resolviendo

Teniamos una confusion operativa bastante clasica:

- por un lado el codigo nuevo ya estaba arreglado
- por el otro, el usuario podia seguir abriendo un `.exe` viejo desde el acceso directo del escritorio

Eso hacia que pareciera que "el fix no anduvo", cuando en realidad se estaba ejecutando otro output.

## Solucion elegida

Se creo:

- `scripts/dev-desktop.bat`
- `C:\Users\lucas\OneDrive\Escritorio\Floorplan Fit Dev Watch.lnk`

Este archivo corre la app usando:

- `dotnet watch --non-interactive run --project "src\FloorplanFit.Desktop\FloorplanFit.Desktop.csproj"`

## Por que esta opcion es la recomendada

Porque para desarrollo continuo necesitamos priorizar:

1. correr siempre lo ultimo
2. no depender de un `.exe` potencialmente stale
3. tener un loop corto de cambio -> guardado -> recompilacion/reinicio

En criollo: este `.bat` hace de equivalente practico a un modo "Vite" para nuestra app desktop.

## Que hace internamente

El launcher:

1. se mueve a la raiz del repo
2. muestra un encabezado para que quede claro que estas en modo desarrollo
3. detecta si `FloorplanFit.Desktop.exe` ya esta abierto
4. si encuentra una instancia abierta, avisa que puede haber locks sobre DLLs
5. lanza `dotnet watch run`

## Tradeoff

### Opcion elegida
`dotnet watch run` dentro de un `.bat`

**Ventaja**
- siempre apunta al codigo mas nuevo
- reduce la confusion con binarios viejos
- sirve mejor para iteracion diaria

**Costo**
- hay que dejar una consola abierta

### Alternativa descartada
abrir el `.exe` de `bin/Debug/net10.0`

**Ventaja**
- mas rapido para hacer doble click

**Problema**
- puede abrir una build vieja y mezclar validacion manual con outputs stale

## Recomendacion de uso

Para desarrollar:

- usar `scripts/dev-desktop.bat`
- o abrir `Floorplan Fit Dev Watch.lnk` desde el escritorio, que apunta a ese `.bat`

Para smoke test puntual:

- podes seguir usando el `.exe` o el acceso directo del escritorio, pero solo si sabes que ese output fue regenerado despues del ultimo cambio
