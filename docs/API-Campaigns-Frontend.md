# Campañas de Correo Corporativas — Guía para Frontend

## Introducción

Este documento describe el módulo de **Campañas de correo** del portal Corporate de VH Consultor: para qué sirve, cómo funciona el flujo completo desde la perspectiva del usuario de negocio, y cómo consumir cada endpoint ya publicado en API Management.

**Base URL (APIM):** `https://vh-apimanagement.azure-api.net/corporate-vh`

**Autenticación:** Todas las peticiones requieren `Authorization: Bearer <token>`.

**Formato de fechas:** UTC en ISO 8601 (ejemplo: `2026-05-31T14:30:00Z`).

**Estructura estándar de respuesta:**

```json
{
  "success": true,
  "message": "Descripción del resultado",
  "data": { }
}
```

En caso de error de validación, `success` será `false` y el mensaje describirá el problema. Los errores 500 incluyen un **Error ID** para soporte técnico.

---

## Tabla de contenidos

1. [¿Qué son las campañas y para qué sirven?](#1-qué-son-las-campañas-y-para-qué-sirven)
2. [Conceptos clave](#2-conceptos-clave)
3. [Flujo completo de una campaña](#3-flujo-completo-de-una-campaña)
4. [Reglas de negocio importantes](#4-reglas-de-negocio-importantes)
5. [Privacidad en el envío de correos](#5-privacidad-en-el-envío-de-correos)
6. [Catálogo de endpoints en APIM](#6-catálogo-de-endpoints-en-apim)
7. [Detalle de cada API](#7-detalle-de-cada-api)
8. [Modelos de datos](#8-modelos-de-datos)
9. [Flujo sugerido para la interfaz de usuario](#9-flujo-sugerido-para-la-interfaz-de-usuario)
10. [Errores frecuentes y cómo manejarlos](#10-errores-frecuentes-y-cómo-manejarlos)

---

## 1. ¿Qué son las campañas y para qué sirven?

Las **campañas de correo** permiten al equipo corporativo de VH Consultor preparar y enviar comunicaciones masivas — pero controladas — a leads y contactos, usando un diseño profesional unificado (plantilla SendGrid) y contenido personalizable por campaña.

### Objetivo principal

Comunicar de forma elegante y consistente con la marca a uno o muchos destinatarios: anuncios, actualizaciones comerciales, invitaciones, material informativo con documentos adjuntos, etc., **sin depender de herramientas externas** y **sin exponer los correos de unos clientes a otros**.

### Qué resuelve para el negocio

| Necesidad | Cómo lo resuelve el módulo |
|-----------|----------------------------|
| Enviar el mismo mensaje a muchos leads | Una campaña, muchos prospectos, un solo clic de envío |
| Reutilizar leads del CRM | Importación desde el listado de leads existente (nombre + correo) |
| Agregar contactos que no son lead | Correos manuales individuales en la lista de prospectos |
| Adjuntar PDFs, brochures, etc. | Múltiples documentos por campaña, almacenados en Azure |
| Personalizar el mensaje | Texto HTML editable por campaña dentro de una plantilla visual premium |
| Saludo personalizado | Nombre y apellido del prospecto se inyectan en el correo |
| Verificar antes de enviar | Correos adicionales al momento del envío (ej. copia al usuario que envía) |
| Historial | Campañas enviadas quedan en estado `Sent` con fecha de envío |
| Trabajo en borrador | Varias campañas pueden estar `Pending` al mismo tiempo |

### Qué NO es este módulo

- No es un programador automático de envíos (la fecha `scheduledAt` es referencia para la UI; el envío se dispara manualmente con el endpoint `/send`).
- No es un editor de la plantilla visual global (eso vive en SendGrid; aquí se edita el **contenido** de cada campaña).
- No reemplaza el módulo de Leads ni de Oportunidades; **los consume** como fuente de destinatarios.

---

## 2. Conceptos clave

### Campaña (`Campaign`)

Una campaña es el contenedor principal. Incluye:

- **Nombre interno** — identifica la campaña en el listado (ej. "Lanzamiento Q2 2026").
- **Asunto del correo** — lo que verá el destinatario en su bandeja de entrada.
- **Contenido del cuerpo** — texto HTML que se inserta dentro de la plantilla SendGrid.
- **Estado** — `Pending` (borrador, editable) o `Sent` (ya enviada, solo lectura).
- **Fecha programada** (opcional) — referencia visual; no dispara envío automático.
- **Fecha de envío** — se registra cuando se ejecuta `/send`.

### Prospecto de campaña (`CampaignProspect`)

Cada fila representa **un destinatario** de esa campaña. Lo esencial es:

| Campo | Importancia |
|-------|-------------|
| **Email** | Obligatorio. Es la dirección a la que se enviará el correo. |
| **FirstName / LastName** | Muy recomendados. Permiten personalizar el saludo en la plantilla ("Dear John Doe,"). |
| **LeadId** | Opcional. Si el prospecto proviene de un lead, se guarda la referencia para trazabilidad. |

Un prospecto **pertenece a una sola campaña**. Si mañana creas otra campaña, puedes volver a incluir al mismo lead o al mismo correo sin problema.

### Adjunto (`CampaignAttachment`)

Documento asociado a la campaña (PDF, imagen, etc.). Se sube a Azure Blob Storage y su URL se guarda en base de datos. **Todos los adjuntos de la campaña se incluyen en cada correo enviado.**

### Lead vs prospecto manual

| Origen | Cuándo usarlo |
|--------|---------------|
| **Desde leads** | El usuario selecciona uno o varios leads del listado corporativo. El sistema extrae automáticamente email, nombre y apellido del lead. |
| **Manual** | El usuario escribe un correo (y opcionalmente nombre/apellido) que no está en leads, o quiere agregar un contacto puntual. |

Ambos métodos alimentan la misma tabla de prospectos de la campaña.

---

## 3. Flujo completo de una campaña

El ciclo de vida natural de una campaña es el siguiente:

```
1. Crear campaña          →  estado Pending, se obtiene campaignId
2. Editar contenido       →  nombre, asunto, cuerpo HTML (opcional: fecha programada)
3. Subir documentos       →  uno o más archivos adjuntos
4. Agregar destinatarios  →  desde leads Y/O correos manuales
5. Revisar                →  listado de prospectos + preview del contenido
6. Enviar                 →  confirmación + correos extra opcionales
7. Campaña enviada        →  estado Sent, ya no editable
```

### Múltiples campañas pendientes

El sistema **permite tener varias campañas en estado `Pending` simultáneamente**. Cada una es independiente: tiene su propio contenido, sus propios adjuntos y su propia lista de prospectos. El equipo puede preparar la campaña de junio y la de julio en paralelo, y enviarlas cuando corresponda.

### Selección múltiple de leads

En la UI, el usuario debe poder:

1. Ver el listado de leads (API de leads existente en Corporate).
2. Seleccionar **varios** con checkbox o selección múltiple.
3. Confirmar → llamar a `POST .../prospects/from-leads` con el array de IDs.

El backend toma de cada lead lo que realmente importa para el correo: **email, firstName, lastName**. Los leads sin email se ignoran silenciosamente.

### Correos manuales e individuales

Además de importar leads, el usuario puede:

- Agregar **un correo suelto** (ej. un partner que no está en leads).
- Agregar **varios correos** en una sola petición.
- Completar nombre y apellido para que el saludo sea personalizado.

Esto se hace con `POST .../prospects` enviando un array de prospectos.

### Correos extra solo al enviar

En la pantalla de confirmación de envío, el usuario puede agregar correos que **no quedan guardados** como prospectos — típicamente su propio correo corporativo para verificar que todo se ve bien. Esto se envía en el body de `POST .../send` como `additionalBccEmails`.

---

## 4. Reglas de negocio importantes

| Regla | Detalle |
|-------|---------|
| **Estados** | Solo existen `Pending` y `Sent`. |
| **Edición** | Nombre, asunto, cuerpo, adjuntos y prospectos solo se modifican en `Pending`. |
| **Email único por campaña** | No puede haber dos prospectos con el mismo correo en la misma campaña. Si se intenta duplicar, se omite y aumenta `skippedCount`. |
| **Mismo correo en otra campaña** | Permitido. Juan puede recibir la campaña de enero y la de marzo. |
| **Envío irreversible** | Una vez enviada (`Sent`), la campaña no se puede editar ni reenviar con los mismos endpoints actuales. |
| **Al menos un destinatario** | Para enviar, debe haber al menos un prospecto **o** un correo en `additionalBccEmails`. |
| **Adjuntos en envío** | Todos los adjuntos vigentes de la campaña van en cada correo individual. |
| **Normalización de email** | Los correos se guardan en minúsculas. `John@Example.com` y `john@example.com` se tratan como el mismo. |

---

## 5. Privacidad en el envío de correos

Este es un requisito crítico del negocio:

> **Ningún destinatario debe poder ver el correo de otro destinatario.**

### Cómo funciona técnicamente

Por cada persona en la lista (prospectos + correos adicionales al enviar), el backend envía **un correo separado** a través de SendGrid con esta estructura:

- **Para (To):** la dirección corporativa de VH Consultor (remitente).
- **Copia oculta (BCC):** el destinatario real (el lead, el contacto manual o el correo extra del usuario).

Así, si envías a 200 personas, se generan 200 envíos individuales. Nadie aparece en CC ni en un To compartido con otros clientes.

### Implicación para la UI

- Mostrar claramente al usuario que el envío es **privado por destinatario**.
- En la pantalla de envío, explicar que los correos adicionales (como el suyo propio) también llegan en copia oculta.
- Mostrar el resumen: "Se enviarán X correos individuales".

---

## 6. Catálogo de endpoints en APIM

| # | Método | URL |
|---|--------|-----|
| 1 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Campaign` |
| 2 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Campaign/{campaignId}` |
| 3 | `POST` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Campaign` |
| 4 | `PUT` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Campaign/{campaignId}` |
| 5 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Campaign/{campaignId}/attachments` |
| 6 | `POST` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Campaign/{campaignId}/attachments` |
| 7 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Campaign/{campaignId}/prospects` |
| 8 | `POST` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Campaign/{campaignId}/prospects` |
| 9 | `POST` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Campaign/{campaignId}/prospects/from-leads` |
| 10 | `DELETE` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Campaign/{campaignId}/prospects/{prospectId}` |
| 11 | `POST` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Campaign/{campaignId}/send` |

---

## 7. Detalle de cada API

---

### 7.1 GET — Listar campañas

**URL:** `GET .../api/corporate/Campaign`

**Propósito:** Obtener el listado de todas las campañas para la pantalla principal. Ideal para mostrar tarjetas o tabla con nombre, estado, fechas y conteo de prospectos (el conteo de prospectos, si se necesita en UI, puede obtenerse con llamadas adicionales o agregarse en frontend cacheando el GET de prospects).

**Parámetros de consulta (query string):**

| Parámetro | Tipo | Obligatorio | Descripción |
|-----------|------|-------------|-------------|
| `status` | string | No | Filtra por estado: `Pending` o `Sent`. Si se omite, devuelve todas. |

**Cuándo usarlo:**

- Al entrar al módulo de campañas.
- Para la pestaña "Pendientes de envío" → `?status=Pending`.
- Para la pestaña "Enviadas" → `?status=Sent`.

**Orden:** Las campañas vienen ordenadas por fecha de creación descendente (más recientes primero).

**Respuesta exitosa — `data`:** Array de objetos campaña (ver [Modelo Campaign](#campaign)).

**Ejemplo de petición:**

```http
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Campaign?status=Pending
Authorization: Bearer <token>
```

---

### 7.2 GET — Detalle de una campaña

**URL:** `GET .../api/corporate/Campaign/{campaignId}`

**Propósito:** Obtener toda la información de una campaña específica para la pantalla de edición, detalle o revisión previa al envío.

**Parámetros de ruta:**

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `campaignId` | int | ID de la campaña |

**Cuándo usarlo:**

- Al abrir una campaña desde el listado.
- Para cargar el formulario de edición (solo si `status === "Pending"`).
- Para mostrar vista de solo lectura de campañas ya enviadas.

**Respuesta exitosa — `data`:** Un objeto campaña.

**Respuesta 404:** La campaña no existe.

---

### 7.3 POST — Crear campaña

**URL:** `POST .../api/corporate/Campaign`

**Propósito:** Crear una nueva campaña en estado **`Pending`**. Este es siempre el **primer paso** del flujo. Devuelve el `campaignId` que se usará en todas las operaciones siguientes (adjuntos, prospectos, envío).

**Content-Type:** `application/json`

**Cuerpo de la petición:**

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|-------------|-------------|
| `name` | string | Sí | Nombre interno de la campaña. Máximo 200 caracteres. Aparece en la plantilla de correo como identificador de campaña. |
| `subject` | string | Sí | Asunto del email. Máximo 500 caracteres. Es lo que verá el destinatario en su bandeja. |
| `bodyContent` | string | Sí | Contenido principal del mensaje. Se recomienda HTML (párrafos, listas, negritas). Se inserta dentro de la plantilla SendGrid. |
| `scheduledAt` | datetime | No | Fecha/hora de referencia (UTC). Útil para mostrar "programada para el 15 de junio" en UI. **No dispara envío automático.** |
| `createdByUserId` | int | No | ID del usuario corporate que crea la campaña. Útil para auditoría. |

**Ejemplo de cuerpo:**

```json
{
  "name": "Executive Brief · Q2 2026",
  "subject": "Una nota personal sobre el próximo capítulo de su marca",
  "bodyContent": "<p>Nos complace compartir una actualización preparada exclusivamente para usted.</p><h2>Próximos pasos</h2><p>Nuestro equipo identificó palancas de alto impacto en marketplace y publicidad.</p>",
  "scheduledAt": "2026-06-15T10:00:00Z",
  "createdByUserId": 123
}
```

**Respuesta exitosa — `data`:**

```json
{
  "campaignId": 5
}
```

**Flujo recomendado en UI:**

1. Usuario llena formulario básico (nombre, asunto, editor de texto enriquecido para el cuerpo).
2. Al guardar → POST crear.
3. Redirigir a pantalla de detalle/edición con el `campaignId` recién obtenido.
4. Desde ahí, permitir subir adjuntos y agregar prospectos.

**Errores 400:** Campos vacíos, longitudes excedidas.

---

### 7.4 PUT — Actualizar campaña

**URL:** `PUT .../api/corporate/Campaign/{campaignId}`

**Propósito:** Modificar nombre, asunto, cuerpo o fecha programada de una campaña que **aún está en `Pending`**.

**Parámetros de ruta:** `campaignId`

**Cuerpo:** Idéntico al POST de creación (mismos campos). El `campaignId` va en la URL, no en el body.

**Cuándo usarlo:**

- El usuario edita el texto de la campaña antes de enviar.
- Corrige el asunto o el nombre interno.
- Actualiza la fecha de referencia.

**Restricción:** Si la campaña ya está en `Sent`, responde **400** con mensaje indicando que no puede modificarse.

**Respuesta exitosa:** `data` es `null`, mensaje de confirmación.

---

### 7.5 GET — Listar adjuntos

**URL:** `GET .../api/corporate/Campaign/{campaignId}/attachments`

**Propósito:** Obtener todos los documentos asociados a una campaña.

**Cuándo usarlo:**

- Mostrar la lista de archivos adjuntos en la pantalla de detalle.
- Antes del envío, confirmar qué documentos irán incluidos en el correo.

**Respuesta exitosa — `data`:** Array de objetos adjunto (ver [Modelo CampaignAttachment](#campaignattachment)).

Cada adjunto incluye `fileName`, `fileUrl`, `contentType` y `uploadedAt`. La URL apunta a Azure Blob y puede usarse para descarga o preview en UI si la política de acceso lo permite.

---

### 7.6 POST — Subir adjuntos

**URL:** `POST .../api/corporate/Campaign/{campaignId}/attachments`

**Propósito:** Adjuntar uno o **varios** documentos a la campaña. Los archivos se almacenan en Azure Blob Storage bajo una carpeta por campaña.

**Content-Type:** `multipart/form-data` (no JSON)

**Campos del formulario:**

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|-------------|-------------|
| `files` | file(s) | Sí | Uno o más archivos. En HTML usar `<input type="file" multiple name="files">`. |
| `uploadedByUserId` | int | No | ID del usuario que sube el archivo. |

**Cuándo usarlo:**

- Después de crear la campaña, en la sección "Documentos adjuntos".
- Cada vez que el usuario agrega más archivos (se pueden hacer múltiples POST).

**Restricción:** Solo campañas `Pending`. Archivos vacíos, extensiones no permitidas o tamaño excedido → **400**.

**Respuesta exitosa — `data`:**

```json
{
  "attachmentCount": 2
}
```

Indica cuántos archivos se subieron en esa petición específica.

**Nota para UI:** Mostrar progreso de carga y refrescar la lista con GET attachments después de cada upload exitoso.

---

### 7.7 GET — Listar prospectos

**URL:** `GET .../api/corporate/Campaign/{campaignId}/prospects`

**Propósito:** Obtener la lista completa de destinatarios configurados para esa campaña.

**Cuándo usarlo:**

- Pantalla "Destinatarios" de la campaña.
- Resumen antes del envío ("Enviar a 47 contactos").
- Verificar nombres y correos antes de confirmar.

**Respuesta exitosa — `data`:** Array de prospectos ordenados por fecha de creación ascendente.

Cada prospecto muestra:

- Email (clave para el envío).
- Nombre y apellido (para personalización).
- `leadId` si provino de un lead (permite mostrar badge "Desde lead").
- `sentAt` — null si aún no se envió; con fecha si la campaña ya se envió a esa persona.

---

### 7.8 POST — Agregar prospectos manualmente

**URL:** `POST .../api/corporate/Campaign/{campaignId}/prospects`

**Propósito:** Agregar destinatarios **escribiendo sus datos directamente**, sin pasar por el listado de leads. Ideal para contactos puntuales, partners, o correos que no están registrados como lead.

**Content-Type:** `application/json`

**Cuerpo:**

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|-------------|-------------|
| `prospects` | array | Sí | Lista de prospectos a agregar. Mínimo uno. |

Cada elemento del array:

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|-------------|-------------|
| `email` | string | Sí | Correo del destinatario. Debe ser formato email válido. |
| `firstName` | string | No | Nombre. **Muy recomendado** para saludo personalizado. |
| `lastName` | string | No | Apellido. |
| `leadId` | int | No | Opcional. Solo si se quiere vincular manualmente a un lead existente. |

**Ejemplo — un contacto individual:**

```json
{
  "prospects": [
    {
      "email": "maria.garcia@partner.com",
      "firstName": "María",
      "lastName": "García"
    }
  ]
}
```

**Ejemplo — varios contactos a la vez:**

```json
{
  "prospects": [
    {
      "email": "contacto1@empresa.com",
      "firstName": "Ana",
      "lastName": "López"
    },
    {
      "email": "contacto2@empresa.com",
      "firstName": "Carlos",
      "lastName": "Ruiz"
    }
  ]
}
```

**Respuesta exitosa — `data`:**

```json
{
  "addedCount": 2,
  "skippedCount": 0
}
```

| Campo | Significado |
|-------|-------------|
| `addedCount` | Prospectos nuevos agregados correctamente. |
| `skippedCount` | Omitidos por email duplicado en esta campaña, email vacío o inválido. |

**Cuándo usarlo en UI:**

- Botón "Agregar correo manual".
- Modal con campos: email, nombre, apellido.
- También útil para pegar una lista pequeña de contactos.

**Importante:** Si el usuario intenta agregar un correo que ya existe en esta campaña, no falla la petición entera — simplemente incrementa `skippedCount`. La UI debe informar cuántos se agregaron y cuántos se omitieron.

---

### 7.9 POST — Agregar prospectos desde leads

**URL:** `POST .../api/corporate/Campaign/{campaignId}/prospects/from-leads`

**Propósito:** Importar destinatarios desde el **módulo de Leads** corporativo. Es el flujo principal cuando el usuario selecciona múltiples leads del listado.

**Content-Type:** `application/json`

**Cuerpo:**

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|-------------|-------------|
| `leadIds` | array de int | Sí | IDs de los leads seleccionados (`SubmissionID` del lead). Mínimo uno. |

**Ejemplo:**

```json
{
  "leadIds": [42, 87, 103, 215]
}
```

**Qué hace el backend internamente:**

1. Busca cada lead por su ID.
2. De cada lead extrae: **email**, **firstName**, **lastName**.
3. Guarda el `leadId` en el prospecto para trazabilidad.
4. Omite leads que no tienen email.
5. Omite correos que ya existen en esta campaña.

**Respuesta:** Misma estructura que agregar manual — `addedCount` y `skippedCount`.

**Flujo recomendado en UI:**

1. Pantalla con listado de leads (API de leads existente).
2. Selección múltiple con checkboxes.
3. Botón "Agregar a campaña" → envía los IDs seleccionados.
4. Mostrar toast: "Se agregaron X contactos. Y omitidos (sin email o duplicados)."
5. Refrescar GET prospects.

**Por qué nombre y correo son lo importante:**

El correo es el canal de entrega. El nombre permite que la plantilla SendGrid muestre un saludo elegante ("Dear John,"). Sin nombre, la plantilla usa un saludo genérico ("Dear Partner,"). Por eso la UI debe mostrar claramente nombre + email en la tabla de prospectos.

---

### 7.10 DELETE — Eliminar prospecto

**URL:** `DELETE .../api/corporate/Campaign/{campaignId}/prospects/{prospectId}`

**Propósito:** Quitar un destinatario de la lista antes de enviar la campaña.

**Parámetros de ruta:**

| Parámetro | Descripción |
|-----------|-------------|
| `campaignId` | ID de la campaña |
| `prospectId` | ID del prospecto (`campaignProspectId`), no el leadId |

**Cuándo usarlo:**

- El usuario se equivocó al agregar un contacto.
- Quiere limpiar la lista antes del envío.

**Restricción:** Solo campañas `Pending`.

**Respuesta exitosa:** Confirmación sin data.

**Nota UI:** El `prospectId` se obtiene del GET de prospectos, no del `leadId`.

---

### 7.11 POST — Enviar campaña

**URL:** `POST .../api/corporate/Campaign/{campaignId}/send`

**Propósito:** **Ejecutar el envío** de la campaña. Es la acción final e irreversible. Dispara el envío de correos individuales vía SendGrid con plantilla corporativa, contenido de la campaña y adjuntos.

**Content-Type:** `application/json`

**Cuerpo:**

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|-------------|-------------|
| `additionalBccEmails` | array de string | No | Correos extra que recibirán la campaña **solo en este envío**. No se guardan en la base de datos. |

**Ejemplo — envío estándar:**

```json
{}
```

**Ejemplo — envío con copia al usuario que envía:**

```json
{
  "additionalBccEmails": [
    "usuario.corporativo@vhconsultor.com"
  ]
}
```

**Ejemplo — varios correos extra:**

```json
{
  "additionalBccEmails": [
    "director@vhconsultor.com",
    "marketing@vhconsultor.com"
  ]
}
```

**Qué ocurre paso a paso:**

1. Valida que la campaña exista y esté en `Pending`.
2. Valida que el template SendGrid esté configurado en el servidor.
3. Construye la lista final de destinatarios: todos los prospectos + `additionalBccEmails`.
4. Elimina duplicados por email (sin distinguir mayúsculas).
5. Si no hay ningún destinatario → error 400.
6. Descarga los adjuntos de Azure.
7. Por **cada destinatario**, envía un correo individual en BCC con:
   - Plantilla visual SendGrid.
   - Asunto de la campaña.
   - Cuerpo HTML de la campaña.
   - Nombre/apellido del prospecto (vacío si es correo extra manual).
   - Todos los adjuntos.
8. Marca la campaña como `Sent` con `sentAt`.
9. Marca `sentAt` en cada prospecto que recibió el correo exitosamente.

**Respuesta exitosa — `data`:**

```json
{
  "campaignId": 5,
  "sentCount": 48,
  "failedCount": 2,
  "totalRecipients": 50,
  "failures": [
    "correo-invalido@dominio.com: Failed to send email. Status: 400"
  ],
  "message": "Campaign sent to 48 recipient(s). 2 failed."
}
```

| Campo | Significado |
|-------|-------------|
| `sentCount` | Correos enviados correctamente. |
| `failedCount` | Correos que fallaron. |
| `totalRecipients` | Total de destinatarios únicos procesados. |
| `failures` | Lista de errores por email (útil para mostrar al usuario). |
| `message` | Resumen legible. |

**Comportamiento ante fallos parciales:**

- Si **al menos uno** se envió con éxito → HTTP 200, campaña pasa a `Sent`.
- Si **todos** fallaron → HTTP 400, campaña permanece `Pending`.

**Pantalla de confirmación sugerida en UI:**

Antes de llamar a este endpoint, mostrar:

- Nombre y asunto de la campaña.
- Cantidad de prospectos.
- Lista de adjuntos.
- Campo opcional: "Enviarme copia a:" (uno o varios emails → `additionalBccEmails`).
- Advertencia: "Esta acción no se puede deshacer."
- Checkbox de confirmación.
- Mensaje de privacidad: "Cada destinatario recibirá un correo individual. Nadie verá los correos de los demás."

---

## 8. Modelos de datos

### Campaign

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `campaignId` | int | Identificador único |
| `name` | string | Nombre interno |
| `subject` | string | Asunto del email |
| `bodyContent` | string | Cuerpo HTML del mensaje |
| `status` | string | `Pending` o `Sent` |
| `scheduledAt` | datetime \| null | Fecha de referencia |
| `sentAt` | datetime \| null | Fecha real de envío |
| `createdByUserId` | int \| null | Usuario creador |
| `createdAt` | datetime | Fecha de creación |
| `updatedAt` | datetime \| null | Última modificación |

### CampaignAttachment

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `campaignAttachmentId` | int | ID del adjunto |
| `campaignId` | int | Campaña asociada |
| `fileUrl` | string | URL en Azure Blob |
| `fileName` | string | Nombre original del archivo |
| `contentType` | string | MIME type |
| `uploadedByUserId` | int \| null | Quién lo subió |
| `uploadedAt` | datetime | Fecha de subida |

### CampaignProspect

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `campaignProspectId` | int | ID del prospecto (usar en DELETE) |
| `campaignId` | int | Campaña asociada |
| `email` | string | Correo del destinatario |
| `leadId` | int \| null | Lead origen, si aplica |
| `firstName` | string \| null | Nombre |
| `lastName` | string \| null | Apellido |
| `sentAt` | datetime \| null | Cuándo se le envió (post-envío) |
| `createdAt` | datetime | Cuándo se agregó a la campaña |

---

## 9. Flujo sugerido para la interfaz de usuario

### Pantalla 1 — Listado de campañas

- Tabs o filtros: **Pendientes** | **Enviadas**.
- Botón "Nueva campaña".
- Columnas sugeridas: nombre, asunto, estado, fecha creación, fecha envío, acciones.
- Acción "Editar" solo en `Pending`. Acción "Ver" en `Sent`.

### Pantalla 2 — Crear / editar campaña

Secciones en tabs o steps:

**A) Información general**
- Nombre, asunto, editor rich text para cuerpo.
- Fecha programada (opcional, date picker).
- Guardar → POST o PUT.

**B) Documentos**
- Zona drag & drop para archivos.
- Lista de adjuntos con nombre y fecha.
- POST attachments al subir; GET attachments para listar.

**C) Destinatarios**
- Sub-sección "Desde leads": abre selector del listado de leads con checkboxes → POST from-leads.
- Sub-sección "Agregar manual": modal email + nombre + apellido → POST prospects.
- Tabla de prospectos actuales con botón eliminar → DELETE.
- Contador visible: "47 destinatarios".

**D) Enviar** (solo `Pending`)
- Resumen de todo lo anterior.
- Campo emails adicionales (tags input).
- Botón "Enviar campaña" con confirmación → POST send.
- Resultado: mostrar sentCount, failedCount y failures si hay.

### Pantalla 3 — Detalle enviada (solo lectura)

- Todo el contenido en modo lectura.
- Fecha de envío.
- Lista de prospectos con `sentAt`.
- Sin botones de edición.

---

## 10. Errores frecuentes y cómo manejarlos

| Situación | HTTP | Qué mostrar al usuario |
|-----------|------|------------------------|
| Campos obligatorios vacíos | 400 | Mensaje de validación del API |
| Editar campaña ya enviada | 400 | "Esta campaña ya fue enviada y no puede modificarse" |
| Subir adjuntos a campaña enviada | 400 | Igual que arriba |
| Enviar sin destinatarios | 400 | "Agregue al menos un destinatario o un correo adicional" |
| Template SendGrid no configurado | 400 | "El servicio de correo no está configurado. Contacte soporte." |
| Campaña no encontrada | 404 | "La campaña no existe" |
| Todos los envíos fallaron | 400 | Mostrar lista `failures` |
| Algunos envíos fallaron | 200 | "Enviado a X de Y. Revisar errores." + lista failures |
| Error interno | 500 | "Error inesperado. ID: {errorNumber}" |

---

## Resumen del orden de llamadas para una campaña nueva

```
POST   /Campaign                              → obtener campaignId
PUT    /Campaign/{id}                         → (opcional, si edita después)
POST   /Campaign/{id}/attachments             → subir documentos (repetible)
POST   /Campaign/{id}/prospects/from-leads    → agregar leads seleccionados
POST   /Campaign/{id}/prospects               → agregar correos manuales
GET    /Campaign/{id}/prospects               → revisar lista final
GET    /Campaign/{id}/attachments             → revisar adjuntos
POST   /Campaign/{id}/send                    → enviar con optional additionalBccEmails
GET    /Campaign/{id}                         → confirmar status Sent
```

---

## Plantilla SendGrid (referencia)

El diseño visual del correo vive en SendGrid. El backend inyecta estas variables en cada envío:

| Variable | Origen |
|----------|--------|
| `subject` | Asunto de la campaña |
| `campaignBody` | Cuerpo HTML de la campaña |
| `campaignName` | Nombre interno de la campaña |
| `recipientFirstName` | Nombre del prospecto |
| `recipientLastName` | Apellido del prospecto |

Variable de entorno en Azure: `SendGrid__CampaignTemplateId`
