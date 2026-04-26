**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** especificacion-funcional_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# Especificación Funcional — Resumen

Este documento es la entrada principal de la especificación funcional del sistema. Detalla los actores, los casos de uso, las reglas de negocio y el modelo de datos conceptual. Es la fuente de verdad para los subagentes downstream (UX/UI, Arquitectura, Backlog).

---

## 1. Actores del sistema

| Actor | Tipo | Frente principal | Descripción operativa |
|---|---|---|---|
| Admin raíz | Humano | Web | Único actor con privilegios sobre la configuración del sistema (storage) y sobre los jefes de área. Existe desde la primera ejecución; no se registra. |
| Jefe de área | Humano | Web | Supervisa los relevamientos y relevadores de su área. Acepta a los relevadores que se registran. Asigna colaboradores. Revisa y resuelve conflictos. Edita relevamientos según necesidad. |
| Relevador (dueño) | Humano | Móvil + Web | Crea, edita, abre, cierra y elimina sus propios relevamientos. Captura puntos en campo. Recibe alertas de conflictos. |
| Colaborador asignado | Humano | Móvil + Web | Trabaja en relevamientos ajenos a los que fue asignado. Crea puntos nuevos y edita los suyos. **No** puede editar puntos creados por otros, ni eliminar el relevamiento. |
| Sistema (workers) | No humano | Backend | Procesa imágenes, drena outbox de sincronización, detecta candidatos a fusión. No es invocado directamente; reacciona a eventos del backend. |

> Los flujos donde "Sistema" interviene como actor secundario están documentados en cada CU dentro del flujo principal o alternativo correspondiente.

---

## 2. Listado de Casos de Uso

| ID | Nombre | NB origen | Frente | Actor primario | Prioridad |
|---|---|---|---|---|---|
| [CU-01](casos-de-uso/CU-01-iniciar-sesion-y-registro-jerarquico_v1.0.md) | Iniciar sesión y registrar usuario con aceptación jerárquica | NB-10 | Web + Móvil | Admin raíz / Jefe / Relevador | Alta |
| [CU-02](casos-de-uso/CU-02-configurar-storage_v1.0.md) | Configurar storage (primer arranque y reconfiguración) | NB-11 | Web | Admin raíz | Alta |
| [CU-03](casos-de-uso/CU-03-crear-versionar-plantilla_v1.0.md) | Crear y versionar plantilla con herencia | NB-03 | Web | Jefe de área | Alta |
| [CU-04](casos-de-uso/CU-04-crear-relevamiento_v1.0.md) | Crear relevamiento | NB-04 | Web + Móvil | Relevador / Jefe | Crítica |
| [CU-05](casos-de-uso/CU-05-asignar-colaboradores-y-ciclo-vida_v1.0.md) | Asignar colaboradores y gestionar ciclo de vida del relevamiento | NB-04, NB-10 | Web + Móvil | Relevador (dueño) / Jefe | Alta |
| [CU-06](casos-de-uso/CU-06-capturar-punto-georreferenciado_v1.0.md) | Capturar punto georreferenciado (modo detenido y móvil) | NB-01 | Móvil | Relevador / Colaborador | Crítica |
| [CU-07](casos-de-uso/CU-07-editar-catalogo-punto-desde-movil_v1.0.md) | Editar catálogo de punto desde móvil | NB-01 | Móvil | Relevador / Colaborador | Alta |
| [CU-08](casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md) | Sincronizar relevamiento bidireccional con resolución de conflictos | NB-02 | Móvil + Backend | Relevador / Sistema | Crítica |
| [CU-09](casos-de-uso/CU-09-cargar-lote-fotos-web_v1.0.md) | Cargar lote de fotos previas desde web (EXIF + manual) | NB-05 | Web | Jefe / Relevador | Media |
| [CU-10](casos-de-uso/CU-10-revisar-y-editar-relevamiento-web_v1.0.md) | Revisar y editar relevamiento desde web | NB-06, NB-09 | Web | Jefe / Relevador / Colaborador | Alta |
| [CU-11](casos-de-uso/CU-11-resolver-candidato-fusion_v1.0.md) | Resolver candidato a fusión de puntos cercanos | NB-07 | Web | Jefe / Relevador (dueño) | Alta |
| [CU-12](casos-de-uso/CU-12-consultar-trazabilidad-punto_v1.0.md) | Consultar trazabilidad histórica de un punto | NB-08 | Web | Jefe / Relevador / Colaborador | Media |

---

## 3. Listado de Reglas de Negocio

