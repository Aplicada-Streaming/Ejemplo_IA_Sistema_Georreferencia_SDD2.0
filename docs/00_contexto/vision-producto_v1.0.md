**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** vision-producto_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-00 via orquestador

---

# Visión del Producto

## 1. Declaración de visión

**Para** los relevadores de campo y jefes de área de Vialidad, **que necesitan** capturar el estado de la infraestructura vial (rutas, puentes, pavimento) trabajando colaborativamente en zonas con conectividad limitada o nula y consolidar la información rápidamente para análisis y planificación de intervenciones, **el Sistema de Gestión de Relevamientos Georreferenciados** es una **plataforma integral móvil + web + backend** que **permite capturar puntos georreferenciados con fotos y datos estructurados de manera offline-first y colaborativa, con sincronización idempotente y resolución de conflictos automática y manual, sobre plantillas de inspección extensibles que habilitan analítica transversal entre tipos de obra**, **a diferencia del** proceso manual actual basado en notas en papel, fotos en cámaras independientes y reconciliación tardía en gabinete, que pierde contexto geoespacial, no tolera trabajo paralelo y demora el ciclo entre captura y dato analizable.

## 2. Propuesta de valor

- **Trabajo de campo offline-first sin pérdida de datos.** Los relevadores operan días enteros sin señal y la sincronización ocurre cuando aparece conectividad, con outbox y reintentos exponenciales transparentes al usuario.
- **Colaboración multi-colaborador real en una misma campaña.** Dos o más relevadores trabajan simultáneamente en el mismo puente o tramo, ven los puntos del otro al sincronizar, y los conflictos se resuelven por last-write-wins por campo con notificación post-sync para revisión humana cuando corresponde.
- **Plantillas de inspección extensibles sin tocar código.** Una plantilla raíz genérica más herencia y versionado permiten dar de alta nuevos tipos de inspección (puente, pavimento, alcantarillas, señalética) cambiando solo configuración. El frontend renderiza dinámicamente.
- **Trazabilidad técnica completa por punto y por foto.** Cada cambio queda registrado con quién, cuándo, qué campo, valor anterior y nuevo, origen del evento. Habilita resolución de conflictos, consultas históricas y, si más adelante el cliente lo requiere, una etapa formal de auditoría.
- **Storage de fotos abstraído y portabilidad de frontend garantizada.** El backend expone una API REST limpia con OpenAPI versionado y storage configurable (local, S3, FTP, SFTP), de modo que cambiar el adaptador de almacenamiento o reemplazar el frontend por otra tecnología no requiere reescribir el dominio.

## 3. Audiencia objetivo

| Audiencia | Frente principal | Necesidad central |
|---|---|---|
| Relevador de campo (dueño) | App móvil MAUI | Capturar puntos georreferenciados con fotos y datos en condiciones de conectividad nula, sin perder información ni contexto geoespacial. |
| Colaborador asignado | App móvil MAUI | Apoyar relevamientos ajenos creando puntos propios sin pisar los del dueño, con permisos restringidos por punto. |
| Jefe de área (Supervisor) | App web Blazor | Gestionar relevamientos del área, aceptar relevadores nuevos, asignar colaboradores, revisar y resolver conflictos pendientes. |
| Admin raíz | App web Blazor | Configurar el sistema en su primer arranque (storage), aceptar/inhabilitar/dar de baja jefes de área. |
| Equipo de gabinete (futuro) | App web | Consumir datos consolidados para análisis y planificación de intervenciones. |

## 4. Métricas de éxito SMART

> Las métricas de éxito propuestas son inferencias razonables del intake. Requieren validación con el sponsor antes de fijar valores objetivo definitivos. Donde el cliente no aportó base, se marca `[REQUIERE_INFO]`.

