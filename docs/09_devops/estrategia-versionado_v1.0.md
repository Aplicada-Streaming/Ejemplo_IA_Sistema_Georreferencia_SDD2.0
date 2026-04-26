**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** estrategia-versionado_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-09 via orquestador

---

# Estrategia de Versionado

Define versionado del software, branching strategy y release notes. Cubre todos los componentes desplegables: backend, frontend web, workers, app móvil. Las plantillas de inspección tienen su propio esquema de versionado documentado en [arquitectura-solucion](../05_arquitectura_tecnica/arquitectura-solucion_v1.0.md) y [RN-05](../02_especificacion_funcional/reglas-de-negocio/RN-05-inmutabilidad-plantilla-publicada_v1.0.md), no incluido aquí.

---

## 1. SemVer para componentes desplegables

Cada componente sigue **SemVer 2.0** (MAJOR.MINOR.PATCH):

- **MAJOR:** cambios incompatibles en el contrato público (API REST → ruptura de OpenAPI; móvil → ruptura mínima requerida con backend).
- **MINOR:** funcionalidad nueva sin romper compatibilidad.
- **PATCH:** bugfixes y mejoras internas sin cambio de contrato.

| Componente | Versión inicial | Notas |
|---|---|---|
| Backend API | 0.1.0 → 1.0.0 al MVP | El path `/api/v1/...` es independiente y se mantiene mientras la API mayor sea v1 |
| Frontend web | 0.1.0 → 1.0.0 al MVP | — |
| App móvil | 0.1.0 → 1.0.0 al MVP | Compatibilidad mínima con backend declarada en cada release |
| Workers | 0.1.0 → 1.0.0 al MVP | Versionados independientemente |

### Compatibilidad mínima requerida

La app móvil declara en su release notes: *"requiere backend ≥ X.Y.Z"*. El backend mantiene compatibilidad hacia atrás dentro de v1.x.x.

---

## 2. Branching strategy

### 2.1. Ramas principales

| Rama | Propósito | Vida | Permisos para mergear |
|---|---|---|---|
| `main` | Estado siempre desplegable a Staging. Cada commit que llega es un release candidate. | Permanente | PR + CI verde + 1+ revisores |
| `release/X.Y` | Rama de release: estabilización antes de desplegar a Producción. Solo bugfixes y docs. | Hasta tag `vX.Y.Z` | PR + CI verde + Tech Lead |

### 2.2. Ramas de trabajo

| Rama | Naming | Origen | Destino |
|---|---|---|---|
| Feature | `feature/US-XX-titulo-corto` | `main` | `main` (PR) |
| Bugfix | `bugfix/US-XX-titulo-corto` o `bugfix/issue-NNN` | `main` o `release/X.Y` | el origen (PR) |
| Hotfix | `hotfix/issue-NNN` | `main` (o tag de prod) | `main` + cherry-pick a `release/X.Y` activa |
| Spike | `spike/nombre-corto` | `main` | descartable, sin merge |

### 2.3. Flujo de merge

```
[Feature branch]
    │
    │ PR a main
    ▼
[main]
    │
    │ release: branch + tag
    ▼
[release/X.Y]  ← solo bugfixes y docs
    │
    │ tag vX.Y.Z
    ▼
[Producción]   (cuando exista)
```

### 2.4. Protección de ramas

- `main` y `release/*` requieren:
  - PR aprobado por al menos 1 revisor (2 para módulos críticos).
  - CI completa en verde.
  - Linear history (rebase + merge, sin merge commits sucios).
  - Sin force-push.
- `feature/*` y `bugfix/*` no tienen protección.

---

## 3. Release notes

Cada release genera un `CHANGELOG.md` con secciones:

```markdown
## [X.Y.Z] - YYYY-MM-DD

### Added
- US-XX: ...

### Changed
- ...

### Fixed
- Issue #NNN: ...

### Security
- ...

### Deprecated
- ...

### Removed
- ...

### Migration notes
- (si aplica)
```

Las release notes se generan **semi-automáticamente** desde commits y PRs cerrados. El equipo revisa y consolida antes de publicar.

---

## 4. Convención de mensajes de commit

`[PENDIENTE]` Confirmar con el equipo en Sprint 0 ([acuerdo-equipo](../00_contexto/acuerdo-equipo_v1.0.md)).

Sugerido: **Conventional Commits**.

```
<tipo>(<scope>): <descripción corta>

<cuerpo opcional>

<footer opcional con refs>
```

Tipos válidos: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`, `ci`, `build`.

Scope sugerido: módulo afectado (`identity`, `sync`, `templates`, `points`, `photos`, `storage`, `web`, `mobile`).

Refs en footer: `Refs US-XX`, `Closes #NNN`.

Ejemplos:

- `feat(sync): add idempotency check on push events`
- `fix(mobile): handle GPS timeout S3-TIMEOUT correctly`
- `docs(adr): publish ADR-03 with sync design`
- `refactor(storage): extract S3 adapter to its own project`

---

## 5. Versionado de la API REST

Independiente del SemVer del backend.

- Versión actual: **v1**, expuesta como prefijo `/api/v1/...`.
- Cambios compatibles dentro de v1: agregados de campos opcionales en responses, agregados de endpoints, agregados de campos opcionales en requests.
- Cambios incompatibles dentro de v1: prohibidos. Se introduce **v2** (`/api/v2/...`) cuando se necesite.
- v1 y v2 conviven al menos un release MAYOR.

---

## 6. Versionado de plantillas de inspección

Documentado en [arquitectura-solucion](../05_arquitectura_tecnica/arquitectura-solucion_v1.0.md) y [RN-05](../02_especificacion_funcional/reglas-de-negocio/RN-05-inmutabilidad-plantilla-publicada_v1.0.md). En síntesis:

- Cada plantilla tiene `version_number` secuencial y `status` `borrador`/`publicada`.
- Una versión publicada es **inmutable**.
- Cada relevamiento queda atado a una versión específica.
- Una nueva versión se crea explícitamente desde la última publicada.

Independiente del SemVer del software.

---

## 7. Política de hotfix

Para issues críticos en producción:

1. Crear `hotfix/issue-NNN` desde el tag actual de producción.
2. Implementar el fix con tests.
3. PR a `release/X.Y` activa.
4. Una vez mergeado: nuevo tag `vX.Y.(Z+1)` + deploy.
5. Cherry-pick a `main` para evitar regresión.

Tiempos objetivo del hotfix: ≤ 24h desde detección hasta deploy en producción para issues "Crítica" según matriz de prioridades (`[REQUIERE_INFO]` confirmar SLA con cliente).

---

## 8. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| [pipeline-cicd](pipeline-cicd_v1.0.md) | Pipeline que materializa los releases |
| [acuerdo-equipo](../00_contexto/acuerdo-equipo_v1.0.md) Sec. 6 | Normas de PR y commits |
| [arquitectura-solucion](../05_arquitectura_tecnica/arquitectura-solucion_v1.0.md) | API versionada como contrato |

---

**Fin del documento — estrategia-versionado_v1.0.md**
