# Documentación Frontend - Sistema de Pricing

## Introducción

El sistema de Pricing permite gestionar rangos de precios para servicios corporativos y calcular automáticamente el monto a cobrar basado en el presupuesto del cliente. El sistema soporta dos modelos de pricing:

1. **Pricing por Porcentaje**: Se calcula un porcentaje sobre el valor mínimo del rango de presupuesto
2. **Pricing por Monto Fijo**: Se cobra un valor fijo independientemente del presupuesto dentro del rango

**Base URL:** `/api/pricing`

**Autenticación:** Todos los endpoints requieren JWT válido mediante el header `Authorization: Bearer {token}`

---

## Estructura del Sistema

### Tablas Principales

1. **ServiceBudgetRanges**: Rangos de presupuesto anual con porcentajes
   - Almacena rangos (ej: $100,000 - $3,000,000) con un porcentaje (ej: 5%)
   - El cálculo se hace sobre el MinBudgetValue del rango, NO sobre el presupuesto ingresado

2. **ServiceAdBudgetRanges**: Rangos de presupuesto de publicidad con valores fijos
   - Almacena rangos (ej: $100,000 - $500,000) con un valor fijo (ej: $4,000)
   - El monto a cobrar es directamente el FixedQuote

### Tablas de Referencia (Solo Lectura)

- **BusinessTypes**: Tipos de negocio (Amazon, Walmart, TikTok Shop, Consulting)
- **Platforms**: Plataformas dentro de cada BusinessType (Vendor, Seller, etc.)
- **Services**: Servicios corporativos disponibles

---

## Funcionalidades para el Usuario

### 1. Mantenimiento de Rangos de Pricing

Como usuario del sistema, necesitas poder:

- **Ver todos los rangos de pricing** (por porcentaje o por monto fijo)
- **Filtrar rangos** por servicio, tipo de negocio, plataforma o estado activo
- **Ver detalles de un rango específico**
- **Crear nuevos rangos** de pricing
- **Actualizar rangos existentes**
- **Desactivar rangos** (soft delete)

### 2. Calculadora de Pricing

Como usuario del sistema, necesitas poder:

- **Ingresar parámetros**: BusinessType, Platform (opcional), Service, Budget Amount
- **Calcular automáticamente** el monto a cobrar
- **Ver información detallada** del cálculo:
  - Tipo de cálculo usado (porcentaje o fijo)
  - Rango encontrado
  - Monto calculado
  - Información del servicio, business type y platform

---

## Endpoints Disponibles

### MANTENIMIENTO - Rangos por Porcentaje

#### 1. GET /api/pricing/budget-ranges

**Propósito:** Obtener lista de rangos de pricing por porcentaje

**Query Parameters (todos opcionales):**
- `serviceId`: int - Filtrar por servicio
- `businessTypeId`: int - Filtrar por tipo de negocio
- `platformId`: int - Filtrar por plataforma
- `isActive`: bool - Filtrar por estado activo (true/false)

**Ejemplo de Request:**
```
GET /api/pricing/budget-ranges?serviceId=1&businessTypeId=1&isActive=true
Authorization: Bearer {token}
```

**Ejemplo de Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "serviceBudgetRangeId": 1,
      "serviceId": 1,
      "serviceName": "Full Scope Platform Management",
      "businessTypeId": 1,
      "platformId": 2,
      "minBudgetValue": 100000.00,
      "maxBudgetValue": 3000000.00,
      "percentage": 5.00,
      "isActive": true,
      "createdAt": "2024-01-15T10:00:00",
      "updatedAt": null
    }
  ],
  "message": "1 rango(s) de presupuesto encontrado(s)"
}
```

**Uso en Frontend:**
- Pantalla de listado de rangos
- Tabla con filtros en la parte superior
- Cada fila muestra: Servicio, BusinessType, Platform, Rango, Porcentaje, Estado
- Botones de acción: Ver detalles, Editar, Desactivar

---

#### 2. GET /api/pricing/budget-ranges/{id}

**Propósito:** Obtener detalles de un rango específico

**Path Parameter:**
- `id`: int - ID del rango

**Ejemplo de Request:**
```
GET /api/pricing/budget-ranges/1
Authorization: Bearer {token}
```

**Ejemplo de Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "serviceBudgetRangeId": 1,
    "serviceId": 1,
    "serviceName": "Full Scope Platform Management",
    "businessTypeId": 1,
    "platformId": 2,
    "minBudgetValue": 100000.00,
    "maxBudgetValue": 3000000.00,
    "percentage": 5.00,
    "isActive": true,
    "createdAt": "2024-01-15T10:00:00",
    "updatedAt": null
  },
  "message": "Rango de presupuesto obtenido exitosamente"
}
```

