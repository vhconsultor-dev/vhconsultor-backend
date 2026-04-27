# Guia de uso de APIs: Inventario y Settlement (BrandPartner)

Este documento describe de forma funcional como operar el modulo de inventario y settlement de BrandPartner por API.

## Objetivo del modulo

Con estos APIs puedes:
- Cargar inventario inicial o de actualizacion por archivo Excel.
- Crear y editar SKUs manualmente.
- Ajustar inventario manualmente con trazabilidad (auditoria).
- Cargar settlements de Amazon en bulk.
- Consultar historial de cortes (headers), detalle de transacciones y snapshots.
- Detectar lineas que no se aplicaron por riesgo de inventario negativo.

## Base URL y prefijos

- Inventario: `api/brand-partner/inventory`
- Settlement: `api/brand-partner/settlement`

## Estructura de respuesta estandar

Todas las respuestas usan `ResponseStructure`:
- `status`: `true/false`
- `statusCode`: codigo HTTP de negocio
- `data`: payload de respuesta
- `message`: mensaje principal (EN en la mayoria de endpoints nuevos)
- `messageES`: mensaje en espanol (cuando aplica)
- `errorNumber`: correlativo interno de error (cuando se loguea excepcion)
- `timestamp`: fecha/hora de respuesta

## Flujo recomendado de operacion

1. Cargar o mantener inventario base de SKUs (bulk o manual).
2. Validar inventario actual por cuenta (`GET account/{amazonAccountId}`).
3. Cargar settlement en bulk.
4. Revisar respuesta:
   - Si hay `missingSKUs`, no se proceso nada (corregir primero).
   - Si hay `linesRequiringManualAdjustment`, se proceso parcialmente y esas lineas requieren accion manual.
5. Consultar header/detalle/snapshots del corte para auditoria.
6. Ejecutar ajustes manuales de inventario si se requieren correcciones.

---

## APIs de Inventario

## 1) Bulk upload de inventario

- Metodo: `POST /api/brand-partner/inventory/bulk-upload`
- Tipo: `multipart/form-data`
- Parametros:
  - `amazonAccountId` (int, requerido)
  - `excelFile` (archivo, requerido)

### Validaciones clave

- Cuenta Amazon debe existir.
- Archivo no vacio.
- Tamano maximo: 10 MB.
- Formato Excel valido.
- Si una fila no tiene SKU, esa fila marca error y continua.
- Si `Quantity` no es entero valido, esa fila marca error y continua.

### Comportamiento de negocio

- Si el SKU ya existe en esa cuenta:
  - Se actualiza el registro (operacion idempotente a nivel SKU).
  - Se registra movimiento `ADJUSTMENT` con `ReasonCode = BULK_UPDATE`.
- Si el SKU no existe:
  - Se crea el SKU.
  - Se registra movimiento `INITIAL_LOAD` con `ReasonCode = BULK_UPLOAD`.

### Respuesta (resumen)

`data` incluye:
- `success`
- `messageEN`
- `messageES`
- `totalFilasLeidas`
- `registrosCargadosOk`
- `registrosActualizados`
- `registrosConError`
- `errores[]` con `fila`, `sku`, `error`, `errorCode`

---

## 2) Crear SKU manualmente

- Metodo: `POST /api/brand-partner/inventory`
- Tipo: `application/json`

### Body principal

- `amazonAccountId` (requerido)
- `sku` (requerido)
- `asin`, `productName`, `prepOwner`, `labelingOwner`
- `unitsPerBox`, `numberOfBoxes`, `boxLengthIn`, `boxWidthIn`, `boxHeightIn`, `boxWeightLb`
- `quantityOnHand`
- `createdBy`

### Validaciones clave

- `amazonAccountId > 0`
- `sku` requerido y maximo 100 chars.
- Rangos no negativos para campos numericos de empaque.
- La cuenta Amazon debe existir.
- No permite duplicar `sku` dentro de la misma cuenta.

### Efecto

- Crea `InventoryItem`.
- Si `quantityOnHand > 0`, registra movimiento `INITIAL_LOAD`.

---

## 3) Actualizar SKU manualmente

- Metodo: `PUT /api/brand-partner/inventory/{inventoryItemId}`
- Tipo: `application/json`

### Notas

- Actualiza metadatos del item (asin, nombre, empaque).
- No cambia el SKU ni el AmazonAccount.
- Si el `inventoryItemId` no existe, retorna `NOT_FOUND`.

---

## 4) Ajuste manual de inventario

- Metodo: `POST /api/brand-partner/inventory/adjust`
- Tipo: `application/json`

