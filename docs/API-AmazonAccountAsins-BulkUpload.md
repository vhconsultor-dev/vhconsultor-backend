# API: Carga masiva de ASINs (Amazon Account Asins)

Documentación del endpoint de carga masiva de ASINs desde archivo Excel, para consumo desde el frontend. Incluye la estructura JSON de todas las respuestas y todos los escenarios posibles (éxito y error).

---

## Estructura JSON común a todas las respuestas

Todas las respuestas del endpoint comparten el mismo envelope. Los nombres de propiedades se serializan en **camelCase** (p. ej. `statusCode`, `errorNumber`).

| Propiedad   | Tipo             | Siempre presente | Descripción |
|------------|------------------|------------------|-------------|
| `status`   | boolean          | Sí               | `true` si la operación fue exitosa (HTTP 200); `false` en cualquier error. |
| `statusCode` | number (entero) | Sí               | Código HTTP de la respuesta. En respuestas de validación (HTTP 400) el backend puede devolver aquí 500; el valor de referencia es el **código HTTP** de la petición. |
| `data`     | object \| null   | Sí               | En 200: objeto con el resultado de la carga. En 4xx/5xx: `null`. |
| `message`  | string           | Sí               | Mensaje descriptivo. En éxito incluye cantidades; en error, el motivo. |
| `errorNumber` | string \| null | Sí             | En este endpoint suele ser `null`. Si el API lo rellena en otros casos, es un código de error interno. |
| `timestamp`| string (ISO 8601)| Sí              | Fecha/hora del servidor (zona Costa Rica). Ejemplo: `"2025-02-28T18:30:00.000"`. |

Ejemplo mínimo de envelope (error):

```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Excel file is required",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

---

## Base URL y recurso

- **Base:** la misma que el resto del API (ej. `https://tu-dominio.com` o `https://localhost:7xxx`).
- **Recurso:** `POST /api/corporate/amazon-account-asins/bulk-upload`

**URL completa de ejemplo:**  
`POST https://tu-dominio.com/api/corporate/amazon-account-asins/bulk-upload`

---

## Autenticación

- Si el API está configurado con `[Authorize]` en controladores Corporate, este endpoint puede requerir autenticación.
- En ese caso enviar el header:
  - **Header:** `Authorization`
  - **Valor:** `Bearer {token_jwt}`

Si el endpoint no está protegido, la petición puede ir sin `Authorization`.

---

## Content-Type

- **Obligatorio:** `multipart/form-data`
- No usar `application/json` para este endpoint.

---

## Invocación

### Método y tipo de contenido

| Método | Ruta | Content-Type |
|--------|------|--------------|
| `POST` | `/api/corporate/amazon-account-asins/bulk-upload` | `multipart/form-data` |

### Parámetros del request (form-data)

Todos los parámetros se envían como partes del `multipart/form-data`:

| Parámetro | Tipo | Obligatorio | Descripción |
|-----------|------|-------------|-------------|
| `amazonAccountId` | número (entero) | Sí | ID de la cuenta Amazon a la que se asociarán los ASINs. Debe ser > 0 y debe existir en el sistema. |
| `excelFile` | archivo | Sí | Archivo Excel (.xlsx o .xls). No puede estar vacío (tamaño > 0). |

- **Nombres exactos:** el backend espera `amazonAccountId` y `excelFile` (sensibles a mayúsculas/minúsculas en algunos clientes; usar exactamente así).
- El archivo debe tener extensión `.xlsx` o `.xls` y contenido válido (no solo el nombre).

---

## Validaciones del request (antes de procesar el Excel)

Si alguna falla, el API responde con **400 Bad Request** y no se procesa el archivo:

| Regla | Mensaje cuando falla |
|-------|----------------------|
| `amazonAccountId` debe ser mayor que 0 | `"AmazonAccountId must be greater than 0"` |
| Debe enviarse un archivo | `"Excel file is required"` |
| Extensión del archivo | `"File must be an Excel file (.xlsx or .xls)"` |
| El archivo no puede estar vacío | `"Excel file cannot be empty"` |

Los mensajes pueden llegar concatenados en un solo string si hay varias validaciones fallidas (ej.: `"Excel file is required, File must be an Excel file (.xlsx or .xls)"`).

---

## Validaciones sobre el Excel (durante el procesamiento)