**Uso en Frontend:**
- Modal o página de detalles
- Mostrar toda la información del rango
- Botones: Editar, Volver a lista

---

#### 3. POST /api/pricing/budget-ranges

**Propósito:** Crear un nuevo rango de pricing por porcentaje

**Request Body:**
```json
{
  "serviceId": 1,
  "businessTypeId": 1,
  "platformId": 2,
  "minBudgetValue": 100000.00,
  "maxBudgetValue": 3000000.00,
  "percentage": 5.00,
  "isActive": true
}
```

**Campos Requeridos:**
- `serviceId`: int - ID del servicio (debe existir)
- `businessTypeId`: int - ID del tipo de negocio (debe existir)
- `minBudgetValue`: decimal - Valor mínimo del rango (debe ser > 0)
- `percentage`: decimal - Porcentaje a aplicar (debe estar entre 0 y 100)

**Campos Opcionales:**
- `platformId`: int? - ID de la plataforma (puede ser null para rangos generales)
- `maxBudgetValue`: decimal? - Valor máximo del rango (null = rango abierto, ej: "$50M+")
- `isActive`: bool - Estado activo (default: true)

**Validaciones del Backend:**
- serviceId debe existir en Corporate.Services
- businessTypeId debe existir en Corporate.BusinessTypes
- platformId (si se proporciona) debe existir y pertenecer al businessTypeId
- minBudgetValue debe ser > 0
- maxBudgetValue (si se proporciona) debe ser > minBudgetValue
- percentage debe estar entre 0 y 100
- No debe haber solapamiento con rangos existentes del mismo serviceId, businessTypeId y platformId

