**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** matriz-cobertura_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-08 via orquestador

---

# Matriz de Cobertura — CU × Tipo de Test × Prioridad

Tabla maestra que cruza cada CU con los tipos de test que debería tener y su prioridad. Esta matriz se usa para validar el DoD por US y para reportar progreso de cobertura en cada Sprint Review.

Convenciones:

- ✓ = Cobertura obligatoria.
- ◐ = Cobertura recomendada según costo/beneficio.
- — = No aplica.
- **TC-XX** = Test case formal documentado (ver `casos-de-prueba/`).

---

## 1. Matriz por CU

| CU | Unit | Integration | Contract | E2E | Manual | TCs | Prioridad |
|---|---|---|---|---|---|---|---|
| [CU-01](../02_especificacion_funcional/casos-de-uso/CU-01-iniciar-sesion-y-registro-jerarquico_v1.0.md) Login + registro | ✓ | ✓ | ✓ | ✓ | ◐ | — | Alta |
| [CU-02](../02_especificacion_funcional/casos-de-uso/CU-02-configurar-storage_v1.0.md) Configurar storage | ✓ | ✓ | ✓ | ◐ | ✓ | — | Alta |
| [CU-03](../02_especificacion_funcional/casos-de-uso/CU-03-crear-versionar-plantilla_v1.0.md) Crear/versionar plantilla | ✓ | ✓ | ✓ | ◐ | ✓ | TC-05 | Alta |
| [CU-04](../02_especificacion_funcional/casos-de-uso/CU-04-crear-relevamiento_v1.0.md) Crear relevamiento | ✓ | ✓ | ✓ | ✓ | ◐ | — | Crítica |
| [CU-05](../02_especificacion_funcional/casos-de-uso/CU-05-asignar-colaboradores-y-ciclo-vida_v1.0.md) Asignar y ciclo vida | ✓ | ✓ | ✓ | ✓ | ◐ | — | Alta |
| [CU-06](../02_especificacion_funcional/casos-de-uso/CU-06-capturar-punto-georreferenciado_v1.0.md) **Capturar punto** | ✓ | ✓ | ✓ | ✓ | ✓ | TC-01 | **Crítica** |
| [CU-07](../02_especificacion_funcional/casos-de-uso/CU-07-editar-catalogo-punto-desde-movil_v1.0.md) **Editar catálogo móvil** | ✓ | ✓ | ✓ | ◐ | ✓ | TC-02 | **Alta** |
| [CU-08](../02_especificacion_funcional/casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md) **Sincronizar** | ✓ | ✓ | ✓ | ✓ | ✓ | TC-03, TC-04 | **Crítica** |
| [CU-09](../02_especificacion_funcional/casos-de-uso/CU-09-cargar-lote-fotos-web_v1.0.md) Carga lote web | ✓ | ✓ | ✓ | ◐ | ✓ | — | Media |
| [CU-10](../02_especificacion_funcional/casos-de-uso/CU-10-revisar-y-editar-relevamiento-web_v1.0.md) Revisar y editar web | ✓ | ✓ | ✓ | ✓ | ✓ | TC-02 (parte) | Alta |
| [CU-11](../02_especificacion_funcional/casos-de-uso/CU-11-resolver-candidato-fusion_v1.0.md) **Resolver fusión** | ✓ | ✓ | ✓ | ✓ | ✓ | TC-04 | **Alta** |
| [CU-12](../02_especificacion_funcional/casos-de-uso/CU-12-consultar-trazabilidad-punto_v1.0.md) Trazabilidad | ✓ | ✓ | ✓ | ◐ | — | — | Media |

## 2. Matriz por RN

Cada RN tiene una batería de tests que valida sus aplicaciones correctas y violaciones esperadas.

