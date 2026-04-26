**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** pipeline-cicd_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-09 via orquestador

---

# Pipeline CI/CD

Define la pipeline automatizada para integración continua y despliegue. La parte de **CI** es obligatoria desde el Sprint 0 (BT-02). La parte de **CD** queda **parcialmente cubierta en MVP**: el deploy automático a ambientes superiores depende de [REQUIERE_INFO] del intake (`PROJECT-BRIEF` Sec. 9.2). El MVP entrega la pipeline lista para activarse cuando el cliente confirme ambientes.

---

## 1. Herramienta de CI

`[REQUIERE_INFO]` Confirmar con el cliente: GitHub Actions / Azure DevOps / GitLab CI / Jenkins. Asumimos **GitHub Actions** como default razonable, intercambiable por el equivalente con esfuerzo bajo.

---

## 2. Stages del pipeline CI (en cada PR)

```
[1. Checkout]
   │
   ▼
[2. Restore dependencies]
   │
   ▼
[3. Build]  ← falla si hay errores de compilación
   │
   ▼
[4. Linting + format check]  ← .NET analyzers + dotnet format
   │
   ▼
[5. Unit tests]  ← xUnit + Coverlet
   │
   ▼
[6. Architecture tests]  ← NetArchTest (módulos no acceden tablas ajenas)
   │
   ▼
[7. Integration tests]  ← Testcontainers (SQL, Minio, FTP/SFTP)
   │
   ▼
[8. Contract tests]  ← OpenAPI snapshot
   │
   ▼
[9. E2E smoke tests]  ← Playwright + dos clientes simulados
   │
   ▼
[10. Quality gate check]
   │
   ▼
[11. Build artifacts]  ← binarios + paquetes
   │
   ▼
[12. Publish coverage report]
   │
   ▼
[13. Status reporting al PR]
```

Cada stage tiene su criterio claro de pass/fail. Una falla en cualquier stage bloquea el merge.

---

## 3. Quality gates (alineados con DoD)

Los gates del pipeline replican los criterios técnicos del [DoD](../08_calidad_y_pruebas/definition-of-done_v1.0.md):

| Gate | Métrica | Umbral | Acción si falla |
|---|---|---|---|
| Build | Errores de compilación | 0 | Bloquear merge |
| Linting | Reglas violadas | 0 (warnings críticos) | Bloquear merge |
| Cobertura unit | Líneas de dominio | ≥ 80% | Bloquear merge |
| Cobertura ramas críticas | Sync + permisos | ≥ 90% | Bloquear merge |
| Tests fallidos | Cualquiera | 0 | Bloquear merge |
| Architecture tests | Reglas de dependencia | 0 violaciones | Bloquear merge |
| Tiempo de pipeline total | Por PR | ≤ 25 min | Warning si excede; investigar |

Para los **PR a `main`** se exige **smoke E2E** además. Para PRs a ramas de feature, los E2E se ejecutan **nightly** sobre `main`.

---

## 4. Pipeline nightly

Ejecuta sobre `main`:

- Suite **E2E completa** (no solo smoke).
- Suite **race scenarios de sync** (más tiempo).
- Suite **adaptadores de storage** (todos los adaptadores).
- Reporte de **performance** básico (P95 de endpoints clave).

Los nightlies fallidos generan un issue automático y notifican al canal del equipo. No bloquean trabajo activo, pero deben atenderse al inicio del día siguiente.

---

## 5. Pipeline de release

Activa solo cuando se mergea a una rama de release o se crea un tag SemVer.

```
[Pipeline release]
   │
   ▼
[Ejecutar pipeline CI completa]
   │
   ▼
[Generar artefactos versionados]
   │
   ▼
[Publicar artefactos]  ← según [REQUIERE_INFO]: registry / blob / package feed
   │
   ▼
[Generar release notes]  ← desde commits + PRs cerrados desde la última versión
   │
   ▼
[Tag git + crear release en repo]
   │
   ▼
[Trigger deploy a Staging]  ← cuando ambiente exista
   │
   ▼
[Smoke tests en Staging]
   │
   ▼
[Aprobación manual para Producción]
   │
   ▼
[Deploy a Producción]
   │
   ▼
[Health checks post-deploy]
   │
   ▼
[Notificación de release exitosa]
```

> Las etapas de Deploy (Staging y Prod) se activan cuando el cliente defina los ambientes superiores (`PROJECT-BRIEF` Sec. 9.2). Hasta entonces el pipeline llega hasta la generación de artefactos versionados.

---

## 6. Tiempos objetivo

| Pipeline | Tiempo objetivo |
|---|---|
| Pipeline CI por PR | ≤ 25 min |
| Pipeline nightly completo | ≤ 90 min |
| Pipeline release a artefactos | ≤ 30 min |
| Smoke en Staging | ≤ 10 min |
| Deploy a Producción + health check | ≤ 10 min |

---

## 7. Notificaciones

| Evento | Canal | Audiencia |
|---|---|---|
| PR build falla | Comentario en PR | Autor del PR |
| PR build pasa | Comentario en PR | Autor del PR |
| Nightly falla | `[REQUIERE_INFO]` Slack/Teams del equipo | Tech Lead + autor del último merge |
| Release deploy exitoso | Canal del equipo | Equipo + sponsor |
| Release deploy falla | Canal del equipo | Equipo + Tech Lead inmediato |

---

## 8. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| [definition-of-done](../08_calidad_y_pruebas/definition-of-done_v1.0.md) | Criterios que los quality gates ejecutan |
| [estrategia-testing](../08_calidad_y_pruebas/estrategia-testing_v1.0.md) | Tipos de tests del pipeline |
| [arquitectura-solucion](../05_arquitectura_tecnica/arquitectura-solucion_v1.0.md) | Componentes a empaquetar |
| `devs/intake/PROJECT-BRIEF.md` Sec. 9 | Local-dev como ambiente confirmado; superiores `[REQUIERE_INFO]` |

---

**Fin del documento — pipeline-cicd_v1.0.md**
