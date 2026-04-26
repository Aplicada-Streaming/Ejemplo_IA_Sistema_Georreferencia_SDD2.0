**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** flujos-de-usuario_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-03 via orquestador

---

# Flujos de Usuario

Cada flujo se origina en uno o más Casos de Uso de SA-02. La descripción se centra en la experiencia del actor: qué ve, qué decide, cómo recupera de errores. Los detalles técnicos (estados, validaciones) están en los CUs y en SA-05.

---

## 1. Mapa de actores y flujos

| Actor | Frente | Flujos |
|---|---|---|
| Admin raíz | Web | F-A1 Wizard primer arranque · F-A2 Aceptar jefes · F-A3 Reconfigurar storage · F-A4 Inhabilitar/dar de baja jefe |
| Jefe de área | Web | F-J1 Aceptar relevadores · F-J2 Crear plantilla · F-J3 Gestionar relevamientos · F-J4 Asignar colaboradores · F-J5 Revisar y editar relevamiento · F-J6 Resolver candidato a fusión · F-J7 Resolver conflictos del panel |
| Relevador (dueño) | Web + Móvil | F-R1 Registrarse · F-R2 Crear relevamiento · F-R3 Capturar punto en campo · F-R4 Editar catálogo en móvil · F-R5 Sincronizar · F-R6 Cerrar/reabrir relevamiento · F-R7 Eliminar relevamiento · F-R8 Cargar lote desde web · F-R9 Revisar desde web |
| Colaborador asignado | Web + Móvil | F-C1 Aceptar asignación · F-C2 Capturar puntos en relevamiento ajeno · F-C3 Editar sus propios puntos · F-C4 Ver puntos del dueño en mapa colaborativo |

---

## 2. Flujos del Admin raíz

### F-A1 — Wizard de primer arranque

**Origen:** [CU-02](../02_especificacion_funcional/casos-de-uso/CU-02-configurar-storage_v1.0.md)

```
[Login admin raíz] → [Sistema detecta sin config] → [Pantalla wizard]
   │
   ▼
[Selección de tipo: local / S3 / FTP / SFTP]
   │
   ▼
[Formulario de credenciales del adaptador elegido]
   │
   ▼
[Botón "Validar conexión"]
   ├── ÉXITO → [Persistir + ir a dashboard]
   └── ERROR → [Mostrar mensaje específico + permitir corregir]
```

**Estados de error:**
- Path local no existe / sin permisos.
- Bucket S3 inaccesible / credenciales inválidas.
- Servidor FTP/SFTP inalcanzable.

**Estados especiales:** si el admin cancela, el sistema queda sin storage y bloquea creación de relevamientos hasta completar el wizard.

### F-A2 — Aceptar nuevos jefes de área

**Origen:** [CU-01](../02_especificacion_funcional/casos-de-uso/CU-01-iniciar-sesion-y-registro-jerarquico_v1.0.md)

```
[Dashboard admin] → [Sección "Solicitudes pendientes"]
   │
   ▼
[Lista de jefes en estado pendiente_aceptacion]
   │
   ▼ (selecciona uno)
[Detalle del solicitante: email, nombre, área]
   │
   ├── [Aceptar] → estado activo → notificar al jefe
   └── [Rechazar con motivo] → estado dado_de_baja
```

### F-A3 — Reconfigurar storage

**Origen:** [CU-02](../02_especificacion_funcional/casos-de-uso/CU-02-configurar-storage_v1.0.md) (flujo alternativo)

Idéntico a F-A1 pero accedido desde "Configuración del sistema" en lugar del wizard.

### F-A4 — Inhabilitar / dar de baja jefe

**Origen:** [CU-01](../02_especificacion_funcional/casos-de-uso/CU-01-iniciar-sesion-y-registro-jerarquico_v1.0.md), [RN-11](../02_especificacion_funcional/reglas-de-negocio/RN-11-aceptacion-jerarquica-y-movil-restringido_v1.0.md)

```
[Lista de jefes activos]
   │
   ▼ (selecciona uno)
[Detalle del jefe]
   │
   ├── [Inhabilitar] → confirmación → estado inhabilitado (reversible)
   └── [Dar de baja]  → confirmación con advertencia → estado dado_de_baja (terminal)
```

**Mensaje de confirmación de baja:** "Esta acción es irreversible. Los datos del jefe se preservan en histórico bajo trazabilidad. ¿Continuar?"

