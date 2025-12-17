# 🔄 Cambios Importantes en API de Contratos - Frontend

## 📋 Resumen de Cambios

Se realizaron cambios **BREAKING CHANGES** en el módulo de contratos debido a modificaciones en la base de datos. **Es necesario actualizar el frontend** para que funcione correctamente con la nueva versión del backend.

---

## 🔴 CAMBIOS CRÍTICOS

### 1. `ContractId` ahora es **numérico** (INT)

**ANTES:**
```typescript
contractId: string  // "CON-2025-001"
```

**AHORA:**
```typescript
contractId: number  // 1, 2, 3...
```

### 2. `ContractNumber` ahora se genera automáticamente

**ANTES:**
```typescript
// El frontend generaba y enviaba el ContractNumber
{
  "contractId": "CON-2025-001",
  "contractNumber": "2025-001",
  "customerId": 7,
  // ...
}
```

**AHORA:**
```typescript
// El backend genera automáticamente contractId y contractNumber
{
  "customerId": 7,
  // NO se envía contractId
  // NO se envía contractNumber
  // ...
}
```

### 3. Nuevo formato de `ContractNumber`

**ANTES:** `2025-001`, `2025-002`... (basado en año)

**AHORA:** `CTR-000001`, `CTR-000002`... (consecutivo global)

---

## 📝 Cambios por Endpoint

### ✅ POST - Crear Contrato

**URL:** `POST /api/corporate/Contract`

#### Cambios en el Request Body:

| Campo | ANTES | AHORA | Estado |
|-------|-------|-------|--------|
| `contractId` | ❌ Requerido (string) | ✅ **NO enviar** (autogenerado) | **ELIMINAR** |
| `contractNumber` | ❌ Requerido (string) | ✅ **NO enviar** (autogenerado) | **ELIMINAR** |
| `customerId` | ✅ Requerido (number) | ✅ Requerido (number) | Sin cambios |

#### Request Body ANTES:
```json
{
  "contractId": "CON-2025-001",
  "contractNumber": "2025-001",
  "customerId": 7,
  "clientLegalName": "XYZ Company",
  "clientTaxId": "3-101-123456",
  // ... otros campos
}
```

#### Request Body AHORA:
```json
{
  "customerId": 7,
  "clientLegalName": "XYZ Company",
  "clientTaxId": "3-101-123456",
  // ... otros campos
  // ⚠️ NO enviar contractId
  // ⚠️ NO enviar contractNumber
}
```

#### Cambios en la Response:

**ANTES:**
```json
{
  "status": true,
  "statusCode": 200,
  "data": "CON-2025-001",  // ← STRING
  "message": "Contrato creado exitosamente",
  "timestamp": "2025-12-09T12:00:00"
}
```

**AHORA:**
```json
{
  "status": true,
  "statusCode": 200,
  "data": 1,  // ← NUMBER (ContractId generado)
  "message": "Contrato creado exitosamente",
  "timestamp": "2025-12-09T12:00:00"
}
```

#### Ejemplo TypeScript/JavaScript:

**ANTES:**
```typescript
const createContract = async (data: ContractRequest) => {
  const response = await axios.post('/api/corporate/Contract', {
    contractId: generateContractId(),      // ❌ ELIMINAR
    contractNumber: generateContractNumber(), // ❌ ELIMINAR
    customerId: data.customerId,
    // ... otros campos
  });
  
  const contractId: string = response.data.data; // string
  return contractId;
};
```

**AHORA:**
```typescript
const createContract = async (data: ContractRequest) => {
  const response = await axios.post('/api/corporate/Contract', {
    // ✅ NO enviar contractId
    // ✅ NO enviar contractNumber
    customerId: data.customerId,
    // ... otros campos
  });
  
  const contractId: number = response.data.data; // number
  return contractId;
};
```

---

### ✅ PUT - Actualizar Contrato

**URL:** `PUT /api/corporate/Contract/{contractId}`

#### Cambios en la URL:

