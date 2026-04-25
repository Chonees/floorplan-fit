# MVP UX — Floorplan Fit

## Objetivo del MVP

El MVP tiene que sentirse como una herramienta de **decisión técnica asistida**, no como un CAD para redibujar planos.

La promesa UX correcta es:

> **curás un floor plan una vez, lo reutilizás muchas veces, y cada site plan nuevo se convierte en un flujo corto de validación y decisión**

---

## Principios UX

1. **Review > redraw**  
   El usuario revisa y corrige; no redibuja todo.

2. **Una vez curado, reutilizable**  
   El curado publicado queda como versión canónica reutilizable.

3. **Siempre editable**  
   Publicado **no significa bloqueado para siempre**.  
   El usuario puede volver a editar cuando quiera y republicar una nueva versión.

4. **Site plan mostly automatic**  
   En la fase de site plan, el sistema debe entender el envelope correctamente en el **90%+** de los casos.  
   El usuario solo edita si realmente falló la detección.

5. **1:1 real**  
   Todo se visualiza en escala real estandarizada.

6. **Comparar opciones, no adivinar**  
   El usuario debe ver opciones de encaje comparables y auditables.

---

# Loop 1 — Curado de Floor Plan (una vez por tipología)

## Pantalla 1 — Library

El usuario abre la app y ve la librería de floor plans.

Cada item muestra:

- nombre
- código
- versión activa
- estado:
  - Imported
  - Extracted
  - Curated Draft
  - Published
- fecha de última publicación

### Qué siente el usuario

“Estoy viendo activos reutilizables del estudio, no archivos sueltos.”

---

## Pantalla 2 — Import Floor Plan DXF

El usuario arrastra o selecciona un DXF.

La app:

- parsea el archivo
- detecta unidades
- normaliza escala
- extrae walls candidatas
- arma preview inicial

### Qué siente el usuario

“El sistema ya hizo el trabajo técnico inicial; yo no tengo que empezar de cero.”

---

## Pantalla 3 — Review de Walls Extraídas

La app muestra:

- canvas con walls detectadas
- lista de candidates
- highlights por selección

El usuario puede:

- aceptar
- rechazar
- fusionar
- dividir
- corregir detecciones equivocadas

### Qué siente el usuario

“El software propone y yo valido.”

---

## Pantalla 4 — Curado de Walls

Para cada wall aceptada, el usuario puede:

- asignar `stable_wall_id`
- definir:
  - `locked`
  - `flexible`
  - `stretchable`
- marcar:
  - interior
  - exterior
  - fachada
  - grupo
- agregar notas

Opcionalmente también puede:

- agrupar walls importantes
- dejar intención libre
  - “no tocar frente”
  - “muro principal”
  - “circular por acá”

### Qué siente el usuario

“Estoy convirtiendo un DXF crudo en una versión utilizable por el sistema.”

---

## Pantalla 5 — Publicar Curado

El usuario aprieta algo tipo:

- `Publish Curation`
- `Set Active Version`

Eso genera:

- una **versión canónica publicada**
- persistida en DB
- reutilizable para futuros site plans

### Importante

**Publicado no significa congelado para siempre.**

El usuario siempre puede:

- volver a abrir el floor plan
- editar el curado
- guardar como draft
- republicar una nueva versión activa

O sea:

> el sistema conserva una versión activa reusable,  
> pero nunca le quita al usuario la posibilidad de editarla después

### Qué siente el usuario

“Esto ya quedó listo para usar en proyectos reales, pero sigo teniendo control si mañana quiero mejorarlo.”

---

# Loop 2 — Adaptación por Site Plan (muchas veces)

## Pantalla 6 — Nuevo Proyecto

El usuario:

- crea un proyecto
- importa `site plan DXF`
- selecciona un `floor plan` ya curado y publicado

### Qué siente el usuario

“No estoy arrancando de cero; estoy combinando un lote nuevo con una tipología ya preparada.”

---

## Pantalla 7 — Extracción Automática del Envelope

La app analiza el site plan y muestra:

- boundary detectado
- envelope construible
- curvas
- ángulos
- lados irregulares
- área construible calculada

### Regla UX crítica

En esta pantalla **no debería haber mucho toqueteo humano**.

El comportamiento esperado es:

- en el **90%+** de los casos, el sistema detecta correctamente el envelope
- el usuario normalmente solo:
  - revisa
  - confirma
- el modo edición queda como **fallback**, no como flujo principal

### Qué puede hacer el usuario

- `Confirm Envelope`
- `Edit Only If Needed`
- `Re-run Detection`

### Qué siente el usuario

“El sistema entendió el lote real casi siempre solo. Yo intervengo solo si realmente hubo una falla.”

Eso es IMPORTANTÍSIMO para que el producto se sienta sólido.

---

## Pantalla 8 — Overlay 1:1

La app superpone:

- envelope construible
- floor plan curado
- escala 1:1

Y destaca:

- qué entra
- qué no entra
- qué walls están en conflicto
- cuánto excede

### Qué siente el usuario

“Ya entendí el problema real del lote sin medir todo a mano.”

---

## Pantalla 9 — Ajustes del Caso

Antes de correr propuestas, el usuario puede tocar solo overrides del proyecto:

- prioridad de fachada
- tolerancia de movimiento en walls flexibles
- notas del caso
- restricciones puntuales del lote

### Importante

Esto **no reabre el curado base** del floor plan salvo que el usuario lo decida explícitamente.

### Qué siente el usuario

“Estoy afinando este caso, no reconstruyendo toda la casa.”

---

## Pantalla 10 — Generar Opciones

El usuario aprieta:

- `Generate Fit Options`

La app devuelve propuestas como:

- Opción A — mínima deformación
- Opción B — mínima pérdida de área
- Opción C — máxima preservación de walls protegidas

### Qué siente el usuario

“El sistema no me encierra en una respuesta única; me da alternativas comparables.”

---

## Pantalla 11 — Comparar Propuestas

Cada propuesta muestra:

- preview sobre el lote
- walls tocadas
- tipo de cambio
- métricas:
  - area loss
  - walls moved
  - envelope violations
  - protected walls touched
  - score total

### Qué siente el usuario

“Estoy decidiendo con evidencia, no adivinando.”

---

## Pantalla 12 — Aprobar

El usuario puede:

- aprobar propuesta
- guardar draft
- volver atrás
- cambiar overrides y recalcular

### Qué siente el usuario

“El sistema propone; yo decido.”

---

## Pantalla 13 — Export

La app exporta:

- DXF ajustado
- reporte del proyecto
- snapshot de la propuesta elegida
- auditoría de cambios

### Qué siente el usuario

“Puedo pasar esto al siguiente paso del proceso sin perder trazabilidad.”

---

# Happy Path del MVP Exitoso

## Primera vez por floor plan

1. importo DXF
2. reviso walls extraídas
3. curo y publico
4. si quiero, después vuelvo y mejoro esa versión

## Cada nuevo lote

1. importo site plan
2. el sistema detecta envelope casi solo
3. confirmo
4. superpongo 1:1
5. genero opciones
6. comparo
7. apruebo
8. exporto

---

# Definición de éxito UX

Si el MVP es exitoso, el usuario siente esto:

> “Preparé esta casa una vez, puedo seguir editándola cuando quiera, y ahora cada lote nuevo lo evalúo rápido sin arrancar de cero.”

Y en site plans siente esto:

> “El sistema entiende el lote casi siempre solo; yo no tengo que corregir manualmente salvo excepciones.”

---

# Anti-objetivo UX

El producto NO debe sentirse como:

- un CAD genérico
- un editor manual pesado
- una demo de overlays
- una herramienta donde hay que corregir siempre el envelope a mano

---

# Resumen

La UX correcta del MVP tiene dos loops:

1. **Curado reusable y siempre editable**
2. **Adaptación por lote con detección automática fuerte y mínima intervención humana**

Ese es el comportamiento que hay que construir.