### 1. Columnas obligatorias (primera fila = encabezados)

La **primera fila** del Excel se interpreta como encabezados. Debe contener al menos estas columnas (comparación sin distinguir mayúsculas/minúsculas):

| Columna obligatoria | Descripción |
|--------------------|-------------|
| `ASIN` | Identificador del producto. Obligatorio por fila. |
| `Product Title` | Título del producto. |

Si falta alguna de estas columnas, el API lanza error y responde **400 Bad Request** con un mensaje del tipo:  
`"Missing required columns: ASIN"` o `"Missing required columns: ASIN, Product Title"` (según lo que falte).

### 2. Columnas opcionales (mapeo)

Si existen estas columnas en el Excel, se usan; si no, el campo queda vacío o NULL en base de datos:

| Columna en Excel | Uso en BD |
|------------------|-----------|
| `Manufacturer Code` | ManufacturerCode |
| `Parent ASIN` | ParentAsin |
| `UPC` | Upc |
| `EAN` | Ean |
| `ISBN` | Isbn |
| `Model Number` | ModelNumber |
| `Category` | Catálogo: código + espacio + descripción (ej. `2000 Hardware`). Se crea o reutiliza categoría. |
| `SubCategory` | Catálogo (mismo formato). Depende de Category. |
| `ProductGroup` | Catálogo (mismo formato). |
| `Replenishment Category` | Catálogo (mismo formato). |
| `Release Date` | ReleaseDate (fecha). |
| `Prep Instructions Required` | PrepInstructionsRequired |
| `Prep Instructions Vendor State` | PrepInstructionsVendorState |

### 3. Reglas por fila (a partir de la fila 2)

- **ASIN vacío:** la fila se cuenta como error, se agrega a la lista de errores con mensaje `"ASIN is required"` y se continúa con la siguiente.
- **ASIN ya existente para la misma cuenta:** no se inserta de nuevo; se cuenta como **duplicado** (registro omitido).
- **Cualquier otra excepción al procesar la fila:** se cuenta como error y se agrega a `Errores` con el mensaje de la excepción.

---

## Respuestas HTTP y JSON por escenario

A continuación se describen **todos** los escenarios posibles y el **JSON completo** que devuelve el API en cada uno. El código HTTP y la presencia de `data` o `data === null` indican el tipo de resultado.

---

### Escenario 1: Éxito total (200) — todas las filas insertadas

- **Cuándo:** Excel válido, columnas correctas, todas las filas con ASIN válido y sin duplicados ni excepciones.
- **HTTP:** `200 OK`
- **Cuerpo:** `status === true`, `data` con el resultado, `errores` vacío.

**JSON de respuesta:**

```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "totalFilasLeidas": 10,
    "registrosCargadosOk": 10,
    "registrosDuplicados": 0,
    "registrosConError": 0,
    "errores": []
  },
  "message": "Bulk upload completed. 10 records loaded successfully, 0 duplicates skipped, 0 errors.",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

---

### Escenario 2: Éxito parcial (200) — algunas filas con error o duplicadas

- **Cuándo:** El procesamiento termina pero hay filas omitidas por duplicado o filas que fallaron (ASIN vacío u otra excepción).
- **HTTP:** `200 OK`
- **Cuerpo:** `status === true`, `data` con totales y array `errores` con al menos un elemento por cada fila que falló.

**JSON de respuesta (ejemplo con 2 errores y 3 duplicados):**

```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "totalFilasLeidas": 150,
    "registrosCargadosOk": 145,
    "registrosDuplicados": 3,
    "registrosConError": 2,
    "errores": [
      {
        "fila": 23,
        "asin": "",
        "error": "ASIN is required"
      },
      {
        "fila": 89,
        "asin": "B0818K2G87",
        "error": "The connection was closed."
      }
    ]
  },
  "message": "Bulk upload completed. 145 records loaded successfully, 3 duplicates skipped, 2 errors.",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

**Formato de cada elemento de `data.errores`:**

| Propiedad | Tipo   | Descripción |
|-----------|--------|-------------|
| `fila`   | number | Número de fila en el Excel (fila 1 = encabezados; la 2 es la primera de datos). |
| `asin`   | string | Valor del ASIN en esa fila; puede ser `""` si el error fue "ASIN is required". |
| `error`  | string | Mensaje de error (ej. `"ASIN is required"` o mensaje de excepción del servidor). |

