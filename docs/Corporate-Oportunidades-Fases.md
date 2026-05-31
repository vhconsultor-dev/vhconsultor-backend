# Módulo de Oportunidades (Corporate) — Plan por fases

**Para quién es este documento:** dueño del producto, gerencia o quien decide si se construye cada parte.  
**Objetivo:** explicar *qué* se va a entregar en cada fase, en lenguaje sencillo, sin detalles de programación.

---

## ¿De qué se trata todo esto?

Hoy en Corporate ya existen **Leads**: personas o marcas que dejaron sus datos (formulario web o carga manual). Eso es el primer contacto.

Cuando alguien del equipo ve **interés real** (quiere propuesta, quiere reunión, etc.), ese lead debe pasar a algo más formal: una **Oportunidad de negocio**.

Una oportunidad permite:

- Saber **quién la lleva** (colaborador responsable).
- Saber **quién la supervisa** (jefe o visor que puede ver y comentar).
- Llevar **etapas** del proceso (primer contacto, propuesta, negociación, etc.) **sin obligar un orden fijo** — se puede saltar etapas si el negocio lo pide.
- Registrar **seguimientos con evidencia** (captura de WhatsApp, correo, PDF de propuesta, etc.).
- Cerrar como **negocio ganado** (y vincular el contrato) o **negocio perdido** (con motivo).
- Conversar en la oportunidad con **comentarios**, menciones con **@** y avisos en tiempo real cuando corresponda.

**Importante:** no se hará todo de golpe. Cada fase entrega valor usable y se puede aprobar o pausar antes de la siguiente.

---

## Resumen visual del recorrido

```
Lead  →  [Convertir]  →  Oportunidad abierta  →  Seguimientos + etapas  →  Ganada o Perdida
                              ↓
                         Comentarios del equipo (supervisor, @menciones, avisos)
```

---

## Fase 1 — Convertir leads y ver oportunidades básicas

### ¿Qué podrá hacer el usuario?

- Desde un **lead**, un botón del tipo **“Convertir a oportunidad”**.
- Al convertir:
  - Se crea la oportunidad con los datos del lead (nombre, marca, correo, teléfono, plataforma, etc.).
  - Se define **quién es el responsable** (colaborador que la lleva).
  - Se define **quién es el supervisor / visor** (persona que puede ver esa oportunidad aunque no sea la responsable del día a día).
  - El lead queda marcado como **ya convertido** (no se puede convertir dos veces el mismo lead).
- Pantalla de **lista de oportunidades**:
  - El colaborador ve **solo las suyas** (donde es responsable).
  - El supervisor ve **las que supervisa** y, según configuración simple, las de su equipo.
- Pantalla de **detalle** de una oportunidad: datos del contacto, responsable, supervisor, fecha de creación, estado (**Abierta**, más adelante Ganada/Perdida).

### ¿Qué NO incluye esta fase?

- Etapas del proceso comercial.
- Seguimientos con evidencia obligatoria.
- Comentarios, @menciones ni alertas en vivo.
- Contratos ni cierre ganado/perdido.

### ¿Por qué empezar aquí?

Es el cimiento: sin conversión lead → oportunidad y sin “quién la lleva”, lo demás no tiene sentido.

### Entregable para validar

> “Puedo convertir un lead en oportunidad, asignar responsable y supervisor, y cada uno ve su listado.”

---

## Fase 2 — Etapas y seguimientos con evidencia

### ¿Qué podrá hacer el usuario?

- Cada oportunidad tiene una **etapa actual**, por ejemplo:
  - Primer contacto  
  - Seguimiento  
  - Propuesta enviada  
  - Seguimiento de propuesta  
  - Negociación  
  - Formalización  
- **No es obligatorio pasar por todas en orden.** Se puede saltar (ej.: de primer contacto directo a propuesta enviada).
- **Seguimientos:** acciones que el responsable debe registrar para avanzar o documentar el trabajo:
  - Texto de qué hizo (ej.: “Envié propuesta por correo”).
  - **Al menos un archivo adjunto obligatorio** (captura, PDF, etc.).
- Al **convertir** un lead a oportunidad, el sistema crea automáticamente el primer seguimiento pendiente: **“Primer contacto el mismo día”**.
- **Alertas simples:** en la lista o en un contador, el colaborador ve cuántos seguimientos tiene **pendientes** (especialmente los de hoy).
- Para **cambiar de etapa**, el sistema exige que quede registrado el seguimiento correspondiente (no se mueve “en vacío”).

### ¿Qué NO incluye?

- Comentarios libres del supervisor.
- Cierre como ganada/perdida con contrato.
- Notificaciones en tiempo real (SignalR).

### ¿Por qué esta fase?

Aquí está el control operativo: el equipo demuestra con evidencia que hizo el seguimiento, y la gerencia ve en qué etapa está cada negocio.

### Entregable para validar

> “Veo pendientes del día, registro seguimientos con archivo, cambio de etapa cuando corresponde, y puedo saltar etapas si hace falta.”

---

## Fase 3 — Cerrar negocio: ganada o perdida (+ contrato)

### ¿Qué podrá hacer el usuario?

- **Marcar como perdida:**
  - La oportunidad deja de estar abierta.
  - Se guarda **motivo** (texto o lista corta: sin presupuesto, eligió competencia, sin respuesta, etc.).
  - El historial de seguimientos y etapas se conserva (no se borra nada).
- **Marcar como ganada:**
  - La oportunidad pasa a estado **Ganada**.
  - Desde ahí se puede **crear o vincular un contrato** existente, para que quede claro que ese negocio sí se concretó.
  - Opcional en esta fase: crear o elegir el **cliente (Customer)** en el sistema si aún no existía.
