**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** wireframes-descripcion_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-03 via orquestador

---

# Wireframes — Descripción Textual

> **Nota sobre capturas de referencia:** El intake (`PROJECT-BRIEF` Sec. 2 y 11.3) declara que **no se compartieron capturas de pantalla** durante el relevamiento. Por lo tanto, los wireframes de este documento son descripciones textuales propuestas por el equipo, sujetas a validación con el cliente. La ruta `/devs/assets/screenshots/` queda reservada para cuando el cliente provea referencias visuales.

Cada pantalla describe: **propósito, elementos UI, acciones, estados (vacío, carga, error)**. La granularidad busca ser suficiente para que SA-05 diseñe los endpoints y SA-06 priorice US.

---

## 1. App móvil (MAUI Blazor Hybrid + MudBlazor)

### W-M01 — Login

**Propósito:** autenticar usuarios con rol `relevador` ([RN-11](../02_especificacion_funcional/reglas-de-negocio/RN-11-aceptacion-jerarquica-y-movil-restringido_v1.0.md)).

**Elementos UI:**
- Logo del sistema en la parte superior.
- Campo "Email".
- Campo "Contraseña".
- Botón **"Iniciar sesión"** (primario).
- Link "Registrarme" → abre la web (no se permite registro en móvil).

**Estados:**
- **Carga:** spinner sobre el botón mientras valida.
- **Error:** alerta debajo del formulario con el mensaje correspondiente (E1–E4 de [CU-01](../02_especificacion_funcional/casos-de-uso/CU-01-iniciar-sesion-y-registro-jerarquico_v1.0.md)).

### W-M02 — Lista de relevamientos asignados

**Propósito:** seleccionar un relevamiento para trabajar.

**Elementos UI:**
- Header con avatar + área del usuario + badge de sync.
- Tabs: "Mis relevamientos" / "Donde colaboro".
- Lista con cada item:
  - Nombre del relevamiento.
  - Etiquetas.
  - Cantidad de puntos · "Soy dueño" / "Soy colaborador".
  - Estado: Abierto / Cerrado.
  - Indicador de conflictos pendientes si aplica.
- FAB **"+ Nuevo relevamiento"** (visible si el usuario es relevador y puede crear).

**Estados:**
- **Vacío:** "Aún no tenés relevamientos asignados. Contactá a tu jefe de área."
- **Carga:** skeleton de la lista.
- **Error de carga (offline reciente):** "Sin conexión. Mostrando datos locales."

### W-M03 — Detalle de relevamiento (mapa de captura)

**Propósito:** capturar puntos en campo. Pantalla principal del relevador.

**Elementos UI:**
- Header con nombre del relevamiento + botón volver + menú (asignar, cerrar, reabrir, ver datos).
- **Mapa OpenStreetMap** ocupando casi toda la pantalla:
  - Marcadores con color por colaborador.
  - Indicador de posición GPS actual.
  - Marcador "actual" resaltado (si hay seleccionado).
- **Toggle de modo de captura** (chip horizontal): `Detenido` / `Móvil`. Si móvil, muestra el radio.
- **Botón flotante "Cámara"** (FAB primario, abajo-derecha).
- **Botón flotante "Centrar GPS"** (secundario).
- **Badge de sync** (esquina superior derecha): "✓ Sync · hace 5 min" / "↻ Pendientes: N".

**Acciones:**
- Tap en marcador → seleccionarlo como actual.
- Doble-tap en marcador → abrir catálogo del punto (W-M05).
- Drag del marcador actual → reubicar antes de tomar foto.
- Tap en cámara → abre W-M04.
- Tap en badge de sync → abre W-M06.

**Estados especiales:**
- **Sin permisos GPS:** banner superior con CTA a configuración.
- **Modo offline detectado:** badge "Offline" en lugar del de sync; las capturas siguen funcionando.

### W-M04 — Diálogo unificado de captura

**Propósito:** gestionar permisos + GPS + cámara con una máquina de estados consistente ([PROJECT-BRIEF Sec. 7](../../devs/intake/PROJECT-BRIEF.md)).

**Elementos UI por estado:**

| Estado | Contenido | Botones |
|---|---|---|
| S0 — Verificando | Spinner + "Verificando permisos..." | (ninguno) |
| S1-CAM-DENY | Mensaje + ícono cámara | "Ir a configuración" / "Cancelar" |
| S1-LOC-DENY | Mensaje + ícono ubicación | "Ir a configuración" / "Cancelar" |
| S1-BOTH-DENY | Mensaje combinado | "Ir a configuración" / "Cancelar" |
| S2 — Obteniendo GPS | Spinner + "Obteniendo posición... 0:12" (contador) | "Cancelar" |
| S3-LOWACC | "La precisión es baja: ±X m." | "Reintentar" / "Continuar igual" / "Cancelar" |
| S3-TIMEOUT | "No pudimos obtener la posición en X s." | "Reintentar" / "Cancelar" / ("Ingresar manualmente" si plantilla lo permite) |
| S3-NOSIGNAL | "La ubicación está desactivada en el dispositivo." | "Abrir ajustes" / "Cancelar" |
| S3-OK | (no se muestra; abre cámara nativa) | — |

