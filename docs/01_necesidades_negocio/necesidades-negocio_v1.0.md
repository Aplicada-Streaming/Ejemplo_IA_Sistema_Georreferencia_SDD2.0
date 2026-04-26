**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** necesidades-negocio_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-01 via orquestador

---

# Necesidades de Negocio — Resumen Ejecutivo

Este documento consolida las necesidades de negocio identificadas a partir del análisis de los requerimientos del intake. Cada necesidad agrupa requerimientos funcionales que responden al mismo problema de negocio. La trazabilidad **NB → CU → US → Sprint** se completa downstream cuando SA-02 produzca los Casos de Uso.

---

## 1. Mapa de stakeholders y necesidades

```
                    ┌───────────────────────────────┐
                    │   Sponsor / Referente cliente │
                    │   (Vialidad)                  │
                    └──────────────┬────────────────┘
                                   │
            ┌──────────────────────┼─────────────────────┐
            │                      │                     │
            ▼                      ▼                     ▼
   ┌────────────────┐    ┌──────────────────┐  ┌──────────────────┐
   │  Admin raíz    │    │  Jefe de área    │  │   Relevador      │
   │  (sistema)     │    │  (supervisor)    │  │   (campo)        │
   └────────────────┘    └──────────────────┘  └──────────────────┘
       │                      │                       │
       │ NB-11                │ NB-04 NB-06 NB-07     │ NB-01 NB-02
       │                      │ NB-08 NB-09 NB-10     │ NB-03 NB-05
       │                      │                       │
       └─── todas pisan en ───┴──── el sistema ───────┘
```

---

## 2. Listado de necesidades de negocio

| ID | Necesidad de negocio | Stakeholder principal | Prioridad | RFs cubiertos |
|---|---|---|---|---|
| [NB-01](necesidades-de-negocio/NB-01-captura-georreferenciada-en-campo_v1.0.md) | Captura georreferenciada eficiente en campo con conectividad limitada | Relevador, Jefe de área | Crítica | RF-14 a RF-23, RNF-01 |
| [NB-02](necesidades-de-negocio/NB-02-trabajo-offline-y-colaborativo_v1.0.md) | Trabajo offline-first y colaborativo en una misma campaña | Relevador, Colaborador, Jefe de área | Crítica | RF-34 a RF-43, RNF-01, RNF-02 |
| [NB-03](necesidades-de-negocio/NB-03-tipos-diversos-de-inspeccion-sin-codigo_v1.0.md) | Soporte de tipos diversos de inspección sin tocar código | Cliente (Vialidad como organización), Jefe de área | Alta | RF-07 a RF-13, RNF-07 |
| [NB-04](necesidades-de-negocio/NB-04-gestion-ciclo-vida-relevamiento_v1.0.md) | Gestión del ciclo de vida del relevamiento | Relevador (dueño), Jefe de área | Alta | RF-01 a RF-06 |
| [NB-05](necesidades-de-negocio/NB-05-onboarding-relevamientos-previos_v1.0.md) | Incorporación de relevamientos hechos sin la app móvil | Jefe de área, Relevador | Media | RF-24 a RF-28 |
| [NB-06](necesidades-de-negocio/NB-06-revision-y-consolidacion-en-gabinete_v1.0.md) | Revisión y consolidación de la información en gabinete | Jefe de área, Relevador, Gabinete (futuro) | Alta | RF-29 a RF-33 |
| [NB-07](necesidades-de-negocio/NB-07-resolucion-colaborativa-de-duplicados_v1.0.md) | Resolución colaborativa de puntos duplicados | Jefe de área, Relevador (dueño) | Alta | RF-44 a RF-48 |
| [NB-08](necesidades-de-negocio/NB-08-trazabilidad-tecnica-de-cambios_v1.0.md) | Trazabilidad técnica de cambios sobre los datos | Jefe de área, Equipo técnico | Alta | RF-49 a RF-51, RNF-03 |
| [NB-09](necesidades-de-negocio/NB-09-visibilidad-de-actividad-colaborativa_v1.0.md) | Visibilidad de la actividad de cada colaborador en la campaña | Relevador, Jefe de área | Media | RF-52 a RF-54 |
| [NB-10](necesidades-de-negocio/NB-10-gestion-jerarquica-de-usuarios_v1.0.md) | Gestión jerárquica de usuarios y permisos finos | Admin raíz, Jefe de área | Alta | RF-55 a RF-59 |
| [NB-11](necesidades-de-negocio/NB-11-portabilidad-storage-y-config-inicial_v1.0.md) | Configuración operativa del sistema portable entre proveedores de almacenamiento | Admin raíz | Alta | RF-60 a RF-62, RNF-04 |