### Body

- `inventoryItemId` (requerido)
- `quantityDelta` (requerido, diferente de 0)
- `reasonCode` (requerido)
- `comments` (opcional, max 1000)
- `createdBy` (requerido)

### ReasonCode permitidos

- `COUNT_CORRECTION`
- `DAMAGE`
- `RETURN_TO_STOCK`
- `LOSS`
- `THEFT`
- `ADJUSTMENT`

### Regla critica

- Si `quantityBefore + quantityDelta < 0`, el ajuste se rechaza.
- Se retorna error explicito EN/ES indicando que resultaria en inventario negativo.

### Efecto

- Actualiza `QuantityOnHand`.
- Inserta `InventoryMovement` tipo `MANUAL`.

---

## 5) Consultar inventario por cuenta

- Metodo: `GET /api/brand-partner/inventory/account/{amazonAccountId}`
- Query params opcionales:
  - `sku`
  - `asin`
  - `prepOwner`
  - `pageNumber` (default 1)
  - `pageSize` (default 50)

### Respuesta

- `data.items[]`
- `data.pageNumber`
- `data.pageSize`
- `data.count`

---

## 6) Consultar item por ID

- Metodo: `GET /api/brand-partner/inventory/{inventoryItemId}`
- Si no existe, retorna `404` con `NOT_FOUND`.

---

## 7) Consultar movimientos (auditoria)

- Metodo: `GET /api/brand-partner/inventory/{inventoryItemId}/movements`
- Query params opcionales:
  - `movementType`
  - `dateFrom`
  - `dateTo`
  - `pageNumber` (default 1)
  - `pageSize` (default 100)

### Uso clave

Permite auditar absolutamente todo lo que cambio el inventario: cargas iniciales, updates bulk, ajustes manuales y afectaciones por settlement.

---

## APIs de Settlement

## 1) Bulk upload de settlement

- Metodo: `POST /api/brand-partner/settlement/bulk-upload`
- Tipo: `multipart/form-data`
- Parametros:
  - `amazonAccountId` (int, requerido)
  - `excelFile` (archivo, requerido)

## Validaciones y condicionales (orden de ejecucion)

1. La cuenta Amazon debe existir.
2. Archivo no vacio.
3. Tamano maximo 10 MB.
4. Excel valido (se puede abrir).
5. Debe tener hoja y filas.
6. Debe venir `settlement-id` en la primera fila de datos.
7. Ese `settlement-id` no debe existir previamente para la cuenta.
8. Deben existir todos los SKUs presentes en el archivo:
   - Si falta uno o mas, se bloquea TODO el bulk (no procesa nada).

## Regla de inventario por linea

Una linea solo afecta inventario si:
- Tiene `sku`.
- `quantityPurchased` tiene valor y no es 0.
- `transactionType` es `ORDER` o `REFUND`.

Calculo:
- `ORDER`: resta inventario (delta negativo).
- `REFUND`: suma inventario (delta positivo).

Si aplicar una linea dejaria inventario negativo:
- Esa linea no afecta inventario.
- Se reporta en `linesRequiringManualAdjustment`.
- El proceso global continua para el resto de lineas.

## Idempotencia

- Se calcula `rowHash` por linea con datos clave.
- Si ya existe el mismo hash para ese settlement header, la linea se omite (`linesSkipped`).

## Efectos al procesar

- Crea `SettlementHeader`.
- Inserta `SettlementDetails` por linea.
- Registra `InventoryMovements` para lineas que si afectan inventario.
- Genera `InventorySnapshots` por SKU afectado en el corte.

## Respuesta funcional esperada

`data` incluye:
- `success`
- `messageEN`
- `messageES`
- `settlementId`
- `totalLinesProcessed`
- `linesCreated`
- `linesSkipped`
- `skusAffected`
- `missingSKUs[]`
- `linesRequiringManualAdjustment[]`
- `errores[]`

### Estructura de `linesRequiringManualAdjustment`

Cada item trae:
- `rowNumber`
- `sku`
- `transactionType`
- `quantityRequired`
- `quantityAvailable`
- `deficit`
- `orderId`
- `postedDate`
- `messageEN`
- `messageES`

---

## 2) Listar settlement headers (historial de cortes)

- Metodo: `GET /api/brand-partner/settlement/account/{amazonAccountId}`
- Query params opcionales:
  - `settlementId`
  - `depositDateFrom`
  - `depositDateTo`
  - `currency`
  - `pageNumber` (default 1)
  - `pageSize` (default 50)

---