| ANTES | AHORA |
|-------|-------|
| `PUT /api/corporate/Contract/CON-2025-001` | `PUT /api/corporate/Contract/1` |

#### Ejemplo:

**ANTES:**
```typescript
const updateContract = async (contractId: string, data: UpdateContractRequest) => {
  await axios.put(`/api/corporate/Contract/${contractId}`, data);
};

// Llamada:
updateContract("CON-2025-001", updateData);
```

**AHORA:**
```typescript
const updateContract = async (contractId: number, data: UpdateContractRequest) => {
  await axios.put(`/api/corporate/Contract/${contractId}`, data);
};

// Llamada:
updateContract(1, updateData);
```

---

### ✅ DELETE - Eliminar Contrato

**URL:** `DELETE /api/corporate/Contract/{contractId}`

#### Cambios en la URL:

| ANTES | AHORA |
|-------|-------|
| `DELETE /api/corporate/Contract/CON-2025-001` | `DELETE /api/corporate/Contract/1` |

#### Ejemplo:

**ANTES:**
```typescript
const deleteContract = async (contractId: string) => {
  await axios.delete(`/api/corporate/Contract/${contractId}`);
};

deleteContract("CON-2025-001");
```

**AHORA:**
```typescript
const deleteContract = async (contractId: number) => {
  await axios.delete(`/api/corporate/Contract/${contractId}`);
};

deleteContract(1);
```

---

### ✅ GET - Obtener Contratos

**URL:** `GET /api/corporate/Contract`

#### Cambios en Query Parameters:

| Parámetro | ANTES | AHORA |
|-----------|-------|-------|
| `contractId` | `string` | `number` |
| `contractNumber` | `string` | `string` (sin cambios, pero nuevo formato) |

#### Ejemplos:

**ANTES:**
```typescript
// Buscar por ID
GET /api/corporate/Contract?contractId=CON-2025-001

// Buscar por número
GET /api/corporate/Contract?contractNumber=2025-001
```

**AHORA:**
```typescript
// Buscar por ID (ahora numérico)
GET /api/corporate/Contract?contractId=1

// Buscar por número (nuevo formato)
GET /api/corporate/Contract?contractNumber=CTR-000001
```

#### Cambios en Response:

**ANTES:**
```json
{
  "status": true,
  "data": {
    "contractId": "CON-2025-001",
    "contractNumber": "2025-001",
    "customerId": 7,
    // ...
  }
}
```

**AHORA:**
```json
{
  "status": true,
  "data": {
    "contractId": 1,
    "contractNumber": "CTR-000001",
    "customerId": 7,
    // ...
  }
}
```

---

## 🔧 Actualización de Interfaces TypeScript

### Actualizar todas las interfaces de contratos:

**ANTES:**
```typescript
interface Contract {
  contractId: string;        // ❌
  contractNumber: string;    // Formato: "2025-001"
  customerId: number;
  clientLegalName?: string;
  // ... otros campos
}

interface CreateContractRequest {
  contractId: string;        // ❌
  contractNumber: string;    // ❌
  customerId: number;
  // ... otros campos
}
```

**AHORA:**
```typescript
interface Contract {
  contractId: number;        // ✅ Cambio: string → number
  contractNumber: string;    // Formato: "CTR-000001"
  customerId: number;
  clientLegalName?: string;
  // ... otros campos
}

interface CreateContractRequest {
  // ✅ contractId y contractNumber NO se incluyen
  customerId: number;
  // ... otros campos
}
```

---

## 📦 Cambios en `ContractService` (servicios relacionados)

Si usas `ContractService` (servicios de un contrato), también hay cambios:

### Interfaces de ContractService:

**ANTES:**
```typescript
interface ContractService {
  contractServiceId: number;
  contractId: string;        // ❌
  serviceId?: number;
  // ...
}

interface CreateContractServiceRequest {
  contractId: string;        // ❌
  serviceId?: number;
  // ...
}
```

