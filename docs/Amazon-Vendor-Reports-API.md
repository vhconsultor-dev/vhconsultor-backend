# API de Reportes de Amazon Vendor

## Descripción General

Esta API permite generar, consultar el estado y descargar reportes de Amazon Vendor Central. Los reportes proporcionan información detallada sobre tráfico, inventario y otros datos analíticos de tu cuenta de vendor en Amazon.

## Autenticación

Todos los endpoints requieren:
1. **JWT Token de Brand Partner**: Header `Authorization: Bearer {token}` - obtenido del login de Brand Partner
2. **Amazon Access Token**: Header `x-amz-access-token: {accessToken}` - obtenido del endpoint de generación de tokens de Amazon

## Flujo General de Uso

Para cualquier reporte, el flujo es:

1. **Generar Access Token**: Llamar al endpoint `/api/amazon/vendoraccesstoken/generate-token-by-customer` con tus credenciales de Brand Partner
2. **Solicitar Reporte**: Usar el access token para llamar al endpoint específico del reporte que deseas generar
3. **Consultar Estado**: Consultar periódicamente el estado del reporte usando el `reportId` obtenido
4. **Descargar Reporte**: Una vez que el estado sea `DONE`, usar el `reportDocumentId` para descargar el contenido del reporte

---

## Reportes Disponibles

### 1. Reporte de Inventario (GET_VENDOR_INVENTORY_REPORT)

Este reporte proporciona datos de inventario a nivel agregado y por ASIN.

#### Endpoint

```
POST /api/amazon/vendor/reports/vendor-inventory
```

#### Headers Requeridos

```
Authorization: Bearer {brandPartnerJWT}
x-amz-access-token: {amazonAccessToken}
Content-Type: application/json
```

#### Body de Request

```json
{
  "reportType": "GET_VENDOR_INVENTORY_REPORT",
  "marketplaceIds": ["A1RKKUPIHCS9HS"],
  "dataStartTime": "2026-02-01T00:00:00Z",
  "dataEndTime": "2026-02-28T23:59:59Z",
  "reportOptions": {
    "reportPeriod": "WEEK",
    "sellingProgram": "RETAIL",
    "distributorView": "MANUFACTURING"
  }
}
```

#### Parámetros del Body

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `reportType` | string | Sí | Debe ser `"GET_VENDOR_INVENTORY_REPORT"` |
| `marketplaceIds` | array | Sí | Lista de IDs de marketplace (ej: `["A1RKKUPIHCS9HS"]` para España) |
| `dataStartTime` | string | Sí | Fecha de inicio en formato ISO 8601 (UTC) |
| `dataEndTime` | string | Sí | Fecha de fin en formato ISO 8601 (UTC) |
| `reportOptions.reportPeriod` | string | Sí | Período del reporte: `DAY`, `WEEK`, `MONTH`, `QUARTER`, `YEAR` |
| `reportOptions.sellingProgram` | string | Sí | Programa de ventas (ej: `"RETAIL"`) |
| `reportOptions.distributorView` | string | Sí | Vista de distribuidor (ej: `"MANUFACTURING"`, `"SOURCING"`) |

#### Validaciones Críticas de Fechas

Amazon valida estrictamente que `dataStartTime` y `dataEndTime` correspondan exactamente a los límites del período definido en `reportPeriod`:

##### DAY (Día)
- Se permite cualquier día calendario
- `dataStartTime` y `dataEndTime` deben corresponder al mismo día
- **Ejemplo válido**:
  - Inicio: `2026-02-10T00:00:00Z`
  - Fin: `2026-02-10T23:59:59Z`

##### WEEK (Semana)
- La semana debe ir de **domingo a sábado**
- `dataStartTime` debe ser domingo
- `dataEndTime` debe ser sábado
- **Ejemplo válido**:
  - Inicio: `2026-02-01T00:00:00Z` (domingo)
  - Fin: `2026-02-07T23:59:59Z` (sábado)
- **Ejemplo inválido**:
  - Inicio: `2026-02-02T00:00:00Z` (lunes) ❌
  - Fin: `2026-02-08T23:59:59Z` (domingo) ❌