## 3) Obtener settlement header por ID

- Metodo: `GET /api/brand-partner/settlement/{settlementHeaderId}`
- Si no existe, retorna `404` con `NOT_FOUND`.

---

## 4) Obtener detalles de un settlement

- Metodo: `GET /api/brand-partner/settlement/{settlementHeaderId}/details`
- Query params opcionales:
  - `transactionType`
  - `orderId`
  - `sku`
  - `postedDateFrom`
  - `postedDateTo`
  - `pageNumber` (default 1)
  - `pageSize` (default 100)

---

## 5) Buscar detalles por OrderId

- Metodo: `GET /api/brand-partner/settlement/order/{orderId}`
- Uso tipico: auditoria transversal de una orden en distintos cortes.

---

## 6) Snapshots por settlement

- Metodo: `GET /api/brand-partner/settlement/{settlementHeaderId}/snapshots`
- Devuelve la "foto" del inventario para los SKUs impactados en ese corte.

---

## 7) Snapshots por item y rango de fechas

- Metodo: `GET /api/brand-partner/settlement/snapshots/item/{inventoryItemId}`
- Query params:
  - `dateFrom` (opcional)
  - `dateTo` (opcional)

---

## JSON de ejemplo de respuestas

## Exito en ajuste manual

```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "messageEN": "Inventory adjusted successfully. SKU: ABC-123, Before: 20, Delta: -3, After: 17.",
    "messageES": "Inventario ajustado exitosamente. SKU: ABC-123, Antes: 20, Delta: -3, Después: 17.",
    "quantityBefore": 20,
    "quantityDelta": -3,
    "quantityAfter": 17
  },
  "message": "Inventory adjusted successfully. SKU: ABC-123, Before: 20, Delta: -3, After: 17.",
  "messageES": "Inventario ajustado exitosamente. SKU: ABC-123, Antes: 20, Delta: -3, Después: 17."
}
```

## Settlement bloqueado por SKUs faltantes

```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Cannot process settlement 24741317191. The following 2 SKU(s) do not exist in inventory: [SKU-AAA, SKU-BBB]. Please create these SKUs first using the inventory bulk upload or manual creation endpoint.",
  "messageES": "No se puede procesar el settlement 24741317191. Los siguientes 2 SKU(s) no existen en el inventario: [SKU-AAA, SKU-BBB]. Por favor cree estos SKUs primero usando la carga masiva de inventario o el endpoint de creación manual.",
  "errorNumber": "BULK_UPLOAD_FAILED"
}
```

## Settlement procesado con lineas para ajuste manual

```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "settlementId": "24741317191",
    "totalLinesProcessed": 1400,
    "linesCreated": 1375,
    "linesSkipped": 5,
    "skusAffected": 48,
    "missingSKUs": [],
    "linesRequiringManualAdjustment": [
      {
        "rowNumber": 229,
        "sku": "SKU-NEG-01",
        "transactionType": "ORDER",
        "quantityRequired": 12,
        "quantityAvailable": 4,
        "deficit": 8,
        "orderId": "113-1234567-1234567",
        "postedDate": "2025-10-16",
        "messageEN": "Insufficient inventory...",
        "messageES": "Inventario insuficiente..."
      }
    ]
  },
  "message": "Settlement 24741317191 was processed with warnings...",
  "messageES": "El settlement 24741317191 se procesó con advertencias..."
}
```

---

## Errores comunes y como interpretarlos

- `VALIDATION_ERROR`: request invalido (campos faltantes, formatos no validos).
- `ACCOUNT_NOT_FOUND`: la cuenta Amazon enviada no existe.
- `SETTLEMENT_ALREADY_EXISTS`: settlement-id ya procesado para esa cuenta.
- `BULK_UPLOAD_FAILED`: fallo de negocio del bulk (ej. SKUs faltantes).
- `NOT_FOUND`: recurso puntual no existe (item/header).
- `500`: error inesperado; usar `errorNumber` para soporte interno.

## Buenas practicas operativas

- Mantener sincronizado el catalogo de SKUs antes de cargar settlements.
- Usar `bulk-upload` de inventario cuando entren nuevos SKUs masivamente.
- Usar `adjust` solo para correcciones controladas y con `reasonCode` correcto.
- Revisar siempre `linesRequiringManualAdjustment` despues de cada settlement.
- Consultar `movements` y `snapshots` para auditoria y explicacion de diferencias.

## Alcance de esta guia

Este documento explica el contrato funcional y operativo de los APIs.
No incluye implementacion de frontend ni ejemplos de codigo cliente.
