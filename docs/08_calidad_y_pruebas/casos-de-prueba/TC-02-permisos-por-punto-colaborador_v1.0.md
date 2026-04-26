**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** TC-02-permisos-por-punto-colaborador_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-08 via orquestador

---

# TC-02 — Permisos por punto: colaborador no edita lo de otros

**ID:** TC-02
**CU relacionado:** [CU-07](../../02_especificacion_funcional/casos-de-uso/CU-07-editar-catalogo-punto-desde-movil_v1.0.md), [CU-10](../../02_especificacion_funcional/casos-de-uso/CU-10-revisar-y-editar-relevamiento-web_v1.0.md)
**RN aplicada:** [RN-01](../../02_especificacion_funcional/reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md)
**Tipo:** Integración (backend) + E2E (frontends)
**Prioridad:** Crítica

## Precondiciones

- Relevamiento "Test Permisos" del que `relevador.duenio@vialidad` es dueño.
- `colaborador.uno@vialidad` y `colaborador.dos@vialidad` están asignados como colaboradores.
- Existe Punto P-A creado por `colaborador.uno`.
- Existe Punto P-B creado por `colaborador.dos`.
- Existe Punto P-D creado por el dueño.

## Datos de entrada

| Caso | Actor | Acción | Punto |
|---|---|---|---|
| 1 | colaborador.uno | Editar título | P-A (suyo) |
| 2 | colaborador.uno | Editar título | P-B (otro colaborador) |
| 3 | colaborador.uno | Editar título | P-D (dueño) |
| 4 | dueño | Editar título | P-A (de colaborador) |
| 5 | colaborador.uno | Eliminar relevamiento | — |

## Pasos y resultados esperados

| Caso | Pasos | Resultado esperado |
|---|---|---|
| 1 | colaborador.uno hace `PATCH /points/P-A` con nuevo título | 200 OK; evento `field_updated` registrado |
| 2 | colaborador.uno hace `PATCH /points/P-B` con nuevo título | 403 Forbidden; sin evento; UI muestra modo lectura |
| 3 | colaborador.uno hace `PATCH /points/P-D` con nuevo título | 403 Forbidden; sin evento |
| 4 | dueño hace `PATCH /points/P-A` con nuevo título | 200 OK; evento `field_updated` registrado |
| 5 | colaborador.uno hace `DELETE /surveys/Test Permisos` | 403 Forbidden ([RN-02](../../02_especificacion_funcional/reglas-de-negocio/RN-02-restricciones-eliminacion-relevamiento_v1.0.md)) |

## Resultado obtenido

(Se completa al ejecutar.)

## Estado

Pendiente.

## Notas

- Equivalente al test obligatorio de la **matriz de permisos** del DoD para US-14.
- Si el frontend móvil del colaborador.uno abre P-B, debe mostrar modo lectura sin botón "Editar".

---

**Fin del documento — TC-02-permisos-por-punto-colaborador_v1.0.md**
