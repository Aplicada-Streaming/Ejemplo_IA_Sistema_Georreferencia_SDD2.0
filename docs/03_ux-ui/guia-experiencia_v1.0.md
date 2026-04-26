**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** guia-experiencia_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-03 via orquestador

---

# Guía de Experiencia — Principios, patrones y estilo

## 1. Principios UX

Estos principios derivan de la visión y el contexto del cliente. Se aplican como criterios de decisión cuando aparece un trade-off de diseño.

### P1. El relevamiento de campo manda
Toda decisión que afecte al relevador en campo se evalúa primero por su impacto sobre él. Latencia, número de taps por captura, claridad de mensajes con luz solar, confiabilidad sin red. La web de gabinete es importante pero secundaria.

### P2. Offline es la norma
Cualquier flujo crítico debe funcionar sin red. La red mejora la experiencia, no la habilita. Los indicadores de "modo offline" no son alarmas; son estado normal.

### P3. Transparencia sobre los conflictos
El usuario nunca pierde algo silenciosamente. Si LWW resuelve automático, hay notificación post-sync. Si hay candidato a fusión, va al panel para revisión. Si se rechaza una captura post-cierre, el dueño se entera con opción de reabrir.

### P4. Mismo modelo, dos frentes
La web y el móvil ven el mismo dominio (relevamientos, puntos, fotos). El usuario debe sentir continuidad: lo que captura en móvil aparece igual en web. Diferentes medios, mismas etiquetas y mismo lenguaje.

### P5. Plantillas son configuración, no decoración
La plantilla controla qué campos existen, cómo se renderizan, qué precisión de GPS se exige. Cambiar de plantilla cambia la experiencia. El sistema rinde plantillas dinámicamente sin pretender uniformar lo que es legítimamente distinto.

### P6. Cada acción destructiva pasa por confirmación
Eliminar relevamiento, dar de baja jefe, descartar capturas post-cierre, marcar candidatos como "no duplicados". Confirmación con resumen del impacto.

---

## 2. Patrones de interacción

### 2.1. Captura en móvil

- **Una mano, un tap.** El gesto crítico (tomar foto) es accesible con el pulgar sin cambiar de pantalla.
- **GPS antes que cámara.** El diálogo unificado obtiene fix antes de abrir cámara para evitar fotos huérfanas.
- **Modo siempre visible.** El toggle Detenido/Móvil está siempre a la vista para evitar capturas en el modo equivocado.
- **Marcador actual destacado.** Si hay un marcador seleccionado, debe ser obvio que la próxima foto se asocia a ese marcador, no a uno nuevo.

### 2.2. Sincronización

- **Badge persistente en lugar de modal.** El estado de sync no interrumpe el trabajo; es un indicador en la barra superior.
- **Reintentos exponenciales transparentes.** El usuario no maneja reintentos; el sistema los hace y solo notifica si llega a `terminal_error`.
- **Resumen post-sync siempre presente.** "Sincronizado · N cambios · M conflictos" tras cada sync exitosa.

### 2.3. Resolución de conflictos

- **Comparación lado a lado.** Cada conflicto manual muestra ambos valores en la misma pantalla.
- **Una decisión a la vez.** El panel se navega por conflicto, no como un bulk-action.
- **Resumen del impacto antes de aplicar.** Antes de fusionar, el usuario ve el resultado proyectado.

### 2.4. Listados y filtros

- **Filtros laterales persistentes** en web; chips horizontales en móvil.
- **Estados vacíos siempre activos.** Si la lista filtra a 0 ítems, el mensaje sugiere quitar filtros o crear el primero.
- **Sortable por columna** en web; sort fijo por fecha en móvil.

### 2.5. Mapa

- **Color por colaborador** con leyenda visible.
- **Filtro mis/todos** siempre accesible sin abrir submenú.
- **Indicador de actividad reciente (24h)** sobre los puntos modificados.

### 2.6. Edición

- **Validaciones inline** mientras se escribe; no esperar al submit.
- **Mensaje de error sobre el campo**, no en la parte superior de la pantalla.
- **Cancel + Save explícitos** en pantallas de edición; no autoguardado en móvil para evitar cambios accidentales.

---

## 3. Guía de estilo

### 3.1. Stack visual

- **MudBlazor** como librería de componentes (web + móvil híbrido). Brinda consistencia entre frentes.
- **Material Design** como base, con ajustes específicos cuando MudBlazor lo permite.
- **OpenStreetMap** como tile provider (`PROJECT-BRIEF` Sec. 1.5).

### 3.2. Tipografía