**AHORA:**
```typescript
interface ContractService {
  contractServiceId: number;
  contractId: number;        // ✅ Cambio: string → number
  serviceId?: number;
  // ...
}

interface CreateContractServiceRequest {
  contractId: number;        // ✅ Cambio: string → number
  serviceId?: number;
  // ...
}
```

---

## ✅ Checklist de Actualización Frontend

### Interfaces TypeScript
- [ ] Cambiar `contractId: string` a `contractId: number` en todas las interfaces
- [ ] Eliminar `contractId` de `CreateContractRequest`
- [ ] Eliminar `contractNumber` de `CreateContractRequest`
- [ ] Actualizar formato esperado de `contractNumber` de `"YYYY-###"` a `"CTR-######"`

### Servicios/API Calls
- [ ] **Crear Contrato**: Eliminar generación de `contractId` y `contractNumber`
- [ ] **Crear Contrato**: Cambiar tipo de response de `string` a `number`
- [ ] **Actualizar Contrato**: Cambiar parámetro `contractId` de `string` a `number`
- [ ] **Eliminar Contrato**: Cambiar parámetro `contractId` de `string` a `number`
- [ ] **Obtener Contrato**: Cambiar query param `contractId` de `string` a `number`

### Componentes/Vistas
- [ ] Actualizar componentes que muestran `contractId` (ahora es numérico)
- [ ] Actualizar componentes que muestran `contractNumber` (nuevo formato)
- [ ] Eliminar lógica de generación de `contractId`
- [ ] Eliminar lógica de generación de `contractNumber`
- [ ] Actualizar validaciones de formularios
- [ ] Actualizar filtros y búsquedas

### Rutas/Navegación
- [ ] Actualizar rutas que usan `contractId` como parámetro (ahora numérico)
- [ ] Ejemplo: `/contracts/CON-2025-001` → `/contracts/1`

### Estado/Store (Redux, Zustand, etc.)
- [ ] Actualizar tipos en store de contratos
- [ ] Actualizar reducers/acciones que manejan contratos

---

## 🧪 Testing

### Casos de prueba recomendados:

1. **Crear contrato sin `contractId` ni `contractNumber`**
   - ✅ Debe funcionar correctamente
   - ✅ Debe retornar un `contractId` numérico

2. **Verificar formato de `contractNumber` generado**
   - ✅ Primer contrato: `CTR-000001`
   - ✅ Segundo contrato: `CTR-000002`

3. **Actualizar/Eliminar usando `contractId` numérico**
   - ✅ URLs deben usar números: `/api/corporate/Contract/1`

4. **Búsquedas por `contractId` y `contractNumber`**
   - ✅ `contractId=1` debe funcionar
   - ✅ `contractNumber=CTR-000001` debe funcionar

---

## 🆘 Preguntas Frecuentes

### ¿Qué pasa con los contratos existentes?

Los contratos antiguos fueron eliminados de la base de datos debido a cambios estructurales. Si necesitas migrar datos, contacta al equipo de backend.

### ¿Puedo seguir enviando `contractId` en el POST?

**NO**. El backend lo ignorará o dará error. Ya no se debe enviar.

### ¿Cómo obtengo el `contractNumber` después de crear?

Después de crear, haz un GET con el `contractId` retornado:

```typescript
// 1. Crear contrato
const response = await createContract(data);
const contractId = response.data.data; // 1

// 2. Obtener contrato completo (incluye contractNumber)
const contract = await getContract(contractId);
console.log(contract.contractNumber); // "CTR-000001"
```

### ¿El formato `CTR-000001` es definitivo?

Sí, es el nuevo formato estándar y profesional para números de contrato.

### ¿Necesito cambiar la base de datos del frontend (localStorage, IndexedDB)?

Sí, si almacenas contratos localmente, debes limpiar los datos antiguos o migrarlos al nuevo formato.

---

## 📞 Soporte

Si tienes dudas o problemas con la migración, contacta al equipo de backend.

---

**Fecha de actualización:** Diciembre 9, 2025  
**Versión del backend:** 716d003