**Ejemplo de Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "rangeId": 1
  },
  "message": "Rango de presupuesto creado exitosamente"
}
```

**Ejemplo de Response (400 Bad Request - Solapamiento):**
```json
{
  "success": false,
  "data": null,
  "message": "Ya existe un rango que se solapa con los valores proporcionados"
}
```

**Uso en Frontend:**
- Formulario de creación de rango
- Dropdowns para: Service, BusinessType, Platform (opcional)
- Campos numéricos para: MinBudgetValue, MaxBudgetValue (opcional), Percentage
- Checkbox para IsActive
- Validación en frontend antes de enviar
- Mostrar errores del backend si la creación falla

---

#### 4. PUT /api/pricing/budget-ranges/{id}

**Propósito:** Actualizar un rango existente

**Path Parameter:**
- `id`: int - ID del rango a actualizar

**Request Body (todos los campos opcionales, solo se actualizan los proporcionados):**
```json
{
  "serviceId": 1,
  "businessTypeId": 1,
  "platformId": 2,
  "minBudgetValue": 150000.00,
  "maxBudgetValue": 3500000.00,
  "percentage": 6.00,
  "isActive": true
}
```

**Ejemplo de Response (200 OK):**
```json
{
  "success": true,
  "data": null,
  "message": "Rango de presupuesto actualizado exitosamente"
}
```

**Uso en Frontend:**
- Formulario de edición (pre-cargado con datos actuales)
- Mismos campos que el formulario de creación
- Botón "Guardar Cambios"
- Validaciones iguales que en creación

---

#### 5. DELETE /api/pricing/budget-ranges/{id}

**Propósito:** Desactivar un rango (soft delete)

**Path Parameter:**
- `id`: int - ID del rango a desactivar

**Ejemplo de Request:**
```
DELETE /api/pricing/budget-ranges/1
Authorization: Bearer {token}
```

**Ejemplo de Response (200 OK):**
```json
{
  "success": true,
  "data": null,
  "message": "Rango de presupuesto desactivado exitosamente"
}
```

**Uso en Frontend:**
- Botón "Desactivar" en la lista o detalles
- Confirmación antes de desactivar ("¿Está seguro de desactivar este rango?")
- El rango desaparece de la lista si se filtra por isActive=true

---

### MANTENIMIENTO - Rangos por Monto Fijo

Los endpoints para rangos por monto fijo son idénticos en estructura, solo cambian los nombres y campos:

#### 6. GET /api/pricing/ad-budget-ranges
#### 7. GET /api/pricing/ad-budget-ranges/{id}
#### 8. POST /api/pricing/ad-budget-ranges
#### 9. PUT /api/pricing/ad-budget-ranges/{id}
#### 10. DELETE /api/pricing/ad-budget-ranges/{id}

**Diferencias clave:**
- Usan `minAdBudgetValue` y `maxAdBudgetValue` en lugar de `minBudgetValue` y `maxBudgetValue`
- Usan `fixedQuote` (decimal, requerido, debe ser > 0) en lugar de `percentage`
- El `fixedQuote` es el monto fijo a cobrar, no un porcentaje

**Ejemplo de Request Body para POST:**
```json
{
  "serviceId": 2,
  "businessTypeId": 3,
  "platformId": 5,
  "minAdBudgetValue": 100000.00,
  "maxAdBudgetValue": 500000.00,
  "fixedQuote": 4000.00,
  "isActive": true
}
```

---

### CALCULADORA DE PRICING

#### 11. POST /api/pricing/calculate

**Propósito:** Calcular automáticamente el monto a cobrar basado en parámetros del cliente

**Request Body:**
```json
{
  "businessTypeId": 1,
  "platformId": 2,
  "serviceId": 1,
  "budgetAmount": 150000.00
}
```

**Campos Requeridos:**
- `businessTypeId`: int - ID del tipo de negocio
- `serviceId`: int - ID del servicio
- `budgetAmount`: decimal - Monto del presupuesto ingresado por el usuario (debe ser > 0)

**Campos Opcionales:**
- `platformId`: int? - ID de la plataforma (puede ser null)

**Lógica del Backend:**
1. Valida que businessTypeId, serviceId existen
2. Si platformId se proporciona, valida que existe y pertenece al businessTypeId
3. Busca primero en ServiceBudgetRanges (porcentaje):
   - Primero busca con platformId específico
   - Si no encuentra, busca con platformId NULL
   - Busca rango donde: budgetAmount >= MinBudgetValue AND (budgetAmount < MaxBudgetValue OR MaxBudgetValue IS NULL)
4. Si encuentra rango de porcentaje:
   - Calcula: MinBudgetValue * (Percentage / 100)
   - Retorna calculationType: "percentage"
5. Si NO encuentra rango de porcentaje, busca en ServiceAdBudgetRanges (monto fijo):
   - Misma lógica de búsqueda
   - Si encuentra: retorna FixedQuote directamente
   - Retorna calculationType: "fixed"
6. Si no encuentra ningún rango, retorna error 404

**Ejemplo de Response (200 OK - Porcentaje):**
```json
{
  "success": true,
  "calculationType": "percentage",
  "businessType": {
    "businessTypeId": 1,
    "businessTypeName": "Amazon",
    "businessTypeKey": "amazon"
  },
  "platform": {
    "platformId": 2,
    "platformName": "Seller",
    "platformKey": "seller"
  },
  "service": {
    "serviceId": 1,
    "serviceName": "Full Scope Platform Management",
    "serviceCode": "FULL-SCOPE-001"
  },
  "inputBudget": 150000.00,
  "range": {
    "rangeId": 1,
    "minValue": 100000.00,
    "maxValue": 3000000.00,
    "rangeDisplay": "$100,000 - $3,000,000"
  },
  "pricing": {
    "percentage": 5.00,
    "fixedQuote": null,
    "calculationBase": 100000.00
  },
  "result": {
    "finalAmount": 5000.00,
    "currency": "USD",
    "formattedAmount": "$5,000.00"
  },
  "metadata": {
    "calculatedAt": "2024-01-15T10:30:00Z",
    "rangeFound": true
  }
}
```

**Ejemplo de Response (200 OK - Monto Fijo):**
```json
{
  "success": true,
  "calculationType": "fixed",
  "businessType": {
    "businessTypeId": 3,
    "businessTypeName": "TikTok Shop",
    "businessTypeKey": "tiktok_shop"
  },
  "platform": {
    "platformId": 5,
    "platformName": "TikTok Shop",
    "platformKey": "tiktok_shop"
  },
  "service": {
    "serviceId": 2,
    "serviceName": "Paid Advertising",
    "serviceCode": "PAID-ADV-001"
  },
  "inputBudget": 200000.00,
  "range": {
    "rangeId": 8,
    "minValue": 100000.00,
    "maxValue": 500000.00,
    "rangeDisplay": "$100,000 - $500,000"
  },
  "pricing": {
    "percentage": null,
    "fixedQuote": 4000.00,
    "calculationBase": 100000.00
  },
  "result": {
    "finalAmount": 4000.00,
    "currency": "USD",
    "formattedAmount": "$4,000.00"
  },
  "metadata": {
    "calculatedAt": "2024-01-15T10:30:00Z",
    "rangeFound": true
  }
}
```

**Ejemplo de Response (404 Not Found - Rango no encontrado):**
```json
{
  "success": false,
  "error": {
    "code": "RANGE_NOT_FOUND",
    "message": "No se encontró un rango de pricing activo para los parámetros proporcionados",
    "details": {
      "businessTypeId": 1,
      "platformId": 2,
      "serviceId": 1,
      "budgetAmount": 150000.00
    }
  }
}
```

**Ejemplo de Response (400 Bad Request - Validación):**
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Datos de entrada inválidos",
    "details": [
      "BudgetAmount debe ser mayor que 0"
    ]
  }
}
```

