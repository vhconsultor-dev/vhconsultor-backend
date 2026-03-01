# API: Carga masiva de ASINs (Amazon Account Asins)

Documentación del endpoint de carga masiva de ASINs desde archivo Excel, para consumo desde el frontend.

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

## Respuesta exitosa (200 OK)

Cuando la petición es válida y el procesamiento termina (con o sin errores por fila), el API responde **200 OK** con un cuerpo con estructura estándar del API.

### Estructura genérica de la respuesta

```json
{
  "status": true,
  "statusCode": 200,
  "data": { ... },
  "message": "string",
  "errorNumber": null,
  "timestamp": "2025-02-28T12:00:00.000"
}
```

- **status:** `true` en 200.
- **statusCode:** `200`.
- **data:** objeto con el resultado de la carga (ver abajo).
- **message:** texto descriptivo con cantidades (ej. registros cargados, duplicados, errores).
- **errorNumber:** `null` en éxito.
- **timestamp:** fecha/hora del servidor (Costa Rica).

### Contenido de `data` (BulkUploadResult)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `totalFilasLeidas` | number | Total de filas de datos (excluyendo la fila de encabezados). |
| `registrosCargadosOk` | number | Cantidad de registros insertados correctamente. |
| `registrosDuplicados` | number | Cantidad de filas omitidas porque el ASIN ya existía para esa cuenta. |
| `registrosConError` | number | Cantidad de filas que fallaron (ASIN vacío o excepción). |
| `errores` | array | Lista de objetos con detalle de cada fila que falló. |

### Objeto de un elemento de `errores` (BulkUploadError)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `fila` | number | Número de fila en el Excel (incluyendo header = fila 1). |
| `asin` | string | Valor del ASIN en esa fila (puede ser vacío si el error fue "ASIN is required"). |
| `error` | string | Mensaje de error (ej. `"ASIN is required"` o mensaje de excepción). |

### Ejemplo de respuesta 200

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

---

## Respuestas de error

Todas las respuestas de error usan la misma estructura genérica; `data` puede ser `null` y `status` es `false`.

### 400 Bad Request

- **Cuándo:** validación del request (FluentValidation) o validación del Excel (columnas obligatorias o cuenta inexistente).
- **statusCode:** `400`
- **message:** mensaje de validación (ej. `"AmazonAccountId must be greater than 0"`, `"Missing required columns: ASIN"`, `"Amazon Account with ID X does not exist."`).

Ejemplo:

```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Missing required columns: ASIN",
  "errorNumber": null,
  "timestamp": "2025-02-28T18:30:00.000"
}
```

### 500 Internal Server Error

- **Cuándo:** error inesperado al procesar el archivo (lectura del Excel, base de datos, etc.).
- **statusCode:** `500`
- **message:** mensaje genérico: `"An error occurred while processing the Excel file. Please check the file format and try again."`
- El detalle técnico no se expone en la respuesta (solo se registra en logs del servidor).

Ejemplo:

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

## Resumen de códigos HTTP

| Código | Significado |
|--------|-------------|
| 200 | Procesamiento completado. Revisar `data.registrosCargadosOk`, `data.registrosDuplicados`, `data.registrosConError` y `data.errores`. |
| 400 | Request inválido (parámetros, archivo, columnas obligatorias o cuenta inexistente). |
| 401 | No autorizado (si el endpoint está protegido y no se envía o el token es inválido). |
| 500 | Error interno del servidor al procesar el Excel. |

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

---

## Resumen rápido para el frontend

1. **URL:** `POST /api/corporate/amazon-account-asins/bulk-upload`
2. **Content-Type:** `multipart/form-data`
3. **Campos:** `amazonAccountId` (número), `excelFile` (archivo .xlsx o .xls).
4. **200:** `data` contiene `totalFilasLeidas`, `registrosCargadosOk`, `registrosDuplicados`, `registrosConError`, `errores[]` con `fila`, `asin`, `error`.
5. **400:** mensaje en `message` (validación o columnas faltantes).
6. **500:** mensaje genérico; no se expone detalle del error.
7. Idempotencia: duplicados por (cuenta + ASIN) se omiten y se cuentan en `registrosDuplicados`.