### W-M05 — Catálogo del punto

**Propósito:** ver y editar título, descripción, comentarios de fotos y valores de campos del punto. Acceso por doble-tap.

**Elementos UI:**
- Header con coordenadas + acción "Ir al mapa".
- Editor de título.
- Editor de descripción (multiline).
- Sección de **valores de plantilla** (renderizado dinámico según `VersiónDePlantilla` del relevamiento).
- Galería de fotos del punto:
  - Cada foto con thumb, comentario editable inline y acción "Eliminar" (si permite por permisos).
- Pie con metadata de origen: "Creado por X el [fecha] desde [móvil/web], modo [detenido/móvil]".
- Botón "Guardar".

**Estados:**
- **Modo lectura:** si el usuario no tiene permiso de edición ([RN-01](../02_especificacion_funcional/reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md)), todos los campos en solo lectura con tooltip "Solo el dueño del relevamiento o el creador del punto pueden editar".
- **Validación fallida:** campo resaltado con mensaje específico de la plantilla.

### W-M06 — Panel de estado de sincronización (móvil)

**Propósito:** transparencia del estado de sync; reintento manual.

**Elementos UI:**
- Resumen: "Última sync: hace X · Pendientes: N · Conflictos: M".
- Lista de operaciones pendientes agrupadas por tipo (punto, foto, edición) con conteo y ícono de estado.
- Botón **"Sincronizar ahora"**.
- Botón **"Reintentar fallidas"** (visible si hay operaciones en error).
- Link "Ver conflictos" → abre la web equivalente o un detalle nativo según diseño final.

**Estados:**
- **Vacío (todo sincronizado):** check verde + "Todo al día".
- **En curso:** progress bar con conteo "Subiendo X de Y".
- **Error de red:** mensaje "Sin conexión. Reintentaremos automáticamente."

---

## 2. App web (Blazor Server + MudBlazor)

### W-W01 — Login

Idéntico a W-M01 pero en layout web. Disponible para todos los roles `activo`.

### W-W02 — Wizard de primer arranque (admin raíz)

**Propósito:** [CU-02](../02_especificacion_funcional/casos-de-uso/CU-02-configurar-storage_v1.0.md).

**Pasos:**
1. Bienvenida + explicación del proceso.
2. Elección del tipo de storage (radio cards: Local / S3 / FTP / SFTP).
3. Formulario de credenciales según el tipo.
4. Validación de conexión + feedback.
5. Confirmación + redirección al dashboard.

**Estados de error:** mensaje específico según el adaptador (host inalcanzable, credenciales inválidas, sin permisos de escritura).

### W-W03 — Dashboard del rol

**Propósito:** página principal post-login, adaptada por rol.

**Variantes:**
- **Admin raíz:** widgets "Solicitudes pendientes (jefes)", "Configuración", "Áreas", "Usuarios".
- **Jefe de área:** "Mis relevamientos abiertos", "Solicitudes pendientes (relevadores)", "Conflictos pendientes", "Plantillas".
- **Relevador:** "Mis relevamientos", "Donde colaboro", "Conflictos en mis relevamientos".

### W-W04 — Listado de relevamientos

**Propósito:** [CU-05](../02_especificacion_funcional/casos-de-uso/CU-05-asignar-colaboradores-y-ciclo-vida_v1.0.md).

**Elementos UI:**
- Filtros laterales: área (jefes ven la suya), estado, fecha, etiquetas.
- Tabla con columnas: nombre, dueño, colaboradores, plantilla, # puntos, conflictos, estado, fecha.
- Acciones en cada fila: Abrir, Editar metadatos, Cerrar/Reabrir, Eliminar (solo dueño/jefe).
- Botón "Nuevo relevamiento".

**Estado vacío:** "No hay relevamientos con esos filtros".

### W-W05 — Detalle del relevamiento

**Propósito:** [CU-10](../02_especificacion_funcional/casos-de-uso/CU-10-revisar-y-editar-relevamiento-web_v1.0.md).

**Layout 3 zonas:**
- **Izquierda:** lista de puntos con scroll, sortable y filtrable.
- **Centro (mapa):** OpenStreetMap con marcadores diferenciados por colaborador, leyenda, filtros (mis/todos), indicador de actividad reciente (24h).
- **Derecha:** catálogo de fotos con dos vistas alternativas (por punto / plana).

**Click en punto** abre **W-W06**.

### W-W06 — Panel del Punto