**Uso en Frontend:**
- Pantalla de calculadora de pricing
- Formulario con:
  - Dropdown: BusinessType (requerido)
  - Dropdown: Platform (opcional, depende del BusinessType seleccionado)
  - Dropdown: Service (requerido)
  - Input numérico: Budget Amount (requerido, > 0)
  - Botón: "Calcular"
- Resultado mostrado en card o sección destacada:
  - Tipo de cálculo (badge: "Porcentaje" o "Monto Fijo")
  - Información del servicio, business type y platform
  - Rango encontrado (ej: "$100,000 - $3,000,000")
  - Detalles del pricing:
    - Si es porcentaje: mostrar porcentaje y base de cálculo
    - Si es fijo: mostrar monto fijo
  - **Resultado final destacado**: Monto a cobrar en formato grande y claro
  - Fecha y hora del cálculo
- Si no se encuentra rango: mostrar mensaje de error claro con los parámetros usados
- Si hay error de validación: mostrar errores específicos por campo

---

## Flujos de Trabajo Recomendados

### Flujo 1: Crear un Nuevo Rango de Pricing

1. Usuario navega a "Mantenimiento de Pricing" > "Rangos por Porcentaje" (o "Rangos por Monto Fijo")
2. Click en botón "Crear Nuevo Rango"
3. Formulario se abre (modal o nueva página)
4. Usuario completa:
   - Selecciona Service (dropdown con búsqueda)
   - Selecciona BusinessType (dropdown)
   - Selecciona Platform (dropdown opcional, filtrado por BusinessType)
   - Ingresa MinBudgetValue
   - Ingresa MaxBudgetValue (opcional, dejar vacío para rango abierto)
   - Ingresa Percentage (o FixedQuote según el tipo)
   - Marca/desmarca IsActive