- Las oportunidades **ganadas** y **perdidas** siguen consultables en listados con filtros (para reportes simples).

### ¿Qué NO incluye?

- Comentarios y menciones.
- SignalR.

### ¿Por qué esta fase?

Cierra el ciclo comercial: no solo “seguimos hablando”, sino **ganamos** (con contrato asociado) o **perdimos** (con razón).

### Entregable para validar

> “Cierro oportunidades como ganadas con contrato vinculado, o como perdidas con motivo, y puedo consultar el historial.”

---

## Fase 4 — Comentarios, adjuntos y menciones con @ (+ correo)

### ¿Qué podrá hacer el usuario?

- En cualquier oportunidad **abierta** (y consultando ganadas/perdidas en lectura), un **hilo de comentarios**:
  - El **responsable** puede comentar.
  - El **supervisor asignado** puede comentar **en cualquier momento**, tantas veces quiera.
  - Cada comentario puede llevar **archivos adjuntos** (opcionales en comentarios; distinto a los seguimientos donde la evidencia es obligatoria).
- Al escribir un comentario, si pone **@** aparece un buscador de **usuarios de Corporate** para mencionar a alguien.
- La persona mencionada recibe un **correo** con el comentario y un enlace para abrir la oportunidad (usando el mismo servicio de correos que ya usa el sistema).
- Los comentarios son **independientes** de los seguimientos por etapa: el jefe puede decir “Ojo con el precio” sin que eso cuente como “seguimiento de etapa”.

### ¿Qué NO incluye?

- Aviso instantáneo en pantalla mientras navegas (eso es la Fase 5).
- Permisos avanzados por roles (RBAC detallado) — se deja para después.

### ¿Por qué esta fase?

Mejora la coordinación jefe–colaborador sin mezclar “charla interna” con “evidencia de venta”.

### Entregable para validar

> “El supervisor comenta y adjunta cuando quiera; puedo @mencionar a un compañero y le llega el correo.”

---

## Fase 5 — Avisos en tiempo real (SignalR)

### ¿Qué podrá hacer el usuario?

- Si el colaborador tiene abierta la **lista** o el **detalle** de una oportunidad:
  - Cuando el supervisor (u otro usuario autorizado) **publica un comentario**, aparece **al momento** sin recargar la página (aviso o el comentario nuevo en el hilo).
- Si lo mencionan con **@** mientras están conectados:
  - Pueden ver un aviso inmediato además del correo de la Fase 4.
- Comportamiento pensado para **no depender solo del correo**: el correo sigue para quien no está en la app; SignalR para quien sí está trabajando en pantalla.

### ¿Qué NO incluye (por ahora)?

- App móvil push.
- Centro de notificaciones global muy complejo (se puede simplificar: solo toast + actualizar el hilo).

### ¿Por qué al final?

Requiere infraestructura nueva (conexión en vivo). Las fases 1–4 ya aportan valor sin esto; esta fase mejora la experiencia, no bloquea el negocio.

### Entregable para validar

> “Estoy viendo una oportunidad y el comentario del jefe aparece solo, sin refrescar.”

---

## Fase 6 (opcional, después) — Permisos avanzados y reportes

Solo si el negocio lo pide más adelante:

- **RBAC** fino (quién puede convertir leads, quién ve todas las oportunidades de la empresa, exportar, etc.).
- **Reportes:** oportunidades por etapa, tasa de cierre, tiempo promedio por etapa, motivos de pérdida.
- **Reasignar** responsable o supervisor en bloque.
- Recordatorios por correo automáticos de seguimientos vencidos.

Esta fase **no es obligatoria** para operar el módulo.

---

## Tabla resumen para decidir

| Fase | En una frase | ¿Operación real sin la siguiente? |
|------|----------------|-----------------------------------|
| **1** | Lead → oportunidad, responsable y supervisor, listados | Parcial (solo registro y asignación) |
| **2** | Etapas, seguimientos con evidencia, pendientes del día | Sí, para vender con control |
| **3** | Ganada con contrato / perdida con motivo | Sí, ciclo comercial cerrado |
| **4** | Comentarios, @ y correo | Sí, mejora coordinación |
| **5** | Avisos en vivo en pantalla | Mejora UX; no bloquea |
| **6** | RBAC y reportes avanzados | Opcional |

---

## Decisiones que el dueño del producto puede marcar (sí / no / después)

Antes o durante la Fase 1, conviene definir:

1. **Al convertir un lead**, ¿el responsable es siempre quien convierte, o un admin elige en un formulario?  
2. **Supervisor:** ¿siempre una persona fija por oportunidad, o a veces el mismo gerente para todas?  
3. **Al ganar**, ¿siempre se crea cliente nuevo o a veces ya existe y solo se elige?  
4. **Motivos de pérdida:** ¿texto libre o lista cerrada (recomendado: lista corta + “otro”)?  
5. **Fase 5 (tiempo real):** ¿es prioridad alta o puede esperar hasta que 1–4 estén en producción?

---

## Cómo usar este documento en una reunión

1. Leer la **tabla resumen**.  
2. Por cada fase, preguntar: *“¿Esto es lo que necesitamos? ¿Aprobamos construirla?”*  
3. Anotar en la columna **sí / no / después** de la tabla de decisiones.  
4. El equipo técnico implementa según el documento complementario: `Corporate-Oportunidades-Fases-Tecnico.md`.

---

*Documento de negocio — VH Consultor Corporate — Oportunidades. Versión 1.*