---

### Escenario 3: Éxito (200) — solo duplicados, nada nuevo insertado

- **Cuándo:** Se envía de nuevo el mismo Excel (o uno cuyos ASINs ya existen para esa cuenta). No se inserta ninguna fila nueva.
- **HTTP:** `200 OK`
- **Cuerpo:** `registrosCargadosOk === 0`, `registrosDuplicados > 0`, `errores` vacío o con solo filas inválidas.

**JSON de respuesta (ejemplo):**

```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "totalFilasLeidas": 50,
    "registrosCargadosOk": 0,
    "registrosDuplicados": 50,
    "registrosConError": 0,
    "errores": []
  },
  "message": "Bulk upload completed. 0 records loaded successfully, 50 duplicates skipped, 0 errors.",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

---

### Escenario 4: Éxito (200) — Excel solo con encabezados (sin filas de datos)

- **Cuándo:** El archivo tiene solo la fila de encabezados (o se considera que no hay filas de datos).
- **HTTP:** `200 OK`
- **Cuerpo:** `totalFilasLeidas === 0`, resto de totales en 0, `errores` vacío.

**JSON de respuesta:**

```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "totalFilasLeidas": 0,
    "registrosCargadosOk": 0,
    "registrosDuplicados": 0,
    "registrosConError": 0,
    "errores": []
  },
  "message": "Bulk upload completed. 0 records loaded successfully, 0 duplicates skipped, 0 errors.",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

---

### Escenario 5: Error 400 — `amazonAccountId` inválido (≤ 0)

- **Cuándo:** Se envía `amazonAccountId` igual a 0 o negativo.
- **HTTP:** `400 Bad Request`
- **Cuerpo:** `status === false`, `data === null`, `message` con el texto de validación.

**JSON de respuesta:**

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "AmazonAccountId must be greater than 0",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

*Nota: en respuestas de validación el backend puede devolver `statusCode: 500` en el cuerpo; el código de referencia es el **HTTP status** (400).*

---

### Escenario 6: Error 400 — archivo no enviado

- **Cuándo:** No se envía la parte `excelFile` en el multipart.
- **HTTP:** `400 Bad Request`
- **Cuerpo:** `data === null`, `message` indicando que falta el archivo.

**JSON de respuesta:**

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Excel file is required",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

---

### Escenario 7: Error 400 — archivo con extensión no permitida

- **Cuándo:** El archivo no es `.xlsx` ni `.xls` (ej. `.csv`, `.pdf`, `.txt`).
- **HTTP:** `400 Bad Request`
- **Cuerpo:** `data === null`.

**JSON de respuesta:**

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "File must be an Excel file (.xlsx or .xls)",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

---

### Escenario 8: Error 400 — archivo vacío (tamaño 0)

- **Cuándo:** Se envía un archivo con 0 bytes.
- **HTTP:** `400 Bad Request`
- **Cuerpo:** `data === null`.

**JSON de respuesta:**

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Excel file cannot be empty",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

---

### Escenario 9: Error 400 — varias validaciones fallidas a la vez

- **Cuándo:** Por ejemplo no se envía archivo y además `amazonAccountId` es 0.
- **HTTP:** `400 Bad Request`
- **Cuerpo:** `message` con varios mensajes concatenados por coma y espacio.

**JSON de respuesta (ejemplo):**

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "AmazonAccountId must be greater than 0, Excel file is required",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

---

### Escenario 10: Error 400 — columnas obligatorias faltantes en el Excel

- **Cuándo:** En la primera fila del Excel falta la columna "ASIN" y/o "Product Title".
- **HTTP:** `400 Bad Request`
- **Cuerpo:** `data === null`, `message` indicando qué columnas faltan. En el cuerpo JSON, `statusCode` puede venir como 500; el código de referencia es el HTTP status (400).

**JSON de respuesta (falta solo ASIN):**

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Missing required columns: ASIN",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

**JSON de respuesta (faltan ASIN y Product Title):**

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Missing required columns: ASIN, Product Title",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

---

### Escenario 11: Error 400 — cuenta Amazon inexistente

