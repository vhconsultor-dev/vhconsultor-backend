# Amazon SP-API - Guía Rápida de Inicio

## 🚀 Configuración Inicial

### 1. Ejecutar el Script de Base de Datos

```sql
-- Ejecutar desde SQL Server Management Studio o Azure Data Studio
Scripts/CreateAmazonTokensTable.sql
```

### 2. Verificar la Configuración en appsettings.json

Ya está configurado con tus credenciales:

```json
{
  "Amazon": {
    "ClientId": "YOUR_AMAZON_CLIENT_ID",
    "ClientSecret": "YOUR_AMAZON_CLIENT_SECRET",
    "RefreshToken": "YOUR_AMAZON_REFRESH_TOKEN"
  }
}
```

### 3. Compilar y Ejecutar el Proyecto

```bash
cd ApiLayer
dotnet build
dotnet run
```

---

## 📝 Uso Básico

### Generar un Access Token (Primera Vez)

**Endpoint:** `POST /api/amazon/amazonauth/generate-token`

**Postman/Insomnia:**

```json
POST https://localhost:5001/api/amazon/amazonauth/generate-token
Content-Type: application/json

{
  "refreshToken": "YOUR_AMAZON_REFRESH_TOKEN",
  "clientId": "YOUR_AMAZON_CLIENT_ID",
  "clientSecret": "YOUR_AMAZON_CLIENT_SECRET"
}
```

**cURL:**

```bash
curl -X POST https://localhost:5001/api/amazon/amazonauth/generate-token \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "YOUR_AMAZON_REFRESH_TOKEN",
    "clientId": "YOUR_AMAZON_CLIENT_ID",
    "clientSecret": "YOUR_AMAZON_CLIENT_SECRET"
  }'
```

**Respuesta Esperada:**

```json
{
  "success": true,
  "message": "Access token generado exitosamente",
  "data": {
    "tokenId": 1,
    "accessToken": "Atza|IwEBIJ7JdF7_dG_yKsRv6t4eRv...",
    "refreshToken": "Atzr|IwEBIFQVzXIt...",
    "tokenType": "bearer",
    "expiresIn": 3600,
    "expiresAt": "2026-01-17T15:30:00"
  }
}
```

---

## ⏱️ Información Importante sobre Tokens

- **Duración del Access Token:** 1 hora (3600 segundos)
- **Duración del Refresh Token:** No expira (permanente)
- **Renovación:** Puedes generar un nuevo access token cuando lo necesites

---

## 🔄 Flujo Recomendado

### Opción 1: Generar Token Cada Vez que lo Necesites

```javascript
// Pseudo-código
async function getAmazonAccessToken() {
  const response = await fetch('/api/amazon/amazonauth/generate-token', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      refreshToken: 'Atzr|...',
      clientId: 'amzn1.application-oa2-client...',
      clientSecret: 'amzn1.oa2-cs...'
    })
  });
  
  const data = await response.json();
  return data.data.accessToken;
}

// Usar el token
const accessToken = await getAmazonAccessToken();
// Ahora puedes usar accessToken para llamar a Amazon SP-API
```

### Opción 2: Obtener Token Activo Existente

```javascript
// Primero verifica si hay un token activo
async function getActiveToken() {
  const response = await fetch('/api/amazon/amazonauth/active-token', {
    headers: {
      'Authorization': 'Bearer YOUR_JWT_TOKEN'
    }
  });
  
  if (response.ok) {
    const data = await response.json();
    return data.data.accessToken;
  }
  
  // Si no hay token activo, genera uno nuevo
  return await getAmazonAccessToken();
}
```

---

## 🧪 Pruebas en Swagger

1. Navega a: `https://localhost:5001/swagger`
2. Busca la sección **Amazon**
3. Expande `POST /api/amazon/amazonauth/generate-token`
4. Click en **Try it out**
5. Pega el JSON con tus credenciales
6. Click en **Execute**

---

## 📊 Endpoints Disponibles

| Método | Endpoint | Descripción | Auth |
|--------|----------|-------------|------|
| POST | `/api/amazon/amazonauth/generate-token` | Genera nuevo access token | No |
| GET | `/api/amazon/amazonauth/active-token` | Obtiene token activo | Sí |
| GET | `/api/amazon/amazonauth/tokens` | Lista todos los tokens | Sí |
| POST | `/api/amazon/amazonauth/deactivate-expired` | Desactiva tokens expirados | Sí |

---

## 🎯 Próximos Pasos

Una vez que tengas el access token funcionando, puedes:

1. **Implementar APIs de Seller:**
   - Órdenes
   - Productos
   - Inventario
   - Reportes

2. **Implementar APIs de Vendor:**
   - Purchase Orders
   - Shipping
   - Invoices

3. **Automatización:**
   - Job para renovar tokens automáticamente
   - Sincronización de datos
   - Webhooks de Amazon

---

## ❓ Preguntas Frecuentes

### ¿Cada cuánto debo generar un nuevo token?

Los tokens duran 1 hora. Puedes:
- Generar uno nuevo cada vez que lo necesites (recomendado para empezar)
- Implementar un sistema de caché que renueve el token automáticamente

### ¿Qué pasa si mi refresh token expira?

El refresh token de Amazon no expira. Si dejas de funcionar, debes:
1. Ir a Amazon Seller Central
2. Generar un nuevo refresh token
3. Actualizar el `appsettings.json`

### ¿Puedo usar el mismo token para Seller y Vendor?

Sí, el mismo access token funciona para ambas APIs, siempre que tu cuenta tenga los permisos necesarios.

---

## 📚 Documentación Completa

Para más detalles, consulta: `AMAZON_API_DOCUMENTATION.md`

---

## 🐛 Problemas Comunes

### Error: "invalid_grant"
**Solución:** Verifica que el refresh token sea correcto y no haya espacios extra.

### Error: "invalid_client"
**Solución:** Verifica que el clientId y clientSecret sean correctos.

### Error: "No hay tokens activos disponibles"
**Solución:** Genera un nuevo token con el endpoint POST.

---

## 💡 Tips

1. **Guarda el Access Token:** Una vez generado, úsalo durante la hora que dura
2. **No expongas credenciales:** En producción, usa Azure Key Vault
3. **Monitorea expiración:** Implementa un sistema que detecte cuando el token está por expirar
4. **Logs:** Revisa los logs de la aplicación para ver detalles de errores

---

¡Listo! Ya tienes todo configurado para trabajar con Amazon SP-API 🎉
