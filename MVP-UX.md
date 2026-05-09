# MVP UX ? Floorplan Fit

## Objetivo del MVP

El MVP tiene que sentirse como una herramienta de **decisi?n t?cnica asistida**, no como un CAD gen?rico para redibujar planos.

La promesa UX correcta es:

> **cur?s un floor plan una vez, lo public?s como activo reutilizable, y cada site plan nuevo se convierte en un flujo corto de validaci?n, ajuste m?nimo y decisi?n auditable.**

La clave no es ?dibujar m?s?. La clave es convertir un DXF crudo en una versi?n confiable que el sistema pueda usar despu?s para adaptar el plano sin romper puertas, ventanas, fixtures, labels, dimensiones ni detalles protegidos.

---

## Principios UX

1. **Review > redraw**
   El usuario revisa, corrige y cura; no redibuja todo desde cero.

2. **CAD-faithful, no CAD completo**
   La review debe verse suficientemente fiel al DXF para tomar decisiones t?cnicas, pero no intenta reemplazar AutoCAD.

3. **Una vez curado, reutilizable**
   El curado publicado queda como versi?n can?nica activa de una tipolog?a.

4. **Siempre editable**
   Publicado **no significa congelado para siempre**.
   El usuario puede volver a abrir, mejorar el curado y republicar una nueva versi?n activa.

5. **Separar familias de artifacts**
   Una pared no es una puerta. Una puerta no es un label. Un toilet no es una wall. Una cota no es texto decorativo.
   Cada familia importante del DXF debe poder verse, seleccionarse, corregirse y persistirse con intenci?n propia.

6. **Pinches estrat?gicos > scaling global**
   El sistema no debe escalar el plano completo para hacerlo entrar.
   El usuario cura **d?nde se puede recortar** mediante pinch groups y pinch markers.

7. **1:1 real y auditable**
   Todo se visualiza en escala real estandarizada. Cuando el fit modifica algo, el usuario debe poder ver cu?nto cambi? y por qu?.

8. **Comparar opciones, no adivinar**
   El usuario debe ver propuestas comparables con m?tricas claras: qu? se toc?, cu?nto se recort?, qu? qued? protegido y qu? riesgo queda.

---

# Loop 1 ? Curado de Floor Plan

Loop 1 ocurre **una vez por tipolog?a** o cada vez que el estudio quiere mejorar una versi?n publicada.

El objetivo de Loop 1 es publicar una versi?n del floor plan que sea:

- visualmente confiable
- geom?tricamente ?til
- corregible por humanos
- reutilizable en futuros site plans
- expl?cita sobre qu? zonas se pueden recortar

---

## Pantalla 1 ? Library

El usuario abre la app y ve la librer?a de floor plans.

Cada item muestra:

- nombre
- c?digo
- versi?n activa
- estado:
  - Imported
  - Extracted
  - Curated Draft
  - Published
- fecha de ?ltima publicaci?n

### Qu? siente el usuario

> ?Estoy viendo activos reutilizables del estudio, no archivos sueltos.?

---

## Pantalla 2 ? Import Floor Plan DXF

El usuario arrastra o selecciona un DXF.

La app:

- copia el DXF al workspace gestionado
- parsea metadata real del archivo
- detecta unidades
- normaliza escala
- crea o actualiza el template/version
- deja el floor plan listo para extracci?n

### Qu? siente el usuario

> ?La app institucionaliz? este plano. Ya no estoy dependiendo de un archivo suelto en cualquier carpeta.?

---

## Pantalla 3 ? Extract CAD Artifacts

El usuario dispara la extracci?n del floor plan.

La app extrae familias separadas de artifacts:

- wall candidates
- wall thickness hints cuando se puedan inferir, por ejemplo likely 2x4 / likely 2x6
- room labels
- opening candidates: puertas y ventanas
- opening labels: modelos/tama?os como `2668`, `24"DR.`, `3050 S.H.`
- fixed plan components: toilets, tubs, cabinets, appliances, fixtures
- protected detail assemblies: detalles h?medos/protegidos, hatches o enclosures relevantes
- dimensiones CAD, cuando esa familia est? implementada

### Regla UX cr?tica

La extracci?n propone. El usuario cura.
El sistema no debe mezclar todo como ?walls?, porque eso vuelve imposible proteger y adaptar bien el plano despu?s.

### Qu? siente el usuario

> ?El sistema ley? el DXF como un plano t?cnico real, no como un dibujo plano sin significado.?

---

## Pantalla 4 ? Review CAD-faithful

La app muestra un workspace visual con:

- canvas del floor plan
- zoom con rueda
- pan con click de rueda / middle mouse
- fondo punteado tipo workspace t?cnico
- selecci?n visual directa sobre el canvas
- paneles por familia de artifact

El usuario puede revisar:

