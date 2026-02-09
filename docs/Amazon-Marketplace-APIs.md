# Amazon Marketplace APIs – Documentación detallada

Documentación de los tres endpoints de mantenimiento de Amazon Marketplaces (GET, POST, PUT). Incluye URLs completas, parámetros, cuerpos de solicitud, todas las respuestas JSON posibles y condiciones de uso.

**Base URL:** `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonMarketplace`  
**Autenticación:** Requerida (token Bearer o la que use la API). Todos los endpoints están protegidos con `[Authorize]`.

**Estructura estándar de respuesta:** Todas las respuestas devuelven un objeto JSON con la forma típica del backend:

- `status` (boolean): `true` si la operación fue exitosa, `false` si hubo error.
- `statusCode` (number): Código HTTP o código interno (ej. 200, 404, 422, 500).
- `data` (objeto o array o null): Datos de la respuesta; en errores suele ser `null`.
- `message` (string): Mensaje descriptivo o mensaje de error.
- `errorNumber` (string o null): Opcional; en muchos casos `null`.
- `timestamp` (string): Fecha/hora de la respuesta (zona Costa Rica), en formato ISO.

---

## 1. GET – Listar / buscar marketplaces

### URL

```
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonMarketplace
```

### Descripción

Devuelve una lista de Amazon Marketplaces. Todos los parámetros son **opcionales**. Si no se envía ninguno, se devuelven **todos** los marketplaces. Si se envían varios parámetros, los filtros se combinan con **AND**: solo aparecen registros que cumplan **todos** los criterios.

Los parámetros de tipo texto (`amazonMarketplaceCode`, `countryCode`, `countryName`, `amazonRegion`, `currencyCode`) se tratan como **búsqueda parcial**: el valor se recorta (trim) y en base de datos se usa un `LIKE` con el patrón `%valor%` (el texto puede aparecer en cualquier parte del campo). La comparación no distingue mayúsculas/minúsculas según la configuración de la base de datos.

El parámetro `id` es **exacto**: solo se devuelve el marketplace con ese `AmazonMarketplaceId`. Si se envía `id` y no existe ningún registro con ese ID, el API responde con **404 Not Found**.

El orden de los resultados es siempre **por nombre de país** (`CountryName`) ascendente.

### Parámetros de consulta (query)

| Parámetro | Tipo | Obligatorio | Descripción |
|-----------|------|-------------|-------------|
| `id` | integer | No | Si se envía, devuelve solo el marketplace con `AmazonMarketplaceId` igual a este valor. Comparación exacta. |
| `amazonMarketplaceCode` | string | No | Código del marketplace. Búsqueda parcial (LIKE). Si está vacío o solo espacios, no se aplica. |
| `countryCode` | string | No | Código de país (ej. US, MX). Búsqueda parcial (LIKE). |
| `countryName` | string | No | Nombre del país. Búsqueda parcial (LIKE). |
| `amazonRegion` | string | No | Región Amazon (ej. NA, EU, FE). Búsqueda parcial (LIKE). |
| `currencyCode` | string | No | Código de moneda (ej. USD, EUR). Búsqueda parcial (LIKE). |
| `isActive` | boolean | No | Si se envía `true` o `false`, filtra por ese estado. Si no se envía, se devuelven activos e inactivos. |

**Comportamiento de los filtros tipo LIKE:**

- El valor se recorta (espacios al inicio y al final se eliminan).
- Si después del recorte el valor no está vacío, en SQL se aplica `Campo LIKE '%valor%'`.
- Ejemplo: `countryName=United` puede coincidir con "United States", "United Kingdom", etc.

### Ejemplos de URL

- Todos los marketplaces:  
  `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonMarketplace`

- Solo el marketplace con ID 5:  
  `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonMarketplace?id=5`

- Marketplaces activos:  
  `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonMarketplace?isActive=true`

- Código de país que contenga "US":  
  `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonMarketplace?countryCode=US`

- Nombre de país que contenga "Mexico":  
  `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonMarketplace?countryName=Mexico`