- **Cuándo:** `amazonAccountId` es válido (> 0) pero no existe en base de datos.
- **HTTP:** `400 Bad Request`
- **Cuerpo:** `data === null`, `message` con el ID usado. En el cuerpo JSON, `statusCode` puede venir como 500; el código de referencia es el HTTP status (400).

**JSON de respuesta (ejemplo para ID 99999):**

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Amazon Account with ID 99999 does not exist.",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

---

### Escenario 12: Error 401 — no autorizado

- **Cuándo:** El endpoint está protegido y no se envía `Authorization: Bearer {token}` o el token es inválido/expirado.
- **HTTP:** `401 Unauthorized`
- **Cuerpo:** En este endpoint el controlador no devuelve 401 explícitamente; si el API global devuelve 401, el cuerpo puede ser el envelope estándar con `status: false`, `data: null` y un mensaje de no autorizado. No se procesa el archivo.

**JSON de respuesta (típico de middleware de autenticación):**

```json
{
  "status": false,
  "statusCode": 401,
  "data": null,
  "message": "Unauthorized",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

*El mensaje exacto puede variar según la configuración del API.*

---

### Escenario 13: Error 500 — fallo inesperado al procesar el Excel

- **Cuándo:** Excepción no controlada (lectura del archivo, base de datos, etc.). El mensaje expuesto es genérico; el detalle queda en logs del servidor.
- **HTTP:** `500 Internal Server Error`
- **Cuerpo:** `data === null`, `message` fijo.

**JSON de respuesta:**

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "An error occurred while processing the Excel file. Please check the file format and try again.",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

---

## Resumen: códigos HTTP y contenido de `data`

| HTTP | `status` | `data` | Uso |
|------|----------|--------|-----|
| 200 | `true` | Objeto con `totalFilasLeidas`, `registrosCargadosOk`, `registrosDuplicados`, `registrosConError`, `errores[]` | Procesamiento completado (con o sin errores/duplicados por fila). |
| 400 | `false` | `null` | Validación de request o Excel (parámetros, archivo, columnas, cuenta inexistente). |
| 401 | `false` | `null` | No autorizado (si el endpoint está protegido). |
| 500 | `false` | `null` | Error interno al procesar el Excel. |

---

## Comportamiento de idempotencia

- Para cada fila se comprueba si ya existe un registro con el mismo `(AmazonAccountId, Asin)`.
- Si existe, la fila **no** se inserta de nuevo y se cuenta en `registrosDuplicados`.
- Si se vuelve a enviar el mismo Excel, solo se insertan los ASINs que aún no existan para esa cuenta; el resto se considera duplicado.

---

## Formato del Excel

- **Hoja:** se usa la primera hoja del libro (`Worksheets[0]`).
- **Fila 1:** encabezados (nombres de columna). Comparación **case-insensitive**.
- **Filas 2 en adelante:** datos; cada fila es un ASIN (o un intento de ASIN).
- **Celdas vacías:** se tratan como valor ausente (NULL o vacío según el campo).
- **Fechas:** en "Release Date" se aceptan formatos que el servidor pueda interpretar como fecha.

---

## Límites y consideraciones

- El tamaño máximo del body multipart lo define el servidor (en el proyecto actual: 10 MB por defecto para `MultipartBodyLengthLimit`). Archivos mayores pueden devolver 413 o error de conexión.
- Nombres de columna con espacios o puntuación deben coincidir exactamente con los de la tabla (ej. `Product Title`, `Parent ASIN`, `Replenishment Category`).
- Columnas de catálogo con formato "código + espacio + descripción" se dividen en el primer espacio; el resto es la descripción (ej. `2000 Hardware` → código `2000`, nombre `Hardware`).
- En respuestas 400 por validación (FluentValidation), el cuerpo puede incluir `statusCode: 500`; el código de referencia para el cliente es siempre el **código HTTP** de la respuesta.

---

## Referencia rápida: propiedades del JSON

**Envelope (todas las respuestas):**  
`status` (boolean), `statusCode` (number), `data` (object | null), `message` (string), `errorNumber` (string | null), `timestamp` (string).

**Cuando `data` no es null (solo en 200):**  
`totalFilasLeidas` (number), `registrosCargadosOk` (number), `registrosDuplicados` (number), `registrosConError` (number), `errores` (array).

**Cada elemento de `errores`:**  
`fila` (number), `asin` (string), `error` (string).
