**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** CU-11-resolver-candidato-fusion_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# CU-11 — Resolver candidato a fusión de puntos cercanos

**Código:** CU-11
**Actor primario:** Jefe de área o Relevador (dueño del relevamiento)
**Frente:** Web

## Precondiciones

- El backend detectó al menos un par de Puntos como CandidatoAFusión durante la sincronización (regla [RN-09](../reglas-de-negocio/RN-09-deteccion-candidatos-fusion_v1.0.md)).
- El usuario tiene permiso (jefe del área del relevamiento o dueño).

## Postcondiciones

- El CandidatoAFusión queda resuelto en estado `fusionado` o `mantenido_separado`.
- Si fusionado: existe un Punto consolidado y un evento `merged` con los valores antes/después; los Puntos originales quedan referenciados en el evento.
- Si mantenido separado: el par no se vuelve a proponer.

## Flujo principal — Fusionar

1. El usuario abre el panel de conflictos pendientes.
2. Selecciona la sección "Candidatos a fusión".
3. El sistema lista los pares pendientes con: distancia, diferencia temporal, autores, relevamiento.
4. El usuario abre uno y el sistema muestra:
   - **Mapa** con ambos puntos resaltados, líneas indicando cercanía y distancia exacta.
   - **Listado lado a lado de fotos** de cada punto.
   - **Comparación de campos**: cada campo donde haya divergencia con sus valores y un selector para elegir el valor final.
5. El usuario elige **Fusionar**.
6. El sistema le pide:
   - **Posición resultante**: centroide, A o B.
   - **Valor final por campo divergente**.
7. El usuario confirma.
8. El sistema:
   - Crea/mantiene un Punto consolidado.
   - Une todas las Fotos en su catálogo.
   - Registra evento `merged` con quién, cuándo, valores antes/después, ubicación final.
   - Marca el CandidatoAFusión como `fusionado`.

## Flujo alternativo — Mantener separados

1a. El usuario elige **Mantener separados**.
2a. El sistema marca el par como `mantenido_separado`.
3a. El par no se vuelve a proponer en futuros chequeos.

## Flujos de error

- E1. Cambio de plantilla entre detección y resolución → el sistema usa la versión de plantilla del relevamiento al momento del merge.
- E2. Uno de los puntos fue eliminado lógicamente entre detección y resolución → el sistema cancela el candidato.

## Reglas de negocio relacionadas

- [RN-09](../reglas-de-negocio/RN-09-deteccion-candidatos-fusion_v1.0.md), [RN-10](../reglas-de-negocio/RN-10-eventos-append-only_v1.0.md).

## Trazabilidad

- Origen: [NB-07](../../01_necesidades_negocio/necesidades-de-negocio/NB-07-resolucion-colaborativa-de-duplicados_v1.0.md).
- RFs cubiertos: RF-44, RF-45, RF-46, RF-47, RF-48.

## Criterios de aceptación

- **CA-11.1** — *Given* dos Puntos del mismo relevamiento creados por colaboradores distintos a 7m de distancia y 2h de diferencia, con threshold radio=10m y ventana=24h, *when* sincronizan, *then* se crea un CandidatoAFusión `pendiente`.
- **CA-11.2** — *Given* un candidato pendiente, *when* el jefe revisa y elige Fusionar con centroide, *then* se crea un Punto en la coordenada media, las fotos se unifican, y el evento `merged` registra los valores antes y después.
- **CA-11.3** — *Given* un candidato pendiente, *when* el jefe elige Mantener separados, *then* el par queda marcado y no aparece en chequeos futuros.
- **CA-11.4** — *Given* dos Puntos creados por **el mismo colaborador**, *when* sincronizan, *then* **no** se crea CandidatoAFusión (la regla exige distintos colaboradores).
- **CA-11.5** — *Given* la fusión completada, *when* se consulta el log de eventos del Punto consolidado, *then* aparece el evento `merged` con la historia completa.

---

**Fin del documento — CU-11-resolver-candidato-fusion_v1.0.md**
