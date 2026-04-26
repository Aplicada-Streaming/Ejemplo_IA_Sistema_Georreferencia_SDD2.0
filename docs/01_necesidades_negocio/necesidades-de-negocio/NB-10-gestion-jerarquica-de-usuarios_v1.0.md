**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** NB-10-gestion-jerarquica-de-usuarios_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-01 via orquestador

---

## NB-10 — Gestión jerárquica de usuarios y permisos finos

### Problema específico

La organización tiene una estructura jerárquica clara: un admin raíz controla los jefes de área, cada jefe controla sus relevadores. Sin un mecanismo de gestión que respete esa jerarquía, los registros de nuevos usuarios pueden volverse caóticos (cualquiera puede entrar a cualquier área), o demasiado centralizados (todo pasa por el admin raíz). Además, el cliente exigió permisos finos a nivel del **punto** dentro del relevamiento: el dueño puede editar todo, los colaboradores solo lo que ellos crearon. Sin este nivel de granularidad, los colaboradores pisan trabajo del dueño o se les bloquea innecesariamente.

### Impacto si no se resuelve

- Registro caótico de usuarios o cuello de botella en el admin raíz.
- Trabajo de un colaborador pisa al del dueño o del relevador original.
- Dueños no pueden corregir errores de colaboradores.
- Inhabilitación de un jefe de área se vuelve drástica (eliminar) cuando bastaría con suspender.

### Criterios de éxito

| Métrica | Baseline | Target | Plazo | Cómo se mide |
|---|---|---|---|---|
| Aceptación jerárquica funcional | N/A | Admin raíz acepta jefes; jefes aceptan relevadores de su área | Slice 6 | Test e2e |
| Inhabilitación de jefe reversible | N/A | Acción reversible vs. baja definitiva, ambas operativas | Slice 6 | Test funcional |
| Permisos por punto correctos | N/A | Matriz de permisos validada al 100% (dueño, colaborador, jefe, otro) | Slice 6 | Test de autorización |
| Móvil restringido a relevadores | N/A | Otros roles no pueden loguear en móvil | Slice 6 | Test de autorización |
| Web disponible para todos los roles aplicables | N/A | Login y vistas adaptadas por rol | Slice 6 | Test funcional |

### Stakeholders

- **Admin raíz** — gestiona jefes de área.
- **Jefe de área** — gestiona relevadores de su área.
- **Relevador (dueño)** — necesita garantizar que sus puntos no se modifiquen sin permiso.
- **Colaborador asignado** — necesita poder editar lo suyo sin pisar al dueño.

### RFs y RNFs cubiertos

RF-55, RF-56, RF-57, RF-58, RF-59.

### Trazabilidad a Casos de Uso

| CU | Nombre | Estado |
|---|---|---|
| `[A completar por SA-02]` | — | Pendiente |

### Dependencias con otras NB

- **Habilita:** [NB-04](NB-04-gestion-ciclo-vida-relevamiento_v1.0.md) (los permisos sobre relevamientos), [NB-06](NB-06-revision-y-consolidacion-en-gabinete_v1.0.md) (los permisos sobre puntos).
- **Independiente** del resto.

---

**Fin del documento — NB-10-gestion-jerarquica-de-usuarios_v1.0.md**