---

## 3. Flujos del Jefe de área

### F-J2 — Crear plantilla con herencia

**Origen:** [CU-03](../02_especificacion_funcional/casos-de-uso/CU-03-crear-versionar-plantilla_v1.0.md)

```
[Módulo plantillas] → [Nueva plantilla]
   │
   ▼
[Selección de plantilla padre (raíz por default)]
   │
   ▼
[Editor de plantilla]
   │ ┌─ Tab "Campos": agregar / sobrescribir / marcar "no aplica"
   │ ├─ Tab "Parámetros de captura": GPS timeout, accuracy, radio modo móvil, compresión foto
   │ └─ Tab "Validación": min/max, requerido, opciones de selección
   │
   ▼
[Botón Guardar borrador]
   │
   ▼
[Botón Publicar] → versión inmutable disponible para nuevos relevamientos
```

**Estados de error:**
- E1: Cambio de tipo de campo heredado → mensaje "El tipo de un campo heredado no se puede modificar".
- E2: Eliminar campo heredado → "Solo puede marcarlo como 'no aplica'".

### F-J5 — Revisar y editar relevamiento

**Origen:** [CU-10](../02_especificacion_funcional/casos-de-uso/CU-10-revisar-y-editar-relevamiento-web_v1.0.md)

```
[Listado de relevamientos] → [Filtrar por área/estado/etiqueta]
   │
   ▼
[Detalle del relevamiento: layout 3 zonas]
   │ ┌─ Mapa colaborativo (con filtro mis/todos)
   │ ├─ Lista de puntos (sortable)
   │ └─ Catálogo de fotos (vista por punto / plana)
   │
   ▼ (click en punto / foto)
[Panel lateral del Punto]
   │ ├─ Título, descripción
   │ ├─ Valores de plantilla (renderizado dinámico)
   │ ├─ Galería de fotos con comentarios
   │ └─ Pestaña "Trazabilidad" (CU-12)
   │
   ▼
[Editar] → [Guardar] → eventos persistidos
```

**Estado vacío:** "Aún no hay puntos en este relevamiento". Botón "Cargar lote desde web" si aplica.

### F-J6 — Resolver candidato a fusión

**Origen:** [CU-11](../02_especificacion_funcional/casos-de-uso/CU-11-resolver-candidato-fusion_v1.0.md)

```
[Panel de conflictos] → [Sección "Candidatos a fusión"]
   │
   ▼ (selecciona par)
[Pantalla de revisión]
   │ ┌─ Mapa con ambos puntos resaltados, distancia
   │ ├─ Listado lado a lado de fotos de cada punto
   │ └─ Tabla comparativa de campos divergentes
   │
   ▼
[Acción: Fusionar] → diálogo
   │ ├─ Posición resultante: centroide / A / B
   │ └─ Selector de valor por campo divergente
   │   → [Confirmar] → punto consolidado + evento merged
   │
[Acción: Mantener separados] → confirmación
   │
   └─→ marca persistente, no se vuelve a proponer
```

---

## 4. Flujos del Relevador (dueño)

### F-R3 — Capturar punto en campo (móvil)

**Origen:** [CU-06](../02_especificacion_funcional/casos-de-uso/CU-06-capturar-punto-georreferenciado_v1.0.md), [CU-07](../02_especificacion_funcional/casos-de-uso/CU-07-editar-catalogo-punto-desde-movil_v1.0.md)

```
[Lista de relevamientos asignados] → [Detalle del relevamiento]
   │
   ▼
[Pantalla principal de captura]
   │ ┌─ Mapa con marcadores (color por colaborador)
   │ ├─ Toggle de modo: detenido / móvil
   │ ├─ Indicador de modo activo + radio (si móvil)
   │ ├─ Botón flotante "Cámara"
   │ ├─ Selector de marcador actual
   │ └─ Badge de sync ("Sincronizado · N cambios · M conflictos")
   │
   ▼ (tap en cámara)
[Diálogo unificado de captura]
   │ S0 Verificando permisos
   │ S1-* Permisos denegados → ir a configuración
   │ S2 Obteniendo GPS (contador)
   │ S3-OK / S3-LOWACC / S3-TIMEOUT / S3-NOSIGNAL
   │
   ▼ (S3-OK)
[Cámara nativa] → [Foto tomada]
   │
   ▼
[Volver al mapa con punto/foto persistido localmente]
   │
   ▼ (doble-tap en marcador)
[Catálogo del punto: edición de título, descripción, comentarios]
```