5. Click en "Guardar"
6. Si hay error (ej: solapamiento), mostrar mensaje de error
7. Si es exitoso, mostrar mensaje de éxito y redirigir a lista o cerrar modal

### Flujo 2: Usar la Calculadora

1. Usuario navega a "Calculadora de Pricing"
2. Usuario completa formulario:
   - Selecciona BusinessType
   - (Opcional) Selecciona Platform (se filtra según BusinessType)
   - Selecciona Service
   - Ingresa Budget Amount
3. Click en "Calcular"
4. Sistema muestra resultado:
   - Si encuentra rango: muestra información completa y monto calculado
   - Si no encuentra: muestra mensaje de error con sugerencias
5. Usuario puede:
   - Cambiar parámetros y recalcular
   - Copiar resultado
   - Exportar cálculo (opcional)

### Flujo 3: Editar un Rango Existente

1. Usuario está en lista de rangos
2. Click en botón "Editar" de un rango
3. Formulario se abre pre-cargado con datos actuales
4. Usuario modifica campos necesarios
5. Click en "Guardar Cambios"
6. Sistema valida (incluyendo solapamiento con otros rangos, excluyendo el actual)
7. Si es exitoso, actualiza y muestra mensaje de éxito

### Flujo 4: Desactivar un Rango

1. Usuario está en lista o detalles de un rango
2. Click en botón "Desactivar"
3. Modal de confirmación: "¿Está seguro de desactivar este rango?"
4. Si confirma:
   - Se envía DELETE al endpoint
   - Rango se desactiva (IsActive = false)
   - Rango desaparece de lista si se filtra por activos
5. Si cancela, no hace nada

---

## Consideraciones Importantes para el Frontend

### 1. Rangos Abiertos

Cuando `maxBudgetValue` o `maxAdBudgetValue` es `null`, significa rango abierto (ej: "$50,000,000+").

**En el formulario:**
- Campo MaxBudgetValue debe ser opcional
- Si se deja vacío, se envía como `null`
- Mostrar placeholder: "Dejar vacío para rango abierto (ej: $50M+)"

**En la visualización:**
- Si MaxValue es null, mostrar como: "$100,000+"
- Si tiene valor, mostrar como: "$100,000 - $3,000,000"

### 2. PlatformId NULL

Cuando `platformId` es `null`, significa que el rango aplica a todas las plataformas del BusinessType.

**En el formulario:**
- Dropdown de Platform debe tener opción "Todas las plataformas" o "General"
- Esta opción envía `null` al backend

**En la visualización:**
- Si PlatformId es null, mostrar: "Todas las plataformas" o "General"

### 3. Validación de Solapamiento

El backend valida que no haya solapamiento de rangos. Si el usuario intenta crear/editar un rango que se solapa, recibirá error 400.

**Manejo en Frontend:**
- Mostrar mensaje de error claro: "Ya existe un rango que se solapa con los valores proporcionados"
- Sugerir revisar rangos existentes para el mismo ServiceId, BusinessTypeId y PlatformId
- Opcional: Mostrar rangos que se solapan para ayudar al usuario

### 4. Cálculo de Porcentaje

**IMPORTANTE:** El porcentaje se aplica sobre el `MinBudgetValue` del rango, NO sobre el `budgetAmount` ingresado.

**Ejemplo:**
- Usuario ingresa: $150,000
- Rango encontrado: $100,000 - $3,000,000 con 5%
- Cálculo: $100,000 * 0.05 = $5,000 (NO $150,000 * 0.05)

**En la UI de la calculadora:**
- Mostrar claramente: "Cálculo basado en el valor mínimo del rango"
- Mostrar: "Base de cálculo: $100,000"
- Mostrar: "Porcentaje: 5%"
- Mostrar: "Resultado: $5,000"

