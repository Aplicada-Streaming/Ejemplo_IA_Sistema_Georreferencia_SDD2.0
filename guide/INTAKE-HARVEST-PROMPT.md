# INTAKE-HARVEST-PROMPT — Generador de Documentos de Intake desde Conversación

**Archivo:** INTAKE-HARVEST-PROMPT.md
**Versión:** 1.0
**Fecha:** 2026-04-23
**Uso:** Pegar en cualquier chat de Claude al finalizar la conversación de relevamiento con el cliente.

---

## Cómo usar este prompt

1. Tener una conversación libre con Claude sobre el proyecto: describir el cliente,
   el problema, las ideas, subir capturas de pantalla, evaluar alternativas, tomar decisiones.
   No hace falta ninguna estructura — hablar con naturalidad.
2. Cuando la conversación esté suficientemente completa, copiar el bloque de la
   Sección 1 y pegarlo como último mensaje del chat.
3. Claude va a cosechar todo lo discutido y generar los dos documentos listos para guardar.
4. Guardar los outputs como `devs/intake/PROJECT-README.md` y `devs/intake/PROJECT-BRIEF.md`
   en el repositorio del proyecto.
5. Revisar los campos `[REQUIERE_INFO]` y completarlos antes de ejecutar el orquestador.

---

## Sección 1 — El prompt (copiar y pegar completo)

---

Basándote en TODO lo que conversamos en este chat hasta ahora — incluyendo
descripciones, capturas de pantalla, alternativas evaluadas, decisiones tomadas
y cualquier información que haya surgido — generá los dos documentos de intake
de la metodología SDD.

## Instrucciones generales

- Extraé la información del historial de esta conversación.
- Lo que se mencionó pero no se formalizó: inferilo y documentalo.
- Lo que genuinamente no se discutió: marcalo como [REQUIERE_INFO].
- No inventés información que no surgió en la conversación.
- Si algo fue discutido con alternativas (evaluamos A vs B y elegimos B),
  documentá la decisión tomada y la alternativa descartada con su razón.
- Si hay capturas de pantalla que compartí, describí su contenido y
  referencialas en la sección correspondiente.

## Documento 1 — PROJECT-README.md

Generá el documento completo con esta estructura:

---
# PROJECT-README — Relevamiento del Cliente
**Proyecto:** [extraer del contexto]
**Fecha:** [hoy]
**Estado:** Borrador

## 1. Identificación del proyecto
[nombre, descripción en una línea]

## 2. El cliente
[nombre/empresa, industria, contexto del negocio, stakeholders identificados
en tabla: Nombre/Rol | Responsabilidad | Nivel de decisión]

## 3. El problema actual
[problema que tiene hoy, cómo lo resuelve actualmente, por qué ya no alcanza]

## 4. Qué se quiere construir
[descripción del sistema, tabla de actores y qué hace cada uno en el sistema]

## 5. Requerimientos funcionales
[lista RF-01, RF-02... extraída de todo lo discutido]

## 6. Requerimientos no funcionales
[lista RNF-01, RNF-02... rendimiento, seguridad, disponibilidad, etc.]

## 7. Restricciones conocidas
[stack, plataformas, tiempo, presupuesto, equipo, integraciones obligatorias]

## 8. Definición de éxito
[cómo sabe el cliente que el proyecto fue exitoso, métricas si las hay]

## 9. Notas adicionales
[decisiones previas, riesgos mencionados, acuerdos implícitos, contexto
político o de equipo que surgió en la conversación]
---

## Documento 2 — PROJECT-BRIEF.md

Generá el documento completo con esta estructura:

---
# PROJECT-BRIEF — Delineamientos Técnicos y Metodológicos
**Proyecto:** [mismo que README]
**Fecha:** [hoy]
**Estado:** Borrador

## 1. Arquitectura propuesta o existente
[tipo de arquitectura elegida o evaluada, tabla de componentes/servicios
con: Componente | Responsabilidad | Tecnología | Nuevo o existente,
tabla de microservicios/servicios externos si los hay]

## 2. Capturas de pantalla de referencia
[tabla SS-01, SS-02... con descripción de cada captura compartida en
la conversación y las notas relevantes que surgieron sobre ella]

## 3. Esquema de aplicaciones
[si es standalone o ecosistema, mapa de apps involucradas,
tabla de flujos de datos principales]

## 4. Decisiones de diseño ya tomadas
[tabla DD-01, DD-02... con todas las decisiones que se tomaron durante
la conversación: la decisión, su justificación y su impacto en diseño.
Incluir alternativas descartadas con su razón.]

## 5. Metodología de ejecución
[metodología elegida (Scrum/Kanban/otra), estrategia de entregables
(vertical slicing / horizontal slicing) con justificación,
velocidad estimada si se discutió, cantidad de sprints estimados]

## 6. Infraestructura y deploy
[ambiente de destino, estrategia de deploy, ambientes requeridos,
estrategia de versionado]

## 7. Restricciones técnicas adicionales
[seguridad, performance, compatibilidad que surgieron en la conversación]

## 8. Referencias y assets
[repositorio si se mencionó, documentación existente referenciada,
lista de capturas con su ruta sugerida en /devs/assets/screenshots/]
---

## Formato de entrega

Generá cada documento en un bloque de código markdown separado, listo
para guardar como archivo. Primero PROJECT-README.md, luego PROJECT-BRIEF.md.

Al final, agregá una sección corta:

**CAMPOS [REQUIERE_INFO]:** lista de todos los campos marcados con su ubicación
exacta (Documento > Sección > Campo) para que sepa qué completar antes de
ejecutar el orquestador.

**DECISIONES IMPLÍCITAS DOCUMENTADAS:** lista de inferencias que hiciste
a partir de la conversación para que pueda confirmarlas o corregirlas.

---

**Fin del prompt**