- Familia: la default de MudBlazor (sin custom font para evitar costos de carga en móvil).
- Tamaño base: 16 px en web, 14 px en móvil.
- Jerarquía: H1, H2, H3, body, caption.

### 3.3. Color

- **Primario:** [REQUIERE_INFO] color institucional de Vialidad si el cliente lo provee. Default propuesto: tono naranja/ámbar (alta visibilidad en zonas viales) — sujeto a validación.
- **Secundario:** azul de MudBlazor.
- **Estados:** verde (éxito), naranja (warning), rojo (error), gris (neutral / offline).
- **Colaboradores en mapa:** paleta categórica de 8 colores distinguibles, asignada estable por user_id.

### 3.4. Iconografía

- Set por defecto de MudBlazor (Material Icons).
- Marcador de mapa: pin diferenciado por color de colaborador.
- Indicador de actividad reciente: dot pulsante sobre el marcador.

### 3.5. Espaciado y densidad

- Móvil: alta densidad táctil; targets ≥ 48 px.
- Web: densidad normal; tablas con altura 40 px por fila.

---

## 4. Mensajes

### 4.1. Tono

- Voz directa, en segunda persona singular ("Vos podés…"), no "usted".
- Evitar jerga técnica en mensajes de error visibles al usuario.
- Preferir verbos a sustantivos: "Sincronizar" en lugar de "Sincronización".

### 4.2. Mensajes de error

| Situación | Mensaje |
|---|---|
| Login con credenciales inválidas | "El usuario o la contraseña son incorrectos." |
| Login en móvil con rol no relevador | "El acceso móvil está disponible solo para relevadores." |
| GPS sin permiso | "Para georreferenciar las fotos necesitamos permiso de ubicación." |
| GPS con timeout | "No pudimos obtener la posición en X segundos. Probá moverte unos metros y reintentá." |
| Almacenamiento local lleno | "El almacenamiento del dispositivo está lleno. Liberá espacio para continuar." |
| Captura post-cierre | "El relevamiento fue cerrado por el dueño. Tus capturas posteriores están en revisión." |
| Permiso insuficiente para editar | "Solo el dueño del relevamiento o el creador del punto pueden editar este campo." |

### 4.3. Mensajes de éxito

Breves, orientados a la próxima acción posible.

| Situación | Mensaje |
|---|---|
| Sync exitosa | "Sincronizado · N cambios · M conflictos para revisar" |
| Punto creado | "Punto guardado." (toast efímero) |
| Plantilla publicada | "Plantilla publicada como versión X. Disponible para nuevos relevamientos." |

---

## 5. Accesibilidad

- Contraste WCAG AA mínimo en todos los textos.
- Labels asociados a inputs en formularios.
- Foco visible en navegación por teclado en web.
- Alternativa textual para íconos puramente decorativos.
- Soporte de lector de pantalla en flujos críticos web.

> [REQUIERE_INFO] Confirmar nivel de accesibilidad exigido por el cliente. WCAG AA es asumido como default razonable.

---

## 6. Internacionalización

- Idioma único en MVP: **español rioplatense** (textos en formato "vos", como el resto de la documentación).
- Estructura preparada para i18n (archivos `.resx` o equivalente) por si el cliente extiende a otras regiones de habla hispana.
- Formatos de fecha: `DD/MM/YYYY` en visualización; ISO 8601 en backend.
- Coordenadas: lat/lng en grados decimales con precisión 6 (≈ 11 cm).

---

## 7. Performance perceibido

- **Móvil:** captura debe sentirse instantánea (< 200 ms de feedback al tap; el procesamiento real corre en background).
- **Web:** páginas críticas con time-to-interactive ≤ 2 s en red de oficina. Listados con paginación cuando el conteo lo justifique.
- **Sincronización:** progreso visible por entidad; nunca un loader bloqueante sin barra.

---

## 8. Trazabilidad

| Documento upstream | Aporte a la guía |
|---|---|
| [vision-producto](../00_contexto/vision-producto_v1.0.md) | Principios P1 a P5 derivan de los principios de producto |
| `devs/intake/PROJECT-BRIEF.md` Sec. 7 | Patrón del diálogo unificado de captura |
| `devs/intake/PROJECT-BRIEF.md` Sec. 1 | Stack visual (MudBlazor) y mapa (OpenStreetMap) |
| Casos de uso CU-* | Patrones de interacción derivados de cada flujo |

---

## 9. Documentos relacionados (esta sección)

- [Flujos de usuario](flujos-de-usuario_v1.0.md)
- [Wireframes — descripción textual](wireframes-descripcion_v1.0.md)

---

**Fin del documento — guia-experiencia_v1.0.md**
