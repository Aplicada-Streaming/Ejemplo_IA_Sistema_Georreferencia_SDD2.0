**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** NB-05-onboarding-relevamientos-previos_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-01 via orquestador

---

## NB-05 — Incorporación de relevamientos hechos sin la app móvil

### Problema específico

Existen relevamientos de campo que se hicieron antes de que la app móvil estuviera disponible, o por relevadores que ese día no tenían el dispositivo, o con cámaras independientes (DSLR, GoPro, drones) cuyas fotos no son capturas dentro de la app pero sí contienen metadata EXIF con coordenadas y timestamps. Forzar a estos casos a quedar fuera del sistema empuja al cliente a mantener dos procesos en paralelo, perdiendo la principal ventaja de digitalizar la operación.

### Impacto si no se resuelve

- Persiste un proceso paralelo manual para todo lo que no nació en la app.
- La cobertura del sistema queda topeada por el dispositivo disponible en campo.
- Las fotos de cámaras profesionales (drones, DSLR) quedan fuera del catálogo digital.
- La adopción del sistema sufre porque no cubre la totalidad de los flujos reales.

### Criterios de éxito

| Métrica | Baseline | Target | Plazo | Cómo se mide |
|---|---|---|---|---|
| Tiempo medio para subir un lote de fotos previas y dejarlas asociadas a un relevamiento | N/A | ≤ 5 minutos para un lote de 50 fotos con EXIF | Slice 7 | Cronómetro en validación funcional |
| Porcentaje de fotos con EXIF GPS auto-georreferenciadas sin intervención manual | N/A | ≥ 90% (depende del origen) | Slice 7 | Métrica del worker de imagen |
| Funcionalidad de georreferenciación manual disponible | N/A | Picker en mapa + ingreso lat/lng operativo | Slice 7 | Validación funcional |
| Relevamientos cargados manualmente equivalentes en valor a los de móvil | N/A | Mismo modelo de datos, mismas operaciones posteriores disponibles | Slice 7 | Inspección de modelo |

### Stakeholders

- **Jefe de área** — onboardea fotos de relevamientos que se hicieron sin la app.
- **Relevador** — puede consolidar trabajos hechos con cámaras adicionales.
- **Equipo de gabinete** — beneficiario indirecto: cobertura completa de datos.

### RFs y RNFs cubiertos

RF-24, RF-25, RF-26, RF-27, RF-28.

### Trazabilidad a Casos de Uso

| CU | Nombre | Estado |
|---|---|---|
| `[A completar por SA-02]` | — | Pendiente |

### Dependencias con otras NB

- **Depende de:** [NB-04](NB-04-gestion-ciclo-vida-relevamiento_v1.0.md) (carga sobre un relevamiento), [NB-03](NB-03-tipos-diversos-de-inspeccion-sin-codigo_v1.0.md) (asocia a una plantilla), [NB-11](NB-11-portabilidad-storage-y-config-inicial_v1.0.md) (las fotos se guardan en el storage configurado).
- **Habilita:** [NB-06](NB-06-revision-y-consolidacion-en-gabinete_v1.0.md).

---

**Fin del documento — NB-05-onboarding-relevamientos-previos_v1.0.md**
