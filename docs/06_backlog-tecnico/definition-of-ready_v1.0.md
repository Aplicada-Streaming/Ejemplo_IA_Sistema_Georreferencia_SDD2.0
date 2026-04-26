**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** definition-of-ready_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# Definition of Ready (DoR)

Una User Story está **lista para entrar a un sprint** cuando cumple **todos** los criterios de este documento. El criterio se aplica al final del refinamiento y antes del Sprint Planning. Si una US no cumple el DoR, queda fuera del sprint.

Este documento extiende el DoR de alto nivel definido en [acuerdo-equipo](../00_contexto/acuerdo-equipo_v1.0.md) con criterios operativos.

---

## 1. Checklist DoR

### 1.1. Trazabilidad

- [ ] La US tiene un identificador `US-XX`.
- [ ] La US declara una **épica de pertenencia** del [roadmap](../00_contexto/roadmap-producto_v1.0.md).
- [ ] La US referencia uno o más **CUs** y/o **RNs** que cubre.
- [ ] Si toca un módulo crítico (Sync, Storage, Identity), el ADR aplicable está leído por el equipo.

### 1.2. Especificación

- [ ] Está escrita con el formato "Como [rol], quiero [acción], para [beneficio]".
- [ ] Tiene **criterios de aceptación** verificables en formato Given/When/Then o equivalente.
- [ ] El alcance de la US es claro: qué incluye y qué deja para otra US.
- [ ] La US tiene **estimación en story points** asignada por el equipo.

### 1.3. Dependencias

- [ ] Las dependencias con otras US o BTs están identificadas y resueltas (la dependencia ya completada o planeada en sprints previos).
- [ ] No depende de información marcada `[REQUIERE_INFO]` que sea bloqueante. Si depende de información secundaria, hay un supuesto documentado.
- [ ] Si requiere capacidades de infraestructura (storage, CI), las BTs correspondientes están planificadas.

### 1.4. Capas y rol

- [ ] Si la US toca **móvil + web + backend**, las tres capas caben en un sprint o la US está dividida.
- [ ] Si la US toca **plantillas**, los puntos de extensión están señalados.
- [ ] Si la US toca **sincronización**, el equipo confirma que el spike de sync ya se ejecutó (BT-07).
- [ ] Si la US toca **storage**, el adaptador objetivo está especificado.
- [ ] Si la US toca **roles o permisos**, la matriz de pruebas afectada está identificada.

### 1.5. Criterios de calidad

- [ ] Hay un **plan de pruebas** mínimo (qué tipos de tests aplican: unit, integration, e2e, manual).
- [ ] La US identifica si requiere **prueba con dos dispositivos** (criterio del DoD para US de sync).
- [ ] La US identifica si requiere **prueba con dos plantillas distintas** (criterio del DoD para US de plantillas).

### 1.6. Validación de PO

- [ ] El **PO confirma valor de negocio** y prioridad relativa.
- [ ] La US está priorizada en MoSCoW (Must / Should / Could / Won't).

---

## 2. Reglas operativas

### 2.1. Tamaño máximo

Una US debe poder **completarse en un sprint** del equipo. Si una US estimada supera 13 SP o el sprint planeado, debe **dividirse** antes de entrar.

### 2.2. División de US

Cuando una US se divide, las US resultantes:
- Conservan el código (US-15 → US-15.A, US-15.B) o reciben códigos nuevos consecutivos.
- Cada una sigue cumpliendo el DoR independientemente.
- La trazabilidad a CU/NB se mantiene en cada parte.

### 2.3. Bloqueo durante el sprint

Si durante el sprint una US deja de cumplir el DoR (por información que aparece, dependencia que se cae), se **escala al PO** y al Tech Lead. Posibles caminos:
- Bajarla del sprint si todavía es temprano.
- Mantenerla y resolver el bloqueo con buffer.
- Convertirla en un spike timeboxed.

---

## 3. Reglas para tareas técnicas (BT-XX)

Las BTs no son US pero también pasan por un check antes de entrar al sprint:

- [ ] Justificación clara: por qué es necesaria ahora y no después.
- [ ] Criterio de "hecho" verificable (output concreto: "el script `.bat` levanta los 4 procesos").
- [ ] Estimación.
- [ ] Dependencia con otras BTs / US identificada.

---

## 4. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| [acuerdo-equipo](../00_contexto/acuerdo-equipo_v1.0.md) Sec. 4 | DoR de alto nivel del que este documento extiende |
| [roadmap-producto](../00_contexto/roadmap-producto_v1.0.md) | Épicas a las que las US se atan |

---

**Fin del documento — definition-of-ready_v1.0.md**
