# Configuración de Azure Storage para Adjuntos de Facturas

## ⚠️ IMPORTANTE: Configuración de Variables de Entorno

La aplicación requiere la configuración de Azure Storage para funcionar correctamente. La **ConnectionString** debe configurarse como **variable de entorno** tanto en desarrollo local como en producción.

## 📋 Pasos para Configurar

### Para Desarrollo Local

Configurar la variable de entorno en tu sistema:

**Windows (PowerShell):**
```powershell
$env:AzureStorage__ConnectionString = "DefaultEndpointsProtocol=https;AccountName=vhstorageblob;AccountKey=<TU_KEY>;EndpointSuffix=core.windows.net"
```

**Linux/Mac:**
```bash
export AzureStorage__ConnectionString="DefaultEndpointsProtocol=https;AccountName=vhstorageblob;AccountKey=<TU_KEY>;EndpointSuffix=core.windows.net"
```

**O usar un archivo `.env` o `launchSettings.json`**

### Para Azure App Service (Producción)

1. Ir a **Azure Portal**
2. Navegar a **Azure App Service** → `vh-backend-app`
3. Ir a **Configuration** → **Application settings**
4. Agregar nueva variable:

**Nombre:**
```
AzureStorage__ConnectionString
```

**Valor:**
```
DefaultEndpointsProtocol=https;AccountName=vhstorageblob;AccountKey=<TU_KEY>;EndpointSuffix=core.windows.net
```

5. Click en **Save** (la app se reiniciará automáticamente)

## 🔍 Cómo Funciona

ASP.NET Core lee las variables de entorno con el formato:
- `AzureStorage__ConnectionString` (doble guion bajo `__`) se mapea a `AzureStorage:ConnectionString` en la configuración

El orden de precedencia es:
1. **Variables de entorno** (más alta prioridad)
2. `appsettings.Production.json`
3. `appsettings.json` (menor prioridad)

## ✅ Verificación

Para verificar que la configuración está correcta, revisar los logs de la aplicación en Azure Portal. Si hay un error de conexión a Azure Storage, aparecerá en los logs.

## 🔒 Seguridad

- ✅ La ConnectionString **NO** está en el repositorio
- ✅ Se configura directamente en Azure App Service
- ✅ Solo el equipo con acceso a Azure puede ver/modificar la configuración

## 📝 Notas

- El archivo `appsettings.Development.json` mantiene la ConnectionString para desarrollo local
- El archivo `appsettings.Production.json` tiene un placeholder vacío
- En producción, Azure App Service usará la variable de entorno

