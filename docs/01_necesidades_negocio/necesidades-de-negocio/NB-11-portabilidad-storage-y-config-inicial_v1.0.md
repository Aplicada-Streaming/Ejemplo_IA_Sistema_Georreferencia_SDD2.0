**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** NB-11-portabilidad-storage-y-config-inicial_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-01 via orquestador

---

## NB-11 — Configuración operativa del sistema portable entre proveedores de almacenamiento

### Problema específico

Las fotos generadas por relevamientos representan el grueso del volumen del sistema. Distintos clientes y distintas fases del propio cliente pueden requerir destinos de almacenamiento diferentes: storage local en un servidor de Vialidad, S3 de AWS, FTP/SFTP heredados, etc. Si el sistema queda acoplado a un único proveedor, perder la libertad de cambiar lo deja a merced del costo del proveedor o de su disponibilidad. Además, la configuración inicial de storage no puede ser un cambio de código: el admin raíz tiene que poder configurarlo desde la web sin un release.

### Impacto si no se resuelve

- El sistema queda acoplado al proveedor con el que se hizo el go-live.
- Cambios de proveedor requieren reescritura del código de dominio.
- La configuración inicial obliga a tocar `appsettings.json` y desplegar, lo que el cliente no quiere.
- Pérdida de flexibilidad operativa para distintos ambientes (desarrollo local vs. productivo).

### Criterios de éxito

| Métrica | Baseline | Target | Plazo | Cómo se mide |
|---|---|---|---|---|
| Adaptadores funcionales en MVP | N/A | Local + S3 funcionales y verificados; FTP/SFTP funcionales con verificación opcional según ambiente del cliente | Slice 8 | Tests de integración por adaptador |
| Cambio de adaptador sin tocar código de dominio | N/A | El módulo de dominio compila y testea sin conocer adaptadores específicos | Slice 0 / Slice 8 | Inspección de código y tests unitarios del dominio sin adaptadores reales |
| Wizard de primer arranque operativo | N/A | Flujo guiado del admin raíz para fijar storage en primera ejecución | Slice 8 | Validación funcional |
| Reconfiguración posterior disponible | N/A | El admin raíz puede cambiar storage desde la web sin redeploy | Slice 8 | Validación funcional |
| Datos previos siguen accesibles | N/A | Los relevamientos creados con el adaptador anterior se siguen leyendo y escribiendo correctamente | Slice 8 | Test e2e de cambio de adaptador con datos previos |

### Stakeholders

- **Admin raíz** — usuario primario del wizard y la reconfiguración.
- **Sponsor** — la portabilidad es decisión estratégica.
- **Equipo técnico** — operación y debug del sistema.

### RFs y RNFs cubiertos

RF-60, RF-61, RF-62, RNF-04.

### Trazabilidad a Casos de Uso

| CU | Nombre | Estado |
|---|---|---|
| `[A completar por SA-02]` | — | Pendiente |

### Dependencias con otras NB

- **Habilita:** [NB-01](NB-01-captura-georreferenciada-en-campo_v1.0.md) (las fotos se guardan en el storage configurado), [NB-05](NB-05-onboarding-relevamientos-previos_v1.0.md) (la carga manual también va al storage), [NB-06](NB-06-revision-y-consolidacion-en-gabinete_v1.0.md) (la web lee fotos del storage).
- **Independiente** del resto desde el punto de vista de negocio.

> Decisión de diseño DD-14 (`PROJECT-BRIEF` Sec. 4): arquitectura hexagonal en el módulo `Storage` para cumplir esta NB. La migración masiva entre adaptadores está **explícitamente fuera del MVP** (ver [alcance EX-02](../../00_contexto/alcance-proyecto_v1.0.md)).

---

**Fin del documento — NB-11-portabilidad-storage-y-config-inicial_v1.0.md**
