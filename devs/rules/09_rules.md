# Reglas Constructivas — SA-09: DevOps y CI/CD

**Sección objetivo:** `docs/09_devops/`  
**Rol del subagente:** DevOps Engineer + Site Reliability Engineer  
**Depende de:** SA-08 completo + PROJECT-BRIEF.md (Secciones 6 — infraestructura)  
**Es upstream de:** ninguno (último de la cadena documental)

---

## 1. Documentos a leer ANTES de escribir

1. `docs/08_calidad_y_pruebas/definition-of-done_v1.0.md`
2. `docs/08_calidad_y_pruebas/estrategia-testing_v1.0.md`
3. `devs/intake/PROJECT-BRIEF.md` — sección infraestructura y deploy
4. `docs/05_arquitectura_tecnica/arquitectura-solucion_v1.0.md`

---

## 2. Artefactos a producir

| Archivo | Descripción | Obligatorio |
|---|---|---|
| `pipeline-cicd_v1.0.md` | Definición del pipeline CI/CD con stages y quality gates | Sí |
| `estrategia-ambientes_v1.0.md` | Ambientes (dev/staging/prod), configuración, promotion flow | Sí |
| `estrategia-versionado_v1.0.md` | SemVer, branching strategy, release notes | Sí |

---

## 3. Reglas constructivas

### 3.1 Pipeline CI/CD

El pipeline debe cubrir como mínimo: build → test → quality gates → package → deploy to staging → smoke test → promote to prod. Cada stage tiene: nombre, herramienta, criterio de pass/fail, qué pasa si falla.

### 3.2 Quality gates

Los quality gates del pipeline deben coincidir con el DoD de SA-08. Si el DoD dice "cobertura ≥ 80%", el pipeline debe fallar si la cobertura es menor.

### 3.3 Branching strategy

Definir al menos: rama de desarrollo, rama de feature, rama de release, rama de hotfix. Documentar el flujo de merge y los permisos requeridos.

---

## 4. Criterios de calidad

- [ ] ¿El pipeline tiene quality gates que coinciden con el DoD?
- [ ] ¿Están documentados los tres ambientes (dev/staging/prod)?
- [ ] ¿La branching strategy está definida con nombres de ramas y flujo de merge?
