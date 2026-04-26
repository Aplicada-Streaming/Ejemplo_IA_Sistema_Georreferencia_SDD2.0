**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** ADR-01-monolito-modular-vs-microservicios_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-05 via orquestador

---

# ADR-01 — Monolito modular + workers vs. microservicios

**Estado:** Aceptado.

## Contexto

El cliente trajo predefinida una preferencia por **microservicios completos**. Durante la conversación de relevamiento se aclaró que la justificación de fondo era *"poder cambiar el frontend a React en el futuro"*. Esa portabilidad no depende de la granularidad del backend: se obtiene con una API REST limpia y bien diseñada (con OpenAPI versionado), independientemente de si el backend es uno o varios procesos.

El alcance del proyecto (una organización, un dominio coherente, equipo único) no presenta condiciones que justifiquen el costo operativo de microservicios:

- Deploy coordinado entre servicios.
- Observabilidad distribuida.
- Transacciones cross-service.
- Complejidad de testing local con scripts `.bat`.

## Decisión

Backend implementado como **monolito modular** organizado internamente en módulos con responsabilidades acotadas (Identity, Templates, Surveys, Points, Photos, Sync, Storage, SystemConfig). **Workers separados** como procesos independientes para cargas asincrónicas (procesamiento de imágenes y sincronización). API REST como contrato público versionado con OpenAPI.

Cada módulo respeta la regla de **no acceder a tablas de otros módulos**; la comunicación entre módulos se hace por interfaces de dominio (puertos hexagonales) o eventos internos.

## Consecuencias positivas

- **Costo operativo bajo** durante el MVP: un único proceso de API + dos workers, sin infraestructura distribuida.
- **Levantamiento local trivial** con `.bat` (cumple RNF-06 y `PROJECT-BRIEF` Sec. 9.1).
- **Refactor a microservicios viable** si un módulo lo justifica: la regla de no compartir tablas y los puertos hexagonales mantienen los módulos extraíbles.
- **Transacciones de dominio simples**: una sola DB, sin two-phase commit ni saga management.
- **Observabilidad simplificada**: trazas de un proceso son trivialmente comprensibles.

## Consecuencias negativas

- **Escalado horizontal heterogéneo limitado:** todos los módulos escalan juntos. Si un módulo (e.g. Sync) requiere más recursos, todo el monolito se replica.
- **Riesgo de erosión de los límites modulares** si la disciplina cae: un atajo accediendo a tablas ajenas degradaría la modularidad. Se mitiga con análisis estático y revisión de PRs.
- **Acoplamiento a la elección de SQL Server** para todos los módulos. Si en el futuro un módulo requiere otro motor, debe extraerse.

## Alternativas consideradas

1. **Microservicios completos**, como pidió originalmente el cliente. Descartada por overhead injustificado para el alcance, una vez clarificada la motivación real (portabilidad de frontend, que no depende de la granularidad del backend).
2. **Monolito sin separación modular interna.** Descartada porque dificultaría tests, mantenimiento y la futura extracción de módulos.
3. **Monolito + un único worker generalista.** Descartada porque mezclar imagen y sync en un mismo worker complica la observabilidad y el escalado independiente futuro.

## Trazabilidad

- Decisión heredada del intake: DD-01, DD-02, DD-03, DD-16 (`PROJECT-BRIEF` Sec. 4).
- RNF-05, RNF-06 (`PROJECT-README` Sec. 6).

---

**Fin del documento — ADR-01-monolito-modular-vs-microservicios_v1.0.md**