**Estados de error / casos especiales:**
- Permiso denegado → modal con CTA "Ir a configuración".
- GPS sin fix por timeout → "Reintentar / Cancelar / Ingresar manualmente" (este último según parámetro de plantilla).
- Almacenamiento lleno → bloqueo con mensaje y sugerencia.

### F-R5 — Sincronizar

**Origen:** [CU-08](../02_especificacion_funcional/casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md)

```
[Cualquier pantalla] → tap en badge de sync
   │
   ▼
[Panel de estado de sync]
   │ ├─ Operaciones pendientes: N por entidad (puntos, fotos, ediciones)
   │ ├─ Última sync: hace X minutos
   │ ├─ Conflictos para revisar: M
   │ └─ Botón [Sincronizar ahora] / [Reintentar fallidas]
   │
   ▼
[Progreso (subir + bajar)]
   │
   ▼
[Resumen post-sync]
   │ "Sincronizado · N cambios · M conflictos para revisar"
   │ [Ver conflictos] → panel web (en móvil deeplink al equivalente)
```

### F-R6 — Cerrar / reabrir relevamiento

**Origen:** [CU-05](../02_especificacion_funcional/casos-de-uso/CU-05-asignar-colaboradores-y-ciclo-vida_v1.0.md)

Acción accesible desde el menú del relevamiento (mobile + web). Confirmación con advertencia. Tras reabrir post-cierre por capturas pendientes, mostrar "Capturas posteriores al cierre han sido aplicadas" con detalle.

---

## 5. Flujos del Colaborador asignado

### F-C2 — Capturar puntos en relevamiento ajeno

Idéntico a F-R3 con dos diferencias visuales:
- En la lista de relevamientos: indicador "Soy colaborador" en lugar de "Soy dueño".
- En el catálogo del punto: si el punto fue creado por otro colaborador o el dueño, el modo de edición está deshabilitado con tooltip explicativo ("Solo el dueño y el creador pueden editar este punto").

### F-C4 — Ver puntos del dueño en mapa colaborativo

**Origen:** [CU-10](../02_especificacion_funcional/casos-de-uso/CU-10-revisar-y-editar-relevamiento-web_v1.0.md), filtros del mapa.

```
[Mapa] → [Filtros]
   │ ├─ "Ver solo mis puntos"
   │ ├─ "Ver todos los puntos del relevamiento" (default)
   │ └─ "Ver actividad reciente (24h)"
   │
   ▼
Mapa renderizado con leyenda de colaboradores.
```

---

## 6. Mapeo de flujos a CUs

| Flujo | CU origen | RFs cubiertos |
|---|---|---|
| F-A1 | CU-02 | RF-60 |
| F-A2 | CU-01 | RF-56 |
| F-A3 | CU-02 | RF-61 |
| F-A4 | CU-01 + RN-11 | RF-57 |
| F-J1 | CU-01 | RF-56 |
| F-J2 | CU-03 | RF-07 a RF-13 |
| F-J3 | CU-04, CU-05 | RF-01 a RF-06 |
| F-J4 | CU-05 | RF-03 |
| F-J5 | CU-10 | RF-29 a RF-33, RF-52 a RF-54 |
| F-J6 | CU-11 | RF-44 a RF-48 |
| F-J7 | CU-08 | RF-42, RF-43 |
| F-R1 | CU-01 | RF-55 |
| F-R2 | CU-04 | RF-01 |
| F-R3 | CU-06, CU-07 | RF-14 a RF-23 |
| F-R4 | CU-07 | RF-19 |
| F-R5 | CU-08 | RF-34 a RF-43 |
| F-R6 | CU-05 | RF-04 |
| F-R7 | CU-05 | RF-01 |
| F-R8 | CU-09 | RF-24 a RF-28 |
| F-R9 | CU-10 | RF-29 a RF-33 |
| F-C1 | CU-01 | RF-56 |
| F-C2 | CU-06 | RF-14 a RF-22 |
| F-C3 | CU-07 | RF-19, RF-58 |
| F-C4 | CU-10 | RF-52, RF-53 |

---

**Fin del documento — flujos-de-usuario_v1.0.md**