##### MONTH (Mes)
- Debe abarcar el mes completo
- `dataStartTime` debe ser el **primer día del mes**
- `dataEndTime` debe ser el **último día del mes**
- **Ejemplo válido**:
  - Inicio: `2026-02-01T00:00:00Z`
  - Fin: `2026-02-28T23:59:59Z`
- **Ejemplo inválido**:
  - Inicio: `2026-02-05T00:00:00Z` ❌
  - Fin: `2026-02-25T23:59:59Z` ❌

##### QUARTER (Trimestre)
- Debe corresponder a trimestres calendario completos:
  - Q1: enero 1 – marzo 31
  - Q2: abril 1 – junio 30
  - Q3: julio 1 – septiembre 30
  - Q4: octubre 1 – diciembre 31
- **Ejemplo válido Q1**:
  - Inicio: `2026-01-01T00:00:00Z`
  - Fin: `2026-03-31T23:59:59Z`
- **Ejemplo inválido**:
  - Inicio: `2026-01-15T00:00:00Z` ❌
  - Fin: `2026-03-15T23:59:59Z` ❌

##### YEAR (Año)
- Debe corresponder al año completo
- **Ejemplo válido**:
  - Inicio: `2025-01-01T00:00:00Z`
  - Fin: `2025-12-31T23:59:59Z`

#### Disponibilidad de Datos

⚠️ **IMPORTANTE**: La información del reporte de inventario se publica aproximadamente **72 horas después del cierre del período**.

Si solicitas el reporte antes de que transcurran 72 horas, el estado del reporte puede ser `FATAL` porque los datos aún no están disponibles.

**Ejemplo**:
- Si solicitas datos de la semana del 1-7 de febrero
- Los datos estarán disponibles aproximadamente el 10 de febrero (72 horas después del 7)
- Si solicitas el reporte el 8 de febrero, probablemente obtendrás estado `FATAL`

#### Respuesta Exitosa (Status 200)

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Reporte de inventario generado exitosamente",
  "result": {
    "reportId": "142013020536"
  }
}
```

#### Respuestas de Error

##### Error 400 - Falta Access Token
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "El header 'x-amz-access-token' es requerido. Debes proporcionar el access token de Amazon generado previamente."
}
```

##### Error 400 - Validación de Campos
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "El periodo del reporte debe ser DAY, WEEK, MONTH, QUARTER o YEAR, La fecha de inicio debe estar en formato ISO 8601 (ejemplo: 2026-02-01T00:00:00Z)"
}
```

##### Error 400 - Error de Amazon (fechas incorrectas)
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Amazon respondió con error. Status: BadRequest. Detalle: {detalles del error de Amazon}"
}
```

**Motivos comunes**:
- Las fechas no corresponden a los límites del período (ej: semana que no va de domingo a sábado)
- El rango solicitado excede los límites máximos de Amazon
- Los datos aún no están disponibles (menos de 72 horas desde el cierre del período)

---

### 2. Reporte de Tráfico (GET_VENDOR_TRAFFIC_REPORT)

Este reporte proporciona métricas de tráfico y rendimiento de productos.

#### Endpoint

```
POST /api/amazon/vendor/reports/vendor-traffic
```

#### Headers Requeridos

```
Authorization: Bearer {brandPartnerJWT}
x-amz-access-token: {amazonAccessToken}
Content-Type: application/json
```

#### Body de Request

```json
{
  "reportType": "GET_VENDOR_TRAFFIC_REPORT",
  "marketplaceIds": ["A1RKKUPIHCS9HS"],
  "dataStartTime": "2026-02-01T00:00:00Z",
  "dataEndTime": "2026-02-28T23:59:59Z",
  "reportOptions": {
    "reportPeriod": "WEEK"
  }
}
```

#### Parámetros del Body

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `reportType` | string | Sí | Debe ser `"GET_VENDOR_TRAFFIC_REPORT"` |
| `marketplaceIds` | array | Sí | Lista de IDs de marketplace |
| `dataStartTime` | string | Sí | Fecha de inicio en formato ISO 8601 (UTC) |
| `dataEndTime` | string | Sí | Fecha de fin en formato ISO 8601 (UTC) |
| `reportOptions.reportPeriod` | string | Sí | Período del reporte: `DAY`, `WEEK`, o `MONTH` |