- Región NA y activos:  
  `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonMarketplace?amazonRegion=NA&isActive=true`

### Respuesta exitosa (HTTP 200 OK)

**Cuando hay resultados (uno o más):**

El cuerpo es un objeto con la estructura estándar; `data` es un **array** de objetos, cada uno con un marketplace.

Campos de cada elemento de `data`:

| Campo | Tipo | Descripción |
|--------|------|-------------|
| `amazonMarketplaceId` | number | ID del marketplace (clave primaria). |
| `amazonMarketplaceCode` | string | Código del marketplace (ej. ATVPDKIKXODER). |
| `countryCode` | string | Código de país (ej. US, MX). |
| `countryName` | string | Nombre del país. |
| `amazonRegion` | string | Región (ej. NA, EU, FE). |
| `currencyCode` | string | Código de moneda (ej. USD, MXN). |
| `isActive` | boolean | Si el marketplace está activo. |

**Ejemplo de respuesta (varios resultados):**

```json
{
  "status": true,
  "statusCode": 200,
  "data": [
    {
      "amazonMarketplaceId": 1,
      "amazonMarketplaceCode": "ATVPDKIKXODER",
      "countryCode": "US",
      "countryName": "United States",
      "amazonRegion": "NA",
      "currencyCode": "USD",
      "isActive": true
    },
    {
      "amazonMarketplaceId": 2,
      "amazonMarketplaceCode": "A2EUQ1WTGCTBG2",
      "countryCode": "CA",
      "countryName": "Canada",
      "amazonRegion": "NA",
      "currencyCode": "CAD",
      "isActive": true
    }
  ],
  "message": "Found 2 Amazon marketplace(s).",
  "errorNumber": null,
  "timestamp": "2025-02-07T16:00:00"
}
```

**Cuando se pidió por `id` y se encontró un solo registro:**  
`data` es un array con un solo elemento y el mensaje es: `"Amazon marketplace retrieved successfully."`

**Cuando no hay resultados pero no se envió `id`:**  
Se devuelve igualmente **HTTP 200**, con `data` como array vacío `[]` y mensaje por ejemplo: `"Found 0 Amazon marketplace(s)."`

### Respuesta cuando no se encuentra el ID (HTTP 404 Not Found)

Se devuelve **solo** cuando se envió el parámetro `id` y no existe ningún marketplace con ese `AmazonMarketplaceId`.

**Ejemplo:**

```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Amazon marketplace with ID 999 was not found.",
  "errorNumber": null,
  "timestamp": "2025-02-07T16:00:00"
}
```

### Respuesta de error interno (HTTP 500 Internal Server Error)