**Propósito:** ver detalle, editar campos, ver histórico ([CU-10](../02_especificacion_funcional/casos-de-uso/CU-10-revisar-y-editar-relevamiento-web_v1.0.md), [CU-12](../02_especificacion_funcional/casos-de-uso/CU-12-consultar-trazabilidad-punto_v1.0.md)).

**Elementos UI:**
- Tabs: "Datos" / "Fotos" / "Trazabilidad".
- **Tab Datos:** título, descripción, campos de plantilla (renderizado dinámico).
- **Tab Fotos:** galería con comentarios editables por foto + link a mapa.
- **Tab Trazabilidad:** timeline cronológica de eventos con autor, timestamp, campo, valores antes/después; filtros por autor, tipo y rango de fechas.

### W-W07 — Cargar lote de fotos

**Propósito:** [CU-09](../02_especificacion_funcional/casos-de-uso/CU-09-cargar-lote-fotos-web_v1.0.md).

**Pasos:**
1. Seleccionar relevamiento de destino (si no se viene desde su detalle).
2. Elegir modo de agrupación (Detenido / Móvil con radio).
3. Drag-and-drop de archivos.
4. Procesamiento + barra de progreso.
5. Resumen: N puntos creados, M fotos pendientes de georreferenciar.

**Pantalla derivada — Cola de fotos pendientes:**
- Lista de fotos sin GPS.
- Por cada foto: thumb, opciones "Ingresar coordenadas" (form lat/lng) o "Picker en mapa".

### W-W08 — Panel de conflictos

**Propósito:** resolución manual de conflictos ([CU-08](../02_especificacion_funcional/casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md), [CU-11](../02_especificacion_funcional/casos-de-uso/CU-11-resolver-candidato-fusion_v1.0.md)).

**Elementos UI:**
- Tabs por categoría: "Sobrescrituras", "Eliminaciones con actividad", "Capturas post-cierre", "Candidatos a fusión".
- Lista por categoría con resumen y acción "Revisar".
- Cada acción abre una pantalla específica (ver W-W09 para fusión).

### W-W09 — Revisión de candidato a fusión

**Propósito:** [CU-11](../02_especificacion_funcional/casos-de-uso/CU-11-resolver-candidato-fusion_v1.0.md).

**Elementos UI:**
- Mini-mapa con ambos puntos resaltados, línea entre ellos y distancia exacta.
- Dos columnas con las fotos de cada punto (lado a lado).
- Tabla comparativa de campos divergentes con selector "valor que prevalece".
- Acciones: **"Fusionar"** (abre diálogo con elección de posición resultante) o **"Mantener separados"**.

### W-W10 — Editor de plantilla

**Propósito:** [CU-03](../02_especificacion_funcional/casos-de-uso/CU-03-crear-versionar-plantilla_v1.0.md).

**Elementos UI:**
- Selector de plantilla padre (raíz o cualquier hija).
- Tabs: Campos / Parámetros de captura / Validación.
- Lista de campos con indicador "heredado" / "propio" / "no aplica".
- Acciones: agregar campo, editar atributos visuales, marcar "no aplica", validar.
- Estados: borrador / publicada (lectura).
- Botones "Guardar borrador", "Publicar versión".

### W-W11 — Configuración del sistema (admin raíz)

**Propósito:** reconfigurar storage ([CU-02](../02_especificacion_funcional/casos-de-uso/CU-02-configurar-storage_v1.0.md)) + gestión de jefes / áreas.

**Tabs:**
- Storage (configuración actual + cambiar).
- Jefes de área (lista + acciones inhabilitar/dar de baja).
- Áreas (CRUD).

---

## 3. Patrones transversales

### Estados vacíos
Siempre incluyen ilustración / ícono + mensaje contextual + CTA de salida ("Crear el primero", "Ir a la documentación", etc.).

### Estados de carga
Skeletons en listados, spinners en operaciones puntuales, barras de progreso en uploads.

### Estados de error
Banners contextuales en línea para errores recuperables; modales para errores que requieren acción del usuario; mensajes con sugerencia concreta de cómo recuperar.

### Confirmaciones destructivas
Doble confirmación textual ("Escribí ELIMINAR para confirmar") solo para baja de jefe y eliminación de relevamiento. Resto: confirmación simple con resumen del impacto.

---

## 4. Trazabilidad

| Wireframe | CU origen |
|---|---|
| W-M01, W-W01 | CU-01 |
| W-W02, W-W11 | CU-02 |
| W-W10 | CU-03 |
| W-W04 | CU-04, CU-05 |
| W-M03, W-M04 | CU-06 |
| W-M05 | CU-07 |
| W-M06 | CU-08 |
| W-W07 | CU-09 |
| W-W05, W-W06 | CU-10 |
| W-W08, W-W09 | CU-08, CU-11 |
| W-W06 (tab Trazabilidad) | CU-12 |

---

**Fin del documento — wireframes-descripcion_v1.0.md**