**Nota**: Para el reporte de tráfico, las validaciones de fecha son las mismas que para el reporte de inventario (ver sección anterior), pero solo aplican los períodos `DAY`, `WEEK` y `MONTH`.

#### Respuesta Exitosa (Status 200)

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Reporte de tráfico generado exitosamente",
  "result": {
    "reportId": "142013020500"
  }
}
```

---

## Consultar Estado de Reporte

Una vez generado el reporte, debes consultar su estado periódicamente.

### Endpoint

```
GET /api/amazon/vendor/reports/status?reportId={reportId}
```

### Headers Requeridos

```
Authorization: Bearer {brandPartnerJWT}
x-amz-access-token: {amazonAccessToken}
```

### Parámetros de Query

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `reportId` | string | Sí | El ID del reporte obtenido al generar el reporte |

### Respuesta Exitosa (Status 200)

#### Reporte en Proceso (IN_QUEUE o IN_PROGRESS)

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "El reporte está siendo procesado por Amazon. Intenta nuevamente en unos momentos.",
  "result": {
    "reportType": "GET_VENDOR_INVENTORY_REPORT",
    "processingStatus": "IN_PROGRESS",
    "reportId": "142013020536",
    "reportDocumentId": null,
    "marketplaceIds": ["A1RKKUPIHCS9HS"],
    "dataStartTime": "2026-02-01T00:00:00Z",
    "dataEndTime": "2026-02-28T23:59:59Z",
    "createdTime": "2026-03-17T10:30:00Z",
    "processingStartTime": "2026-03-17T10:30:05Z",
    "processingEndTime": null
  }
}
```

#### Reporte Completado (DONE)

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Reporte completado exitosamente. Document ID: amzn1.spdoc.1.4.eu.c9e08a92-0a5e-408f-b5cb-e04bc60e4f8a.TC55BKSPVBKQ7.75300",
  "result": {
    "reportType": "GET_VENDOR_INVENTORY_REPORT",
    "processingStatus": "DONE",
    "reportId": "142013020536",
    "reportDocumentId": "amzn1.spdoc.1.4.eu.c9e08a92-0a5e-408f-b5cb-e04bc60e4f8a.TC55BKSPVBKQ7.75300",
    "marketplaceIds": ["A1RKKUPIHCS9HS"],
    "dataStartTime": "2026-02-01T00:00:00Z",
    "dataEndTime": "2026-02-28T23:59:59Z",
    "createdTime": "2026-03-17T10:30:00Z",
    "processingStartTime": "2026-03-17T10:30:05Z",
    "processingEndTime": "2026-03-17T10:35:20Z"
  }
}
```

⚠️ **Guarda el `reportDocumentId`** - lo necesitarás para descargar el reporte.

#### Reporte con Error (FATAL)

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Ocurrió un error fatal al procesar el reporte.",
  "result": {
    "reportType": "GET_VENDOR_INVENTORY_REPORT",
    "processingStatus": "FATAL",
    "reportId": "142013020536",
    "reportDocumentId": null,
    "marketplaceIds": ["A1RKKUPIHCS9HS"],
    "dataStartTime": "2026-02-01T00:00:00Z",
    "dataEndTime": "2026-02-28T23:59:59Z",
    "createdTime": "2026-03-17T10:30:00Z",
    "processingStartTime": "2026-03-17T10:30:05Z",
    "processingEndTime": "2026-03-17T10:30:15Z"
  }
}
```

**Posibles causas de estado FATAL**:
- Fechas que no corresponden a los límites del período
- Datos aún no disponibles (menos de 72 horas desde el cierre del período)
- Problemas de permisos en la cuenta de Amazon
- Parámetros inválidos en las opciones del reporte

### Estados Posibles

| Estado | Descripción | Acción Recomendada |
|--------|-------------|-------------------|
| `IN_QUEUE` | El reporte está en cola esperando ser procesado | Esperar y consultar nuevamente en 30-60 segundos |
| `IN_PROGRESS` | El reporte está siendo procesado | Esperar y consultar nuevamente en 30-60 segundos |
| `DONE` | El reporte está completo y listo para descargar | Usar el `reportDocumentId` para descargar |
| `CANCELLED` | El reporte fue cancelado | Generar un nuevo reporte si es necesario |
| `FATAL` | Ocurrió un error fatal al procesar | Verificar parámetros y fechas, generar nuevo reporte |