| ID | Métrica | Específica | Medible | Alcanzable | Relevante | Plazo (T) |
|---|---|---|---|---|---|---|
| MET-01 | Tiempo entre captura en campo y dato disponible en gabinete | Mediana de días desde el primer punto capturado de un relevamiento hasta que está visible y consolidado en la web | Logs de servidor + timestamps de sync | Reducir de [REQUIERE_INFO baseline] a ≤ 1 día hábil | Acelera el ciclo decisional sobre intervenciones | Final del MVP (Slice 4 desplegado) |
| MET-02 | Cobertura del proceso digital sobre el manual | % de relevamientos del organismo capturados a través del sistema vs. proceso manual | Conteo en DB / total de campañas reportadas por el cliente | ≥ 60% al cierre del MVP, ≥ 90% a 6 meses post-MVP | Mide adopción real, no instalaciones | 6 meses post go-live productivo |
| MET-03 | Adopción efectiva por relevador | Cantidad de puntos capturados por mes por usuario activo | Query a la tabla `points` agrupada por `created_by` | ≥ 50 puntos/mes/relevador activo a partir del segundo mes de uso | Distingue uso real de instalaciones inertes | 3 meses post go-live |
| MET-04 | Tiempo de incorporación de un nuevo tipo de inspección | Días desde que se solicita una plantilla nueva hasta que está operativa para los relevadores | Registro de tickets / cambios en tabla `templates` | ≤ 5 días hábiles para una plantilla derivada (sin nuevos campos custom de UI) | Valida que la abstracción de plantillas no se degrade | Validable a partir del Slice 5 |
| MET-05 | Tasa de pérdida de datos en sincronización | % de operaciones del outbox local que terminan en estado terminal-error después de los reintentos | Métrica de la tabla `pending_operations` / total de operaciones generadas | ≤ 0,1% sobre operaciones de los últimos 30 días | Mide la robustez del núcleo offline-first | Permanente a partir de Slice 1 |
| MET-06 | Tiempo de resolución de candidatos a fusión | Mediana de horas desde que un candidato a fusión es detectado hasta que el jefe de área lo resuelve | Timestamps en panel de conflictos | ≤ 48h en horario hábil | Mide la usabilidad del panel de conflictos | A partir de Slice 10 |

> Las métricas de adopción y cobertura (MET-02, MET-03) requieren `[REQUIERE_INFO]` sobre el volumen base de campañas y relevadores activos del cliente para fijar la línea de base.

## 5. Principios de producto

Estos principios guían las decisiones de diseño cuando aparece un trade-off:

1. **El relevamiento de campo manda.** Si una decisión beneficia al relevador en campo a costa de complejidad en gabinete, gana el relevador. La calidad de la captura determina la calidad de todo lo posterior.
2. **Offline es la norma, online es la excepción.** Todo flujo crítico debe poder ejecutarse sin red. La conectividad solo se asume para sincronizar y revisar.
3. **No fusionamos lo que el humano puede confirmar barato.** Donde la decisión automática tiene riesgo de pérdida silenciosa de información (fusiones, eliminaciones con actividad), proponemos al humano en lugar de decidir por él.
4. **El frontend es desechable, el contrato no.** La API REST con OpenAPI versionado es el activo durable. Cualquier frontend (Blazor hoy, React mañana) consume el mismo contrato.
5. **Las plantillas son configuración, no código.** Agregar un tipo de inspección no es un release.

## 6. Visión de evolución

El sistema entrega el MVP cubriendo el caso concreto de Vialidad (rutas y puentes), pero está diseñado para extenderse a otras inspecciones de obra pública (alcantarillas, señalética, drenajes, edificios públicos) sin reescritura. La extensión típica esperada es: nueva plantilla derivada de la raíz + ajuste de parámetros de captura + ajuste de UI de campos custom solo si el caso lo requiere.

Funcionalidades fuera del MVP que el roadmap deja explícitamente reservadas para fases posteriores:

- Etapa formal de cierre/aprobación de relevamiento por jefe de área (`[REQUIERE_INFO]` en intake; pendiente de definición con cliente).
- Migración masiva de archivos entre adaptadores de storage.
- Pipeline ML de pre-clasificación de defectos sobre fotos.
- Detección automática de fotos borrosas o mal expuestas.
- Compresión adaptativa según ancho de banda al sincronizar.
- Estrategia de archivado frío de relevamientos históricos.
- Migración del flujo de auth de ROPC a OAuth 2.1 con code+PKCE (deuda DT-01).

## 7. Trazabilidad

| Documento upstream | Sección | Aporte |
|---|---|---|
| `devs/intake/PROJECT-README.md` | 2 — El cliente | Identificación de stakeholders y vocación de extensión |
| `devs/intake/PROJECT-README.md` | 3 — El problema actual | Justificación del cambio y dolor del proceso manual |
| `devs/intake/PROJECT-README.md` | 4 — Qué se quiere construir | Composición del sistema y actores |
| `devs/intake/PROJECT-README.md` | 8 — Definición de éxito | Métricas inferidas |
| `devs/intake/PROJECT-BRIEF.md` | 5 — Sincronización multi-colaborador | Núcleo de la propuesta de valor diferencial |

## 8. Documentos relacionados (esta sección)

- [Alcance del proyecto](alcance-proyecto_v1.0.md)
- [Roadmap del producto](roadmap-producto_v1.0.md)
- [Acuerdo de equipo](acuerdo-equipo_v1.0.md)

---

**Fin del documento — vision-producto_v1.0.md**