- Lines / wall candidates
- Rooms
- Openings
- Fixed Elements
- Protected Details
- Pinch Groups
- Pinch Markers
- futuras Dimensions

### Qu? puede hacer el usuario

- seleccionar una l?nea, opening, fixture o detail desde el canvas
- rechazar l?neas que no sirven
- eliminar openings falsas positivas
- eliminar labels falsas positivas
- eliminar fixed components falsos positivos
- eliminar protected details falsos positivos
- revisar labels de ambientes en su posici?n CAD-like
- revisar puertas/ventanas con su geometr?a y label real

### Qu? siente el usuario

> ?Estoy auditando el plano como t?cnico, pero sin meterme en un CAD pesado.?

---

## Pantalla 5 ? Pinch Curation

El usuario marca d?nde el futuro fit puede recortar.

La herramienta principal es el **pinch**.

Un pinch tiene:

- l?nea/candidate fuente
- posici?n sobre la l?nea
- grupo al que pertenece
- eje del grupo:
  - Width
  - Height
- m?ximo recortable

Un pinch group tiene:

- nombre humano, por ejemplo:
  - Patio
  - Garage depth
  - Bedroom side
  - Left corridor
- eje Width o Height
- conjunto de pinches relacionados

### Por qu? grupos

No alcanza con decir ?todos los pinches Height?.
El sistema necesita saber si puede recortar **el patio**, **el garage**, **un lateral espec?fico** o **un corredor**, sin tocar todos los lugares del mismo eje.

### Qu? puede hacer el usuario

- crear grupos nombrados
- agregar pinches estrat?gicos
- remover pinches equivocados
- seleccionar un grupo
- previsualizar compresi?n de ese grupo
- ver que solo se toca lo autorizado

### Qu? siente el usuario

> ?Le estoy ense?ando al sistema d?nde puede pellizcar el plano si m?s adelante no entra en un lote.?

---

## Pantalla 6 ? Preview de Compresi?n

El usuario puede arrastrar handles de preview para simular recortes.

La app debe mostrar:

- qu? grupo se est? comprimiendo
- cu?nto se est? recortando
- qu? l?neas se mueven
- qu? artifacts quedan protegidos
- qu? labels permanecen alineados
- en el futuro, qu? dimensiones cambian en tiempo real

### Regla UX cr?tica

Esto es preview de curation, no fit final.
Sirve para comprobar que los pinches est?n bien puestos antes de publicar.

### Qu? siente el usuario

> ?Si ma?ana el site plan pide achicar, ya s? que el sistema va a tocar estos lugares y no cualquier cosa.?

---

## Pantalla 7 ? Publish Curation

El usuario aprieta:

- `Publish Curation`
- `Set Active Version`

Eso genera una versi?n activa reusable que contiene:

- template/version del floor plan
- extraction run base
- artifacts visibles y curados
- rejected wall candidates
- removals/corrections de falsos positivos
- pinch groups
- pinch markers
- metadata necesaria para el fit futuro

### Importante

**Publicado no significa perfecto para siempre.**

El usuario siempre puede:

- volver a abrir el floor plan
- corregir artifacts
- sumar o sacar pinches
- mejorar grupos
- republicar una nueva versi?n activa

### Qu? siente el usuario

> ?Este plano ya est? listo para usarse en proyectos reales, pero sigo teniendo control si ma?ana quiero mejorarlo.?

---

# Loop 2 ? Adaptaci?n por Site Plan

Loop 2 ocurre **muchas veces**: una vez por cada lote/site plan nuevo donde se quiere probar una tipolog?a ya publicada.

El objetivo no es redibujar la casa.
El objetivo es responder r?pido:

- ?entra?
- si no entra, ?cu?nto falta?
- ?hay pinches autorizados suficientes?
- ?qu? propuesta toca lo m?nimo indispensable?
- ?qu? riesgo t?cnico queda?

---

## Pantalla 8 ? Nuevo Proyecto

El usuario:

- crea un proyecto
- importa un `site plan DXF`
- selecciona un `floor plan` publicado

### Qu? siente el usuario

> ?No estoy arrancando de cero; estoy combinando un lote nuevo con una tipolog?a ya preparada.?

---

## Pantalla 9 ? Extracci?n Autom?tica del Envelope

La app analiza el site plan y muestra:

- boundary detectado
- envelope construible
- setbacks relevantes
- curvas
- ?ngulos
- lados irregulares
- ?rea construible calculada

### Regla UX cr?tica

En esta pantalla **no deber?a haber mucho toqueteo humano**.

El comportamiento esperado es:

- en el **90%+** de los casos, el sistema detecta correctamente el envelope
- el usuario normalmente solo revisa y confirma
- el modo edici?n queda como fallback

### Qu? puede hacer el usuario

- `Confirm Envelope`
- `Edit Only If Needed`
- `Re-run Detection`

### Qu? siente el usuario