---

## Descargar Reporte

Cuando el estado del reporte sea `DONE`, puedes descargar el contenido del reporte.

### Endpoint

```
GET /api/amazon/vendor/reports/download?reportDocumentId={reportDocumentId}
```

### Headers Requeridos

```
Authorization: Bearer {brandPartnerJWT}
x-amz-access-token: {amazonAccessToken}
```

### Parámetros de Query

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `reportDocumentId` | string | Sí | El ID del documento obtenido cuando el estado es DONE |

### Respuesta Exitosa (Status 200)

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Reporte descargado y descomprimido exitosamente. Documento ID: amzn1.spdoc.1.4.eu...",
  "result": {
    "reportSpecification": {
      "reportType": "GET_VENDOR_INVENTORY_REPORT",
      "reportOptions": {
        "reportPeriod": "WEEK",
        "sellingProgram": "RETAIL",
        "distributorView": "MANUFACTURING"
      },
      "dataStartTime": "2026-02-01T00:00:00Z",
      "dataEndTime": "2026-02-07T23:59:59Z",
      "marketplaceIds": ["A1RKKUPIHCS9HS"]
    },
    "inventoryData": [
      {
        "asin": "B08EXAMPLE1",
        "productName": "Example Product",
        "availableInventory": 150,
        "sellableOnHandInventory": 140,
        "openPurchaseOrderQuantity": 50,
        "averageDailyUnits": 5.2
      }
    ]
  }
}
```

**Nota**: La estructura del `result` varía según el tipo de reporte. El API se encarga de descomprimir el archivo GZIP de Amazon y retornar el JSON contenido.

### Respuestas de Error

##### Error 400 - Falta reportDocumentId
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "El parámetro 'reportDocumentId' es requerido. Debes proporcionar el ID del documento obtenido cuando el estado del reporte es DONE."
}
```

##### Error 400 - Documento No Encontrado
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "El documento de reporte 'amzn1.spdoc...' no fue encontrado en Amazon. Verifica que el reportDocumentId sea correcto y que el reporte tenga estado DONE."
}
```

##### Error 400 - URL Expirada
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Error al descargar el archivo del reporte desde S3. Status: Forbidden. La URL de descarga puede haber expirado. Obtén un nuevo reportDocumentId."
}
```

**Nota**: Las URLs pre-firmadas de Amazon S3 expiran después de un tiempo. Si obtienes este error, vuelve a consultar el estado del reporte para obtener un nuevo `reportDocumentId`.

---

## Ejemplo de Flujo Completo

### Paso 1: Generar Access Token

```bash
POST /api/amazon/vendoraccesstoken/generate-token-by-customer
Authorization: Bearer {brandPartnerJWT}
Content-Type: application/json

{
  "email": "usuario@ejemplo.com",
  "password": "miPassword123"
}

# Respuesta
{
  "result": {
    "accessToken": "Atza|IwEBIL...",
    "expiresIn": 3600
  }
}
```

### Paso 2: Solicitar Reporte de Inventario

```bash
POST /api/amazon/vendor/reports/vendor-inventory
Authorization: Bearer {brandPartnerJWT}
x-amz-access-token: Atza|IwEBIL...
Content-Type: application/json

{
  "reportType": "GET_VENDOR_INVENTORY_REPORT",
  "marketplaceIds": ["A1RKKUPIHCS9HS"],
  "dataStartTime": "2026-02-01T00:00:00Z",
  "dataEndTime": "2026-02-07T23:59:59Z",
  "reportOptions": {
    "reportPeriod": "WEEK",
    "sellingProgram": "RETAIL",
    "distributorView": "MANUFACTURING"
  }
}

# Respuesta
{
  "result": {
    "reportId": "142013020536"
  }
}
```

### Paso 3: Consultar Estado (cada 30-60 segundos)