### 5. Prioridad de Búsqueda en Calculadora

El sistema busca primero rangos con PlatformId específico, luego rangos con PlatformId NULL.

**En la UI:**
- Si el usuario selecciona una Platform específica, el sistema buscará primero en rangos de esa platform
- Si no encuentra, buscará en rangos generales (PlatformId NULL)
- Mostrar en el resultado qué tipo de rango se usó (específico o general)

### 6. Filtros en Listados

Los endpoints GET soportan múltiples filtros opcionales que se pueden combinar.

**Recomendación de UI:**
- Barra de filtros en la parte superior de la tabla
- Filtros: Service (dropdown), BusinessType (dropdown), Platform (dropdown), Estado (toggle o dropdown)
- Botón "Limpiar Filtros"
- Los filtros se aplican al hacer la petición GET

### 7. Formato de Montos

Todos los montos deben mostrarse con formato de moneda:
- Formato: `$X,XXX.XX`
- Ejemplos: `$100,000.00`, `$5,000.00`, `$1,500,000.00`

### 8. Estados de Rangos

Los rangos tienen un campo `isActive`:
- `true`: Rango activo (se usa en cálculos)
- `false`: Rango desactivado (no se usa en cálculos, pero se mantiene en BD)

**En la UI:**
- Mostrar badge o indicador visual del estado
- Filtro por estado activo/inactivo
- Solo rangos activos se usan en la calculadora

---

## Ejemplos de Pantallas Sugeridas

### Pantalla 1: Listado de Rangos por Porcentaje

```
┌─────────────────────────────────────────────────────────┐
│ Mantenimiento de Pricing - Rangos por Porcentaje       │
├─────────────────────────────────────────────────────────┤
│ Filtros:                                                 │
│ [Service ▼] [BusinessType ▼] [Platform ▼] [Activos ▼] │
│ [Limpiar]                                                │
├─────────────────────────────────────────────────────────┤
│ [+ Crear Nuevo Rango]                                    │
├─────────────────────────────────────────────────────────┤
│ Servicio          │ Business │ Platform │ Rango         │
│                   │ Type     │          │               │
├───────────────────┼──────────┼──────────┼───────────────┤
│ Full Scope        │ Amazon   │ Seller   │ $100K-$3M     │
│ Platform          │          │          │ 5%            │
│                   │          │          │ [✓ Activo]    │
│                   │          │          │ [Editar] [X]  │
├───────────────────┼──────────┼──────────┼───────────────┤
│ Full Scope        │ Amazon   │ General  │ $3M+          │
│ Platform          │          │          │ 4%            │
│                   │          │          │ [✓ Activo]    │
│                   │          │          │ [Editar] [X]  │
└─────────────────────────────────────────────────────────┘
```

### Pantalla 2: Calculadora de Pricing

```
┌─────────────────────────────────────────────────────────┐
│ Calculadora de Pricing                                  │
├─────────────────────────────────────────────────────────┤
│ Business Type: [Amazon ▼]                              │
│ Platform:      [Seller ▼] (Opcional)                   │
│ Service:       [Full Scope Platform Management ▼]       │
│ Budget Amount: [$150,000.00]                            │
│                                                         │
│ [Calcular]                                              │
├─────────────────────────────────────────────────────────┤
│ Resultado:                                              │
│                                                         │
│ Tipo de Cálculo: [Porcentaje]                          │
│                                                         │
│ Servicio: Full Scope Platform Management                │
│ Business Type: Amazon                                   │
│ Platform: Seller                                        │
│                                                         │
│ Rango Encontrado: $100,000 - $3,000,000                │
│                                                         │
│ Detalles:                                               │
│ - Base de Cálculo: $100,000.00                          │
│ - Porcentaje: 5%                                        │
│                                                         │
│ ┌─────────────────────────────────────────────────┐   │
│ │ MONTO A COBRAR                                  │   │
│ │ $5,000.00                                       │   │
│ └─────────────────────────────────────────────────┘   │
│                                                         │
│ Calculado el: 15/01/2024 10:30:00                     │
└─────────────────────────────────────────────────────────┘
```