| RN | Unit | Integration | Tests adicionales |
|---|---|---|---|
| [RN-01](../02_especificacion_funcional/reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md) | ✓ | ✓ | TC-02 |
| [RN-02](../02_especificacion_funcional/reglas-de-negocio/RN-02-restricciones-eliminacion-relevamiento_v1.0.md) | ✓ | ✓ | TC-02 (caso 5) |
| [RN-03](../02_especificacion_funcional/reglas-de-negocio/RN-03-plantilla-raiz-inmutable_v1.0.md) | ✓ | ✓ | TC-05 (caso 8) |
| [RN-04](../02_especificacion_funcional/reglas-de-negocio/RN-04-restricciones-herencia-plantillas_v1.0.md) | ✓ | ✓ | TC-05 |
| [RN-05](../02_especificacion_funcional/reglas-de-negocio/RN-05-inmutabilidad-plantilla-publicada_v1.0.md) | ✓ | ✓ | TC-05 (casos 6-7) |
| [RN-06](../02_especificacion_funcional/reglas-de-negocio/RN-06-guids-cliente-idempotencia_v1.0.md) | ✓ | ✓ | TC-03 |
| [RN-07](../02_especificacion_funcional/reglas-de-negocio/RN-07-resolucion-conflictos-lww-y-precedencia_v1.0.md) | ✓ | ✓ | TC-03 |
| [RN-08](../02_especificacion_funcional/reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md) | ✓ | ✓ | — (a documentar TC) |
| [RN-09](../02_especificacion_funcional/reglas-de-negocio/RN-09-deteccion-candidatos-fusion_v1.0.md) | ✓ | ✓ | TC-04 |
| [RN-10](../02_especificacion_funcional/reglas-de-negocio/RN-10-eventos-append-only_v1.0.md) | ✓ | ✓ (DB-trigger) | — |
| [RN-11](../02_especificacion_funcional/reglas-de-negocio/RN-11-aceptacion-jerarquica-y-movil-restringido_v1.0.md) | ✓ | ✓ | — |
| [RN-12](../02_especificacion_funcional/reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md) | ✓ | ✓ (cambio adapter) | — |

## 3. Matriz por US (mapeo a tests obligatorios del DoD)

| US | Unit | Integration | Contract | E2E | Manual | Notas DoD |
|---|---|---|---|---|---|---|
| US-01 | ✓ | ✓ | ✓ | ✓ | ◐ | — |
| US-02 | ✓ | ✓ | ✓ | ✓ | — | — |
| US-03 | ✓ | ✓ | — | ✓ | ✓ | Dos clientes (DoD sync) |
| US-04 | ✓ | ✓ | ✓ | ✓ | — | Idempotencia con tests parametrizados |
| US-05 | ✓ | ✓ | ✓ | ✓ | — | — |
| US-06 | ✓ | ✓ | ✓ | ◐ | ✓ | — |
| US-07 | ✓ | ✓ | ✓ | ✓ | ✓ | Dispositivo físico (DoD móvil) |
| US-08 | ✓ | ✓ | — | ◐ | ✓ | Dispositivo físico |
| US-09 | ✓ | ✓ | ✓ | ✓ | ✓ | Permisos por punto |
| US-10 | ✓ | ✓ | — | ✓ | ✓ | — |
| US-11 | ✓ | ✓ | ✓ | ◐ | ✓ | Dos plantillas distintas |
| US-12 | ✓ | ✓ | — | ✓ | ✓ | Dos plantillas distintas |
| US-13 | ✓ | ✓ | ✓ | ✓ | — | Matriz de roles |
| US-14 | ✓ | ✓ | — | ✓ | ✓ | Matriz de permisos completa |
| US-15 | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| US-16 | ✓ | ✓ | ✓ | ◐ | ✓ | — |
| US-17 | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| US-18 | ✓ | ✓ | — | ✓ | ✓ | Tests de adaptador con testcontainers |
| US-19 | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| US-20 | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| US-21 | ✓ | ✓ | — | ✓ | — | Suite race scenarios |
| US-22 | ✓ | ✓ | ✓ | ✓ | ✓ | — |

---

## 4. Reporte por Sprint Review

En cada Sprint Review se reporta:

| Métrica | Sprint 0 | Sprint 1 | ... |
|---|---|---|---|
| US del sprint con cobertura DoD completa | — | — | |
| TCs ejecutados | — | — | |
| % cobertura de líneas | — | — | |
| % cobertura de ramas (sync + permisos) | — | — | |
| Tests añadidos en el sprint | — | — | |
| Tests fallidos en CI durante el sprint (no intermitentes) | — | — | |

---

## 5. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| [estrategia-testing](estrategia-testing_v1.0.md) | Pirámide y herramientas que la matriz consolida |
| [definition-of-done](definition-of-done_v1.0.md) | Criterios DoD que la matriz operacionaliza |
| Casos de uso CU-* | Criterios de aceptación a cubrir |
| [product-backlog](../06_backlog-tecnico/product-backlog_v1.0.md) | US cuyas pruebas se planifican |

---

**Fin del documento — matriz-cobertura_v1.0.md**
