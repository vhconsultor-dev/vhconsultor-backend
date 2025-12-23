# Configuración de Azure Storage para Adjuntos de Facturas

## ⚠️ IMPORTANTE: Configuración de Variables de Entorno

La aplicación requiere la configuración de Azure Storage para funcionar correctamente. La **ConnectionString** debe configurarse como **variable de entorno** en Azure App Service.

---

## 🔧 Variables de Entorno para Azure App Service

### Pasos para Configurar:

1. Ir a **Azure Portal** → https://portal.azure.com
2. Navegar a **App Services** → Buscar `vh-backend-app`
3. En el menú lateral, ir a **Configuration** → **Application settings**
4. Click en **+ New application setting**

### Variable a Crear:

| **Nombre de la Variable** | **Valor** |
|---------------------------|-----------|
| `AzureStorage__ConnectionString` | `DefaultEndpointsProtocol=https;AccountName=vhstorageblob;AccountKey=[TU_ACCOUNT_KEY_AQUI];EndpointSuffix=core.windows.net` |

**Nota:** Reemplazar `[TU_ACCOUNT_KEY_AQUI]` con el Account Key real de tu Azure Storage Account `vhstorageblob`. Puedes obtenerlo en:
- Azure Portal → Storage Accounts → `vhstorageblob` → **Access keys** → **key1**

### ⚠️ Nota Importante:
- El nombre de la variable usa **doble guion bajo** (`__`) que se mapea a la estructura JSON `AzureStorage:ConnectionString`
- Después de agregar la variable, hacer click en **Save** (la aplicación se reiniciará automáticamente)

---

## 📋 Para Desarrollo Local (Opcional)

Si quieres probar localmente, puedes configurar la variable de entorno en tu sistema:

**Windows (PowerShell):**
```powershell
$env:AzureStorage__ConnectionString = "DefaultEndpointsProtocol=https;AccountName=vhstorageblob;AccountKey=[TU_ACCOUNT_KEY];EndpointSuffix=core.windows.net"
```

**Linux/Mac:**
```bash
export AzureStorage__ConnectionString="DefaultEndpointsProtocol=https;AccountName=vhstorageblob;AccountKey=[TU_ACCOUNT_KEY];EndpointSuffix=core.windows.net"
```

---

## 🔍 Cómo Funciona

ASP.NET Core lee las variables de entorno con el formato:
- `AzureStorage__ConnectionString` (doble guion bajo `__`) se mapea a `AzureStorage:ConnectionString` en la configuración

El orden de precedencia es:
1. **Variables de entorno** (más alta prioridad) ✅
2. `appsettings.Production.json`
3. `appsettings.json` (menor prioridad)

---

## ✅ Verificación

Para verificar que la configuración está correcta:

1. Después de guardar la variable, esperar a que la aplicación se reinicie
2. Revisar los logs de la aplicación en Azure Portal:
   - **App Service** → `vh-backend-app` → **Log stream** o **Logs**
3. Si hay un error de conexión a Azure Storage, aparecerá en los logs
4. Probar subiendo un adjunto a una factura para confirmar que funciona

---

## 🔒 Seguridad

- ✅ La ConnectionString **NO** está en el repositorio de código
- ✅ Se configura directamente en Azure App Service
- ✅ Solo el equipo con acceso a Azure puede ver/modificar la configuración
- ✅ Las variables de entorno son encriptadas en Azure

---

## 📝 Notas Adicionales

- El archivo `appsettings.json` y `appsettings.Production.json` tienen la ConnectionString vacía
- En producción, Azure App Service **DEBE** usar la variable de entorno
- Si la variable no está configurada, la aplicación fallará al intentar subir adjuntos
- La estructura de carpetas en Azure Blob Storage será: `InvoicesAttachment/{InvoiceNumber}/`