### Pantalla 3: Formulario de Crear/Editar Rango

```
┌─────────────────────────────────────────────────────────┐
│ Crear Nuevo Rango de Pricing                            │
├─────────────────────────────────────────────────────────┤
│ Service: *        [Full Scope Platform Management ▼]    │
│ Business Type: *  [Amazon ▼]                            │
│ Platform:         [Seller ▼] [Todas las plataformas]   │
│                                                         │
│ Min Budget Value: * [$100,000.00]                       │
│ Max Budget Value:  [$3,000,000.00] (Opcional)          │
│                      Dejar vacío para rango abierto     │
│                                                         │
│ Percentage: *     [5.00] % (0-100)                     │
│                                                         │
│ [✓] Activo                                              │
│                                                         │
│ [Cancelar] [Guardar]                                    │
└─────────────────────────────────────────────────────────┘
```

---

## Manejo de Errores

### Errores Comunes y Cómo Mostrarlos

1. **400 Bad Request - Validación**
   - Mostrar mensajes de error específicos por campo
   - Resaltar campos con error en rojo
   - Mostrar tooltip o mensaje debajo del campo

2. **400 Bad Request - Solapamiento**
   - Mostrar mensaje: "Ya existe un rango que se solapa con los valores proporcionados"
   - Sugerir revisar rangos existentes
   - Opcional: Mostrar lista de rangos que se solapan

3. **404 Not Found - Rango no encontrado (en calculadora)**
   - Mostrar mensaje: "No se encontró un rango de pricing para los parámetros proporcionados"
   - Mostrar los parámetros usados
   - Sugerir: "Verifique que existan rangos activos para esta combinación de Service, BusinessType y Platform"

4. **401 Unauthorized**
   - Redirigir a login
   - Mostrar mensaje: "Su sesión ha expirado"

5. **500 Internal Server Error**
   - Mostrar mensaje genérico: "Error del servidor. Por favor intente más tarde"
   - Opcional: Botón para reportar error

---

## Recursos Adicionales Necesarios

Para implementar el frontend, necesitarás también endpoints para obtener las listas de referencia:

### Endpoints de Referencia (Asumidos - verificar si existen)

1. **GET /api/corporate/services** - Lista de servicios para dropdowns
2. **GET /api/corporate/business-types** - Lista de business types para dropdowns
3. **GET /api/corporate/platforms?businessTypeId={id}** - Lista de platforms filtradas por business type

Si estos endpoints no existen, necesitarás crearlos o usar los endpoints existentes de mantenimiento de estas entidades.

---

## Resumen para el Frontend

**Lo que el usuario necesita hacer:**

1. **Mantenimiento de Pricing:**
   - Ver, crear, editar y desactivar rangos de pricing
   - Filtrar rangos por múltiples criterios
   - Gestionar dos tipos de rangos: por porcentaje y por monto fijo

2. **Usar la Calculadora:**
   - Ingresar parámetros del cliente (BusinessType, Platform, Service, Budget)
   - Obtener cálculo automático del monto a cobrar
   - Ver información detallada del cálculo

**Recursos disponibles:**

- 10 endpoints de mantenimiento (5 para porcentaje, 5 para monto fijo)
- 1 endpoint de calculadora que busca automáticamente en ambos tipos de rangos
- Validaciones completas en el backend
- Manejo de errores estructurado
- Soporte para rangos abiertos y PlatformId NULL

**Consideraciones técnicas:**

- Todos los endpoints requieren JWT
- Validación de solapamiento de rangos
- Cálculo de porcentaje sobre MinBudgetValue, no sobre budgetAmount
- Prioridad de búsqueda: PlatformId específico primero, luego NULL
- Formato de moneda para todos los montos
- Estados activo/inactivo para rangos

---

**Última actualización:** Diciembre 2024  
**Versión del Backend:** .NET 8.0