| ID | Nombre | Origen | Prioridad |
|---|---|---|---|
| [RN-01](reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md) | Permisos por punto: dueño edita todo, colaborador solo lo suyo | NB-10, DD-13 | Alta |
| [RN-02](reglas-de-negocio/RN-02-restricciones-eliminacion-relevamiento_v1.0.md) | Restricciones de eliminación del relevamiento | NB-04 | Alta |
| [RN-03](reglas-de-negocio/RN-03-plantilla-raiz-inmutable_v1.0.md) | Plantilla genérica raíz inmutable y no eliminable | NB-03, DD-05 | Alta |
| [RN-04](reglas-de-negocio/RN-04-restricciones-herencia-plantillas_v1.0.md) | Restricciones de herencia de plantillas | NB-03, DD-06 | Alta |
| [RN-05](reglas-de-negocio/RN-05-inmutabilidad-plantilla-publicada_v1.0.md) | Inmutabilidad de plantilla publicada (versionado) | NB-03 | Alta |
| [RN-06](reglas-de-negocio/RN-06-guids-cliente-idempotencia_v1.0.md) | GUIDs en cliente e idempotencia de operaciones | NB-02, DD-08 | Alta |
| [RN-07](reglas-de-negocio/RN-07-resolucion-conflictos-lww-y-precedencia_v1.0.md) | Resolución LWW por campo + precedencia del dueño | NB-02, DD-11 | Alta |
| [RN-08](reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md) | Capturas post-cierre del relevamiento | NB-02, NB-04 | Alta |
| [RN-09](reglas-de-negocio/RN-09-deteccion-candidatos-fusion_v1.0.md) | Detección de candidatos a fusión | NB-07, DD-21 | Alta |
| [RN-10](reglas-de-negocio/RN-10-eventos-append-only_v1.0.md) | Eventos del log son append-only e inmutables | NB-08, DD-12 | Alta |
| [RN-11](reglas-de-negocio/RN-11-aceptacion-jerarquica-y-movil-restringido_v1.0.md) | Aceptación jerárquica de usuarios y móvil restringido a relevadores | NB-10 | Alta |
| [RN-12](reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md) | Storage: datos previos siguen en su adaptador original | NB-11 | Media |

---

## 4. Modelo de datos conceptual

Documento separado: [modelo-datos-conceptual_v1.0.md](modelo-datos-conceptual_v1.0.md).

---

## 5. Flujo general del sistema

```
                                     ┌──────────────────────────────┐
[Primera ejecución]                  │ Admin raíz: Wizard storage   │ CU-02
                                     └──────────────┬───────────────┘
                                                    │
                                                    ▼
                                     ┌──────────────────────────────┐
[Onboarding]                         │ Jefe se registra → Admin     │ CU-01
                                     │ acepta. Relevador se registra│
                                     │ → Jefe acepta.               │
                                     └──────────────┬───────────────┘
                                                    │
                                                    ▼
                                     ┌──────────────────────────────┐
[Configuración funcional]            │ Jefe crea plantilla hija     │ CU-03
                                     └──────────────┬───────────────┘
                                                    │
                                                    ▼
                                     ┌──────────────────────────────┐
[Inicio de campaña]                  │ Relevador crea relevamiento  │ CU-04
                                     │ Asigna colaboradores         │ CU-05
                                     └──────────────┬───────────────┘
                                                    │
                            ┌───────────────────────┼─────────────────┐
                            ▼                       ▼                 ▼
                    [Móvil — campo]          [Móvil — campo]    [Web — gabinete]
                    Capturar punto           Capturar punto     Cargar lote fotos
                    Editar catálogo          Editar catálogo    CU-09
                    CU-06, CU-07             CU-06, CU-07
                            │                       │                 │
                            └───────────┬───────────┘                 │
                                        ▼                             │
                            ┌──────────────────────────────┐         │
                            │ Sync bidireccional           │ CU-08   │
                            │ + LWW + detección fusión     │         │
                            └──────────────┬───────────────┘         │
                                           │                         │
                                           ├─────────────────────────┘
                                           ▼
                            ┌──────────────────────────────┐
                            │ Web: revisión + edición      │ CU-10
                            │ + resolver candidato fusión  │ CU-11
                            │ + consultar trazabilidad     │ CU-12
                            │ + resolver conflictos panel  │ (parte de CU-08)
                            └──────────────────────────────┘
```

---

## 6. Cobertura de NBs por CUs

| NB | CUs que la resuelven |
|---|---|
| NB-01 | CU-06, CU-07 |
| NB-02 | CU-08 |
| NB-03 | CU-03 |
| NB-04 | CU-04, CU-05 |
| NB-05 | CU-09 |
| NB-06 | CU-10 |
| NB-07 | CU-11 |
| NB-08 | CU-12 |
| NB-09 | CU-10 (vista de mapa colaborativo) |
| NB-10 | CU-01, CU-05 |
| NB-11 | CU-02 |

Cada NB tiene al menos un CU que la resuelve. La cobertura inversa (cada CU traza a una NB) está garantizada por las trazabilidades en cada archivo CU.

---

## 7. Cobertura de RFs por CUs

| RF | CU que lo cubre |
|---|---|
| RF-01 a RF-06 | CU-04, CU-05 |
| RF-07 a RF-13 | CU-03 |
| RF-14 a RF-23 | CU-06, CU-07 |
| RF-24 a RF-28 | CU-09 |
| RF-29 a RF-33 | CU-10 |
| RF-34 a RF-43 | CU-08, CU-11 (panel parte de CU-08) |
| RF-44 a RF-48 | CU-11 |
| RF-49 a RF-51 | CU-12 |
| RF-52 a RF-54 | CU-10 |
| RF-55 a RF-59 | CU-01, CU-05 |
| RF-60 a RF-62 | CU-02 |

---

## 8. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| [necesidades-negocio](../01_necesidades_negocio/necesidades-negocio_v1.0.md) | NBs que cada CU debe resolver |
| `devs/intake/PROJECT-README.md` Sec. 4.3 | Definición de actores |
| `devs/intake/PROJECT-README.md` Sec. 5 | Cobertura de RFs |
| `devs/intake/PROJECT-BRIEF.md` Sec. 4 | DDs (decisiones de diseño) que originan reglas de negocio |

## 9. Documentos relacionados (esta sección)

- [Modelo de datos conceptual](modelo-datos-conceptual_v1.0.md)
- Casos de uso: ver tabla en Sección 2.
- Reglas de negocio: ver tabla en Sección 3.

---

**Fin del documento — especificacion-funcional_v1.0.md**