Ante una excepción no controlada (por ejemplo fallo de base de datos):

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Error retrieving Amazon marketplaces: <detalle del error>",
  "errorNumber": null,
  "timestamp": "2025-02-07T16:00:00"
}
```

---

## 2. POST – Crear un marketplace

### URL

```
POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonMarketplace
```

### Descripción

Crea un nuevo registro en `Corporate.AmazonMarketplaces`. El cuerpo de la petición debe ser un JSON con todos los campos requeridos. Antes de guardar se ejecutan las reglas de validación; si alguna falla, se responde con **400 Bad Request** y un mensaje que agrupa los errores. Si la creación en base de datos falla, se devuelve **500**.

En el servidor, los valores de tipo string se guardan **recortados** (trim): espacios al inicio y al final se eliminan.

### Cuerpo de la solicitud (Body)

Content-Type: `application/json`.

| Campo | Tipo | Obligatorio | Descripción |
|--------|------|-------------|-------------|
| `amazonMarketplaceCode` | string | Sí | Código del marketplace. No puede estar vacío. Máximo 20 caracteres. |
| `countryCode` | string | Sí | Código de país. No puede estar vacío. Máximo 10 caracteres. |
| `countryName` | string | Sí | Nombre del país. No puede estar vacío. Máximo 100 caracteres. |
| `amazonRegion` | string | Sí | Región Amazon. No puede estar vacío. Máximo 20 caracteres. |
| `currencyCode` | string | Sí | Código de moneda. No puede estar vacío. Máximo 10 caracteres. |
| `isActive` | boolean | No | Si no se envía, se asume `true`. Indica si el marketplace está activo. |

**Ejemplo de body correcto:**

```json
{
  "amazonMarketplaceCode": "A2EUQ1WTGCTBG2",
  "countryCode": "CA",
  "countryName": "Canada",
  "amazonRegion": "NA",
  "currencyCode": "CAD",
  "isActive": true
}
```

**Ejemplo mínimo (isActive por defecto true):**

```json
{
  "amazonMarketplaceCode": "A1AM78C64UM0Y8",
  "countryCode": "MX",
  "countryName": "Mexico",
  "amazonRegion": "NA",
  "currencyCode": "MXN"
}
```

### Validaciones (reglas del backend)

Si no se cumplen, la respuesta es **400 Bad Request** y en `message` viene el texto de los errores concatenados (en inglés):

- **Amazon marketplace code:** obligatorio; máximo 20 caracteres. Mensajes: "Amazon marketplace code is required." / "Amazon marketplace code cannot exceed 20 characters."
- **Country code:** obligatorio; máximo 10 caracteres. "Country code is required." / "Country code cannot exceed 10 characters."
- **Country name:** obligatorio; máximo 100 caracteres. "Country name is required." / "Country name cannot exceed 100 characters."
- **Amazon region:** obligatorio; máximo 20 caracteres. "Amazon region is required." / "Amazon region cannot exceed 20 characters."
- **Currency code:** obligatorio; máximo 10 caracteres. "Currency code is required." / "Currency code cannot exceed 10 characters."

Si hay varios errores, el `message` puede contener varios textos separados por coma (ej. "Amazon marketplace code is required., Country code is required.").

### Respuesta exitosa (HTTP 200 OK)

El `data` es el **ID numérico** del marketplace creado (`AmazonMarketplaceId`).

**Ejemplo:**

```json
{
  "status": true,
  "statusCode": 200,
  "data": 16,
  "message": "Amazon marketplace created successfully.",
  "errorNumber": null,
  "timestamp": "2025-02-07T16:00:00"
}
```

### Respuesta de validación (HTTP 400 Bad Request)

Cuando falla la validación del body, el JSON tiene `status: false`, `statusCode: 422` en el cuerpo (la petición HTTP es 400), y `message` con los errores.

**Ejemplo (campo vacío y longitud):**

```json
{
  "status": false,
  "statusCode": 422,
  "data": null,
  "message": "Amazon marketplace code is required., Country name cannot exceed 100 characters.",
  "errorNumber": null,
  "timestamp": "2025-02-07T16:00:00"
}
```

### Respuesta de error de base de datos (HTTP 500 Internal Server Error)

Por ejemplo violación de clave única o otro error al guardar:

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Error creating Amazon marketplace: <detalle del error o excepción interna>",
  "errorNumber": null,
  "timestamp": "2025-02-07T16:00:00"
}
```

---

## 3. PUT – Actualizar un marketplace

### URL

```
PUT https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonMarketplace/{amazonMarketplaceId}
```

`{amazonMarketplaceId}` es el **ID** del marketplace a actualizar (entero positivo). Va en la ruta, no en el body.

**Ejemplo:**  
`PUT https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonMarketplace/5`

### Descripción

Actualiza un registro existente identificado por `amazonMarketplaceId`. El cuerpo debe contener todos los campos editables; las mismas reglas de validación que en POST se aplican aquí. Si el ID no existe, el API responde con **404 Not Found**. Si la validación falla, **400 Bad Request**. Los valores string se guardan recortados (trim).

### Parámetro de ruta

| Parámetro | Tipo | Obligatorio | Descripción |
|-----------|------|-------------|-------------|
| `amazonMarketplaceId` | integer | Sí | ID del marketplace a actualizar. Debe existir en la base de datos. |

### Cuerpo de la solicitud (Body)

Content-Type: `application/json`. Misma forma que en POST, pero **isActive es obligatorio** en PUT (no hay valor por defecto en el DTO de actualización).