```bash
GET /api/amazon/vendor/reports/status?reportId=142013020536
Authorization: Bearer {brandPartnerJWT}
x-amz-access-token: Atza|IwEBIL...

# Primera consulta - En proceso
{
  "message": "El reporte está siendo procesado por Amazon...",
  "result": {
    "processingStatus": "IN_PROGRESS"
  }
}

# Consulta posterior - Completado
{
  "message": "Reporte completado exitosamente. Document ID: amzn1.spdoc...",
  "result": {
    "processingStatus": "DONE",
    "reportDocumentId": "amzn1.spdoc.1.4.eu.c9e08a92..."
  }
}
```

### Paso 4: Descargar Reporte

```bash
GET /api/amazon/vendor/reports/download?reportDocumentId=amzn1.spdoc.1.4.eu.c9e08a92...
Authorization: Bearer {brandPartnerJWT}
x-amz-access-token: Atza|IwEBIL...

# Respuesta
{
  "isSuccess": true,
  "message": "Reporte descargado y descomprimido exitosamente",
  "result": {
    "reportSpecification": {...},
    "inventoryData": [...]
  }
}
```

---

## Notas Importantes

### Expiración de Tokens
- El **Access Token de Amazon** expira después de 3600 segundos (1 hora)
- Si tu proceso de consulta de estado toma más de 1 hora, necesitarás generar un nuevo access token
- El **JWT de Brand Partner** tiene su propia expiración según la configuración del sistema

### Estrategia de Polling (Consulta de Estado)
- **No hagas polling continuo**: Amazon puede limitar las peticiones
- **Intervalo recomendado**: 30-60 segundos entre consultas
- **Timeout recomendado**: Establecer un límite máximo (ej: 15 minutos) para evitar bucles infinitos

### Expiración de URLs Pre-firmadas
- Las URLs de descarga de S3 tienen un tiempo de vida limitado
- Si obtienes un error de URL expirada, vuelve a consultar el estado para obtener un nuevo `reportDocumentId`

### Formato de Fechas
- Todas las fechas deben estar en formato **ISO 8601** con zona horaria UTC
- Formato: `YYYY-MM-DDTHH:mm:ssZ`
- Ejemplo: `2026-02-01T00:00:00Z`

### Períodos de Reporte
- **Reporte de Inventario**: DAY, WEEK, MONTH, QUARTER, YEAR
- **Reporte de Tráfico**: DAY, WEEK, MONTH
- Las fechas deben corresponder **exactamente** a los límites del período

### Autenticación de Dos Niveles
1. **Brand Partner JWT**: Valida que eres un usuario autorizado del sistema
2. **Amazon Access Token**: Valida que tienes permisos para acceder a los datos de Amazon de ese customer

Ambos son necesarios para todas las operaciones de reportes.

### Marketplace IDs Comunes

| País | Marketplace ID |
|------|----------------|
| España | A1RKKUPIHCS9HS |
| Alemania | A1PA6795UKMFR9 |
| Francia | A13V1IB3VIYZZH |
| Italia | APJ6JRA9NG5V4 |
| Reino Unido | A1F83G8C2ARO7P |
| Estados Unidos | ATVPDKIKX0DER |

### Disponibilidad de Datos

Cada tipo de reporte tiene diferentes tiempos de disponibilidad:
- **Reporte de Inventario**: ~72 horas después del cierre del período
- **Reporte de Tráfico**: Consultar documentación específica de Amazon

Planifica tus solicitudes considerando estos tiempos para evitar reportes con estado `FATAL` por datos no disponibles.

---

## Variables de Entorno Requeridas

Para que los endpoints funcionen correctamente, el servidor debe tener configuradas las siguientes variables de entorno en Azure:

```
AmazonVendorReports__AccessKey=AKIAXXXXXXXXXXXXXXXX
AmazonVendorReports__SecretKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
AmazonVendorReports__AwsRegion=eu-west-1
AmazonVendorReports__ServiceName=execute-api
AmazonVendorReports__BaseUrl=https://sellingpartnerapi-eu.amazon.com/reports/2021-06-30
```

Estas credenciales son gestionadas por el equipo de infraestructura y no deben ser incluidas en los requests del cliente.