---

## 3. Cobertura de RFs y RNFs del intake

| Origen | Total | Cubiertos | Sin NB |
|---|---|---|---|
| RF-01 a RF-62 | 62 | 62 | 0 |
| RNF-01 a RNF-08 (los aplicables a NB de negocio) | 8 | 4 (RNF-01, 02, 03, 04, 07) | RNF-05, RNF-06, RNF-08 son técnicos |

> RNF-05 (portabilidad de frontend), RNF-06 (levantamiento local), RNF-08 (autenticación JWT) son requisitos técnicos puros que no generan NB de negocio. Son insumo directo de SA-05 (Arquitectura).
> RNF-09 a RNF-12 están marcados `[REQUIERE_INFO]` en el intake y no afectan NBs todavía.

---

## 4. Priorización general

La priorización surge del valor de negocio relativo, no del orden de implementación. El orden de implementación lo define el [roadmap](../00_contexto/roadmap-producto_v1.0.md).

| Prioridad | NBs |
|---|---|
| **Crítica** — sin esto el sistema no resuelve el problema central | NB-01, NB-02 |
| **Alta** — sin esto el sistema sirve pero no opera de forma sostenible | NB-03, NB-04, NB-06, NB-07, NB-08, NB-10, NB-11 |
| **Media** — agrega valor pero el MVP funciona sin esto | NB-05, NB-09 |

---

## 5. Dependencias entre necesidades

```
NB-02 (offline + colaborativo) ───────┐
   ├─→ NB-01 (captura)                │
   ├─→ NB-04 (ciclo vida)             │
   ├─→ NB-07 (duplicados) ←───────────┤
   └─→ NB-08 (trazabilidad) ←─────────┤
                                      │
NB-03 (plantillas) ───→ NB-01, NB-04, NB-06
NB-10 (usuarios) ─────→ NB-04, NB-06 (permisos por punto)
NB-11 (storage)  ─────→ NB-01 (donde van las fotos)
NB-05 (carga web) ────→ NB-06 (la web también consume el storage)
NB-09 (visualización) ←── NB-02 (ver puntos de otros colaboradores)
```

---

## 6. Métricas agregadas de éxito de las NBs

Las métricas SMART globales del producto se documentan en [vision-producto](../00_contexto/vision-producto_v1.0.md) Sección 4. Cada NB declara su métrica específica, alineada con esas métricas globales.

| Métrica global | NBs que contribuyen |
|---|---|
| MET-01 Tiempo entre captura y dato disponible | NB-01, NB-02, NB-06 |
| MET-02 Cobertura del proceso digital | NB-01, NB-04, NB-05 |
| MET-03 Adopción efectiva por relevador | NB-01, NB-02, NB-09 |
| MET-04 Tiempo de incorporación de plantilla nueva | NB-03 |
| MET-05 Tasa de pérdida de datos en sync | NB-02, NB-08 |
| MET-06 Tiempo de resolución de candidatos a fusión | NB-07 |

---

## 7. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| `devs/intake/PROJECT-README.md` Sec. 3 | Problema actual y dolor del proceso manual |
| `devs/intake/PROJECT-README.md` Sec. 5 | Lista de RFs agrupados por temática |
| `devs/intake/PROJECT-README.md` Sec. 6 | RNFs aplicables a NBs |
| [vision-producto](../00_contexto/vision-producto_v1.0.md) | Audiencia y métricas de éxito |
| [alcance-proyecto](../00_contexto/alcance-proyecto_v1.0.md) | Funcionalidades incluidas en MVP |

---

**Fin del documento — necesidades-negocio_v1.0.md**