| Campo | Tipo | Obligatorio | Descripción |
|--------|------|-------------|-------------|
| `amazonMarketplaceCode` | string | Sí | Código del marketplace. No vacío. Máximo 20 caracteres. |
| `countryCode` | string | Sí | Código de país. No vacío. Máximo 10 caracteres. |
| `countryName` | string | Sí | Nombre del país. No vacío. Máximo 100 caracteres. |
| `amazonRegion` | string | Sí | Región Amazon. No vacío. Máximo 20 caracteres. |
| `currencyCode` | string | Sí | Código de moneda. No vacío. Máximo 10 caracteres. |
| `isActive` | boolean | Sí | Estado activo/inactivo del marketplace. |

**Ejemplo de body correcto:**

```json
{
  "amazonMarketplaceCode": "A2EUQ1WTGCTBG2",
  "countryCode": "CA",
  "countryName": "Canada",
  "amazonRegion": "NA",
  "currencyCode": "CAD",
  "isActive": false
}
```

### Validaciones

Son las mismas que en POST (campos requeridos y longitudes máximas). Los mensajes de error están en inglés y se devuelven en `message` cuando la respuesta es **400 Bad Request** (cuerpo con `statusCode: 422`).

### Respuesta exitosa (HTTP 200 OK)

No se devuelve el objeto actualizado; solo un mensaje de éxito. El `data` en la respuesta es `null`.

**Ejemplo:**

```json
{
  "status": true,
  "statusCode": 200,
  "data": null,
  "message": "Amazon marketplace updated successfully.",
  "errorNumber": null,
  "timestamp": "2025-02-07T16:00:00"
}
```

### Respuesta cuando no existe el ID (HTTP 404 Not Found)

Cuando `amazonMarketplaceId` no existe en la base de datos:

**Ejemplo:**

```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Amazon marketplace with ID 999 was not found.",
  "errorNumber": null,
  "timestamp": "2025-02-07T16:00:00"
}
```

### Respuesta de validación (HTTP 400 Bad Request)

Mismo formato que en POST: `status: false`, `statusCode: 422` en el cuerpo, `message` con los errores de validación concatenados.

### Respuesta de error de servidor (HTTP 500 Internal Server Error)

Por error de base de datos u otra excepción:

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Error updating Amazon marketplace: <detalle del error>",
  "errorNumber": null,
  "timestamp": "2025-02-07T16:00:00"
}
```

---

## Resumen de condiciones y códigos HTTP

| Endpoint | Condición | HTTP | Contenido de `data` | Notas |
|----------|-----------|------|---------------------|--------|
| GET | Sin parámetros | 200 | Array con todos los marketplaces | Orden por CountryName. |
| GET | Con parámetros (y hay resultados) | 200 | Array con los que cumplen los filtros | Filtros AND; strings con LIKE. |
| GET | Con `id` y existe | 200 | Array con un elemento | Mensaje: "Amazon marketplace retrieved successfully." |
| GET | Con `id` y no existe | 404 | null | Mensaje indica el ID no encontrado. |
| GET | Filtros sin resultados (sin `id`) | 200 | [] | Mensaje: "Found 0 Amazon marketplace(s)." |
| GET | Error interno | 500 | null | Mensaje con detalle del error. |
| POST | Body válido | 200 | ID (number) del creado | Mensaje: "Amazon marketplace created successfully." |
| POST | Body inválido | 400 | null | Cuerpo con statusCode 422 y message con errores. |
| POST | Error al guardar | 500 | null | Mensaje con detalle. |
| PUT | ID existe y body válido | 200 | null | Mensaje: "Amazon marketplace updated successfully." |
| PUT | ID no existe | 404 | null | Mensaje indica el ID no encontrado. |
| PUT | Body inválido | 400 | null | Cuerpo con statusCode 422 y message con errores. |
| PUT | Error al guardar | 500 | null | Mensaje con detalle. |

---

## Orden de resultados (GET)

En todas las respuestas GET que devuelven lista, el orden es **siempre** por `CountryName` ascendente (A–Z).