> ?El sistema entendi? el lote real casi siempre solo. Yo intervengo solo si realmente hubo una falla.?

---

## Pantalla 10 ? Overlay 1:1 y Diagn?stico

La app superpone:

- envelope construible
- floor plan publicado
- escala 1:1

Y destaca:

- si entra o no entra
- cu?nto excede en Width / Height u otra direcci?n relevante
- qu? zonas chocan
- qu? artifacts son protegidos
- qu? pinch groups podr?an absorber el ajuste

### Qu? siente el usuario

> ?Ya entend? el problema real del lote sin medir todo a mano.?

---

## Pantalla 11 ? Ajustes del Caso

Antes de correr propuestas, el usuario puede tocar solo overrides del proyecto:

- prioridad de fachada
- tolerancias del caso
- notas del lote
- restricciones puntuales
- preferencia de qu? pinch groups usar primero
- grupos prohibidos para este caso

### Importante

Esto **no reabre el curado base** del floor plan salvo que el usuario lo decida expl?citamente.

### Qu? siente el usuario

> ?Estoy afinando este caso, no reconstruyendo toda la casa.?

---

## Pantalla 12 ? Generate Fit Options

El usuario aprieta:

- `Generate Fit Options`

La app genera propuestas determin?sticas usando solo lo permitido por el curado publicado y los overrides del caso.

Ejemplos:

- Opci?n A ? m?nima deformaci?n total
- Opci?n B ? usar primero Patio
- Opci?n C ? preservar Garage completo
- Opci?n D ? menor impacto en habitaciones

Cada opci?n debe explicar:

- qu? pinch groups us?
- cu?nto recort? cada grupo
- qu? artifacts movi?
- qu? artifacts quedaron intactos
- si alguna dimensi?n cambi?
- si todav?a queda violaci?n de envelope

### Qu? siente el usuario

> ?El sistema no me encierra en una respuesta ?nica; me da alternativas comparables.?

---

## Pantalla 13 ? Comparar Propuestas

Cada propuesta muestra:

- preview sobre el lote
- grupos pinch usados
- total recortado
- recorte por grupo
- impacto por zona/habitaci?n cuando sea posible
- puertas/ventanas preservadas
- fixed components preservados
- protected details preservados
- dimensiones originales vs dimensiones recalculadas, cuando existan
- score total

### Qu? siente el usuario

> ?Estoy decidiendo con evidencia, no adivinando.?

---

## Pantalla 14 ? Aprobar

El usuario puede:

- aprobar propuesta
- guardar draft
- volver atr?s
- cambiar overrides y recalcular
- decidir que no entra de forma aceptable

### Qu? siente el usuario

> ?El sistema propone; yo decido.?

---

## Pantalla 15 ? Export

La app exporta:

- DXF ajustado
- reporte del proyecto
- snapshot de la propuesta elegida
- auditor?a de cambios
- lista de pinch groups usados
- m?tricas finales

### Qu? siente el usuario

> ?Puedo pasar esto al siguiente paso del proceso sin perder trazabilidad.?

---

# Happy Path del MVP exitoso

## Primera vez por floor plan

1. importo DXF
2. corro extracci?n de artifacts CAD
3. reviso visualmente el plano
4. elimino falsos positivos
5. creo grupos de pinches
6. pongo pinches estrat?gicos
7. previsualizo compresi?n
8. publico versi?n activa
9. si quiero, despu?s vuelvo y mejoro esa versi?n

## Cada nuevo lote

1. importo site plan
2. el sistema detecta envelope casi solo
3. confirmo envelope
4. selecciono floor plan publicado
5. superpongo 1:1
6. diagnostico si entra o no
7. genero opciones usando pinches permitidos
8. comparo propuestas
9. apruebo o rechazo
10. exporto

---

# Definici?n de ?xito UX

Si el MVP es exitoso, el usuario siente esto:

> ?Prepar? esta casa una vez, s? exactamente d?nde se puede adaptar, y ahora cada lote nuevo lo eval?o r?pido sin arrancar de cero.?

Y en site plans siente esto:

> ?El sistema entiende el lote casi siempre solo; si el plano no entra, intenta tocar ?nicamente las zonas que yo autoric?.?

---

# Anti-objetivo UX

El producto NO debe sentirse como:

- un CAD gen?rico
- un editor manual pesado
- una demo de overlays
- una herramienta que escala todo el plano sin criterio
- una herramienta donde puertas, ventanas, fixtures o cotas se rompen sin explicaci?n
- una herramienta donde hay que corregir siempre el envelope a mano

---

# Resumen

La UX correcta del MVP tiene dos loops:

1. **Curado CAD-faithful reusable y siempre editable**
2. **Adaptaci?n por lote con envelope autom?tico, pinches autorizados y m?nima intervenci?n humana**

Ese es el comportamiento que hay que construir.
