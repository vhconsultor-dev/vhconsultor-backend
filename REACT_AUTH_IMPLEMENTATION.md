# 🔐 Implementación de Autenticación JWT para React - VHConsultor API

## 📋 Descripción General

Este documento describe cómo implementar la autenticación JWT para conectarse con la API VHConsultor. El sistema utiliza encriptación AES-256 y validación de timestamp en UTC-6 (Costa Rica).

## 🌐 Configuración de la API

### Endpoint Principal
```
POST https://vh-apimanagement.azure-api.net/shared-vh/api/Security/generate-token
```

### Configuración Requerida
```javascript
const API_CONFIG = {
  baseUrl: 'https://vh-apimanagement.azure-api.net/shared-vh/api',
  secretKey: 'VHC0n5ult0r!2024$S3cur3K3y#F0rJWT@T0k3nG3n3r4t10n&V4l1d4t10n',
  validationKey: 'AES256V4l1d4t10nK3y!2024@VHC0n5ult0r#Encryp7&D3cryp7$S3cur3',
  endpoint: '/Security/generate-token'
};
```

## 🔄 Flujo de Autenticación

1. **Crear Payload**: `SecretKey|Timestamp`
2. **Encriptar**: AES-256-CBC con ValidationKey
3. **Enviar**: POST al endpoint con payload encriptado
4. **Recibir**: JWT Token válido por 60 minutos
5. **Usar**: Token en headers de peticiones subsecuentes

## 📦 Instalación de Dependencias

```bash
npm install crypto-js
npm install @types/crypto-js  # Si usas TypeScript
```

## 🛠️ Implementación Completa

### 1. Archivo: `authService.js`

```javascript
import CryptoJS from 'crypto-js';

// Configuración de la API
const API_CONFIG = {
  baseUrl: 'https://vh-apimanagement.azure-api.net/shared-vh/api',
  secretKey: 'VHC0n5ult0r!2024$S3cur3K3y#F0rJWT@T0k3nG3n3r4t10n&V4l1d4t10n',
  validationKey: 'AES256V4l1d4t10nK3y!2024@VHC0n5ult0r#Encryp7&D3cryp7$S3cur3',
  endpoint: '/Security/generate-token'
};

// Claves de almacenamiento
const STORAGE_KEYS = {
  TOKEN: 'vhconsultor_jwt_token',
  EXPIRES_AT: 'vhconsultor_token_expires_at'
};

/**
 * Genera un timestamp en UTC-6 (Costa Rica)
 * @returns {number} Unix timestamp en milisegundos
 */
export function generateTimestamp() {
  const now = new Date();
  // UTC-6 = UTC - 6 horas
  const utcMinus6 = new Date(now.getTime() - (6 * 60 * 60 * 1000));
  return utcMinus6.getTime();
}

/**
 * Crea el payload original con SecretKey y Timestamp
 * @param {string} secretKey - Clave secreta
 * @param {number} timestamp - Timestamp en milisegundos
 * @returns {string} Payload en formato "SecretKey|Timestamp"
 */
export function createPayload(secretKey, timestamp) {
  return `${secretKey}|${timestamp}`;
}

/**
 * Deriva una clave usando SHA256
 * @param {string} password - Contraseña base
 * @param {number} keyLength - Longitud deseada de la clave
 * @returns {Uint8Array} Clave derivada
 */
export function deriveKey(password, keyLength = 32) {
  const hash = CryptoJS.SHA256(password);
  const hashBytes = CryptoJS.enc.Hex.parse(hash.toString());
  
  if (hashBytes.sigBytes === keyLength) {
    return hashBytes;
  }
  
  // Si no coincide exactamente, truncar o extender
  const key = new Uint8Array(keyLength);
  const hashArray = new Uint8Array(hashBytes.words.length * 4);
  
  for (let i = 0; i < hashBytes.words.length; i++) {
    const word = hashBytes.words[i];
    hashArray[i * 4] = (word >>> 24) & 0xff;
    hashArray[i * 4 + 1] = (word >>> 16) & 0xff;
    hashArray[i * 4 + 2] = (word >>> 8) & 0xff;
    hashArray[i * 4 + 3] = word & 0xff;
  }
  
  for (let i = 0; i < keyLength; i++) {
    key[i] = hashArray[i % hashArray.length];
  }
  
  return key;
}

/**
 * Encripta un payload usando AES-256-CBC
 * @param {string} payload - Payload a encriptar
 * @param {string} validationKey - Clave de validación
 * @returns {string} Payload encriptado en Base64
 */
export function encryptPayload(payload, validationKey) {
  try {
    // Generar IV aleatorio de 16 bytes
    const iv = CryptoJS.lib.WordArray.random(16);
    
    // Derivar la clave de 32 bytes
    const keyBytes = deriveKey(validationKey, 32);
    const key = CryptoJS.lib.WordArray.create(keyBytes);
    
    // Encriptar usando AES-256-CBC
    const encrypted = CryptoJS.AES.encrypt(payload, key, {
      iv: iv,
      mode: CryptoJS.mode.CBC,
      padding: CryptoJS.pad.Pkcs7
    });
    
    // Combinar IV + datos encriptados
    const combined = iv.concat(encrypted.ciphertext);
    
    // Convertir a Base64
    return combined.toString(CryptoJS.enc.Base64);
  } catch (error) {
    console.error('Error encriptando payload:', error);
    throw new Error('Error al encriptar el payload');
  }
}

/**
 * Genera un JWT token llamando a la API
 * @returns {Promise<string|null>} Token JWT o null si falla
 */
export async function generateJWTToken() {
  try {
    console.log('🔄 Generando JWT token...');
    
    // 1. Generar timestamp UTC-6
    const timestamp = generateTimestamp();
    console.log('📅 Timestamp UTC-6:', new Date(timestamp).toISOString());
    
    // 2. Crear payload
    const payload = createPayload(API_CONFIG.secretKey, timestamp);
    console.log('📦 Payload creado:', payload.substring(0, 50) + '...');
    
    // 3. Encriptar payload
    const encryptedPayload = encryptPayload(payload, API_CONFIG.validationKey);
    console.log('🔐 Payload encriptado:', encryptedPayload.substring(0, 50) + '...');
    
    // 4. Enviar a la API
    const response = await fetch(`${API_CONFIG.baseUrl}${API_CONFIG.endpoint}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      },
      body: JSON.stringify({
        encryptedPayload: encryptedPayload
      })
    });
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    
    const result = await response.json();
    console.log('📡 Respuesta de la API:', result);
    
    // 5. Verificar respuesta
    if (result.success && result.data?.success) {
      const token = result.data.token;
      const expiresAt = new Date(result.data.expiresAt);
      
      // 6. Almacenar token
      storeToken(token, expiresAt);
      
      console.log('✅ Token generado exitosamente');
      console.log('⏰ Expira en:', expiresAt.toISOString());
      
      return token;
    } else {
      console.error('❌ Error en la respuesta de la API:', result.message);
      return null;
    }
    
  } catch (error) {
    console.error('❌ Error generando JWT token:', error);
    return null;
  }
}

/**
 * Almacena el token y su fecha de expiración
 * @param {string} token - Token JWT
 * @param {Date} expiresAt - Fecha de expiración
 */
export function storeToken(token, expiresAt) {
  try {
    localStorage.setItem(STORAGE_KEYS.TOKEN, token);
    localStorage.setItem(STORAGE_KEYS.EXPIRES_AT, expiresAt.toISOString());
    console.log('💾 Token almacenado en localStorage');
  } catch (error) {
    console.error('❌ Error almacenando token:', error);
  }
}

/**
 * Obtiene el token almacenado
 * @returns {string|null} Token JWT o null si no existe
 */
export function getStoredToken() {
  try {
    return localStorage.getItem(STORAGE_KEYS.TOKEN);
  } catch (error) {
    console.error('❌ Error obteniendo token:', error);
    return null;
  }
}

/**
 * Verifica si el token está expirado
 * @returns {boolean} True si está expirado o no existe
 */
export function isTokenExpired() {
  try {
    const expiresAtStr = localStorage.getItem(STORAGE_KEYS.EXPIRES_AT);
    if (!expiresAtStr) return true;
    
    const expiresAt = new Date(expiresAtStr);
    const now = new Date();
    
    // Considerar expirado si falta menos de 5 minutos
    const timeUntilExpiry = expiresAt.getTime() - now.getTime();
    const isExpired = timeUntilExpiry < (5 * 60 * 1000); // 5 minutos
    
    if (isExpired) {
      console.log('⏰ Token expirado o próximo a expirar');
    }
    
    return isExpired;
  } catch (error) {
    console.error('❌ Error verificando expiración:', error);
    return true;
  }
}

/**
 * Obtiene headers de autenticación
 * @returns {Object} Headers con Authorization Bearer
 */
export function getAuthHeaders() {
  const token = getStoredToken();
  if (!token) {
    console.warn('⚠️ No hay token disponible');
    return {};
  }
  
  return {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  };
}

/**
 * Obtiene un token válido (genera uno nuevo si es necesario)
 * @returns {Promise<string|null>} Token JWT válido o null si falla
 */
export async function getValidToken() {
  // Si no hay token o está expirado, generar uno nuevo
  if (!getStoredToken() || isTokenExpired()) {
    console.log('🔄 Generando nuevo token...');
    return await generateJWTToken();
  }
  
  // Si hay token válido, devolverlo
  console.log('✅ Usando token existente');
  return getStoredToken();
}

/**
 * Limpia el token almacenado (logout)
 */
export function logout() {
  try {
    localStorage.removeItem(STORAGE_KEYS.TOKEN);
    localStorage.removeItem(STORAGE_KEYS.EXPIRES_AT);
    console.log('🚪 Logout realizado - Token eliminado');
  } catch (error) {
    console.error('❌ Error en logout:', error);
  }
}

/**
 * Verifica si el usuario está autenticado
 * @returns {boolean} True si está autenticado y el token es válido
 */
export function isAuthenticated() {
  const token = getStoredToken();
  return token && !isTokenExpired();
}

// Exportar configuración para uso externo
export { API_CONFIG };
```

### 2. Archivo: `apiClient.js`

```javascript
import { getValidToken, getAuthHeaders, logout } from './authService';

// Configuración base de la API
const API_BASE_URL = 'https://vh-apimanagement.azure-api.net/shared-vh/api';

/**
 * Cliente HTTP con autenticación automática
 */
class ApiClient {
  constructor() {
    this.baseURL = API_BASE_URL;
  }

  /**
   * Realiza una petición HTTP con autenticación automática
   * @param {string} endpoint - Endpoint de la API
   * @param {Object} options - Opciones de fetch
   * @returns {Promise<Object>} Respuesta de la API
   */
  async request(endpoint, options = {}) {
    try {
      // Obtener token válido
      const token = await getValidToken();
      if (!token) {
        throw new Error('No se pudo obtener un token válido');
      }

      // Preparar headers
      const headers = {
        ...getAuthHeaders(),
        ...options.headers
      };

      // Realizar petición
      const response = await fetch(`${this.baseURL}${endpoint}`, {
        ...options,
        headers
      });

      // Si es 401, el token puede haber expirado
      if (response.status === 401) {
        console.log('🔄 Token expirado, generando nuevo...');
        logout(); // Limpiar token expirado
        const newToken = await getValidToken();
        
        if (newToken) {
          // Reintentar con nuevo token
          const retryHeaders = {
            ...getAuthHeaders(),
            ...options.headers
          };
          
          const retryResponse = await fetch(`${this.baseURL}${endpoint}`, {
            ...options,
            headers: retryHeaders
          });
          
          return await this.handleResponse(retryResponse);
        }
      }

      return await this.handleResponse(response);

    } catch (error) {
      console.error('❌ Error en petición API:', error);
      throw error;
    }
  }

  /**
   * Maneja la respuesta de la API
   * @param {Response} response - Respuesta de fetch
   * @returns {Promise<Object>} Datos parseados
   */
  async handleResponse(response) {
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }

    const data = await response.json();
    return data;
  }

  // Métodos HTTP
  async get(endpoint, options = {}) {
    return this.request(endpoint, { ...options, method: 'GET' });
  }

  async post(endpoint, data, options = {}) {
    return this.request(endpoint, {
      ...options,
      method: 'POST',
      body: JSON.stringify(data)
    });
  }

  async put(endpoint, data, options = {}) {
    return this.request(endpoint, {
      ...options,
      method: 'PUT',
      body: JSON.stringify(data)
    });
  }

  async delete(endpoint, options = {}) {
    return this.request(endpoint, { ...options, method: 'DELETE' });
  }
}

// Instancia singleton
export const apiClient = new ApiClient();

// Funciones de conveniencia para endpoints específicos
export const customerApi = {
  // Obtener customers con filtros opcionales
  getCustomers: (params = {}) => {
    const queryString = new URLSearchParams(params).toString();
    const endpoint = queryString ? `/corporate/customer?${queryString}` : '/corporate/customer';
    return apiClient.get(endpoint);
  },

  // Obtener customer por ID
  getCustomerById: (id) => apiClient.get(`/corporate/customer?id=${id}`),

  // Obtener customer por NIT
  getCustomerByNIT: (nit) => apiClient.get(`/corporate/customer?nit=${nit}`),

  // Buscar customers por nombre
  searchCustomers: (companyName) => apiClient.get(`/corporate/customer?companyName=${companyName}`),

  // Crear customer
  createCustomer: (customerData) => apiClient.post('/corporate/customer', customerData),

  // Actualizar customer
  updateCustomer: (id, customerData) => apiClient.put(`/corporate/customer/${id}`, customerData),

  // Eliminar customer
  deleteCustomer: (id) => apiClient.delete(`/corporate/customer/${id}`),

  // Actualizar fecha de último contacto
  updateLastContact: (id) => apiClient.patch(`/corporate/customer/${id}/last-contact`)
};
```

### 3. Archivo: `useAuth.js` (Hook personalizado)

```javascript
import { useState, useEffect, useCallback } from 'react';
import { 
  generateJWTToken, 
  getStoredToken, 
  isTokenExpired, 
  logout as authLogout,
  isAuthenticated 
} from './authService';

/**
 * Hook personalizado para manejar autenticación
 */
export function useAuth() {
  const [isLoading, setIsLoading] = useState(false);
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [error, setError] = useState(null);

  // Verificar estado de autenticación al cargar
  useEffect(() => {
    const checkAuthStatus = () => {
      const authenticated = isAuthenticated();
      setIsLoggedIn(authenticated);
    };

    checkAuthStatus();
  }, []);

  /**
   * Iniciar sesión (generar token)
   */
  const login = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const token = await generateJWTToken();
      
      if (token) {
        setIsLoggedIn(true);
        console.log('✅ Login exitoso');
      } else {
        setError('No se pudo generar el token');
        setIsLoggedIn(false);
      }
    } catch (err) {
      setError(err.message);
      setIsLoggedIn(false);
    } finally {
      setIsLoading(false);
    }
  }, []);

  /**
   * Cerrar sesión
   */
  const logout = useCallback(() => {
    authLogout();
    setIsLoggedIn(false);
    setError(null);
    console.log('🚪 Logout exitoso');
  }, []);

  /**
   * Refrescar token
   */
  const refreshToken = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const token = await generateJWTToken();
      
      if (token) {
        setIsLoggedIn(true);
        console.log('🔄 Token refrescado');
      } else {
        setError('No se pudo refrescar el token');
        setIsLoggedIn(false);
      }
    } catch (err) {
      setError(err.message);
      setIsLoggedIn(false);
    } finally {
      setIsLoading(false);
    }
  }, []);

  return {
    isLoggedIn,
    isLoading,
    error,
    login,
    logout,
    refreshToken
  };
}
```

## 📖 Ejemplos de Uso

### 1. Uso Básico con Hook

```jsx
import React from 'react';
import { useAuth } from './useAuth';
import { customerApi } from './apiClient';

function App() {
  const { isLoggedIn, isLoading, error, login, logout } = useAuth();
  const [customers, setCustomers] = useState([]);

  // Cargar customers cuando esté autenticado
  useEffect(() => {
    if (isLoggedIn) {
      loadCustomers();
    }
  }, [isLoggedIn]);

  const loadCustomers = async () => {
    try {
      const response = await customerApi.getCustomers();
      setCustomers(response.data);
    } catch (error) {
      console.error('Error cargando customers:', error);
    }
  };

  if (isLoading) {
    return <div>Cargando...</div>;
  }

  if (!isLoggedIn) {
    return (
      <div>
        <h1>VHConsultor - Autenticación Requerida</h1>
        <button onClick={login}>Iniciar Sesión</button>
        {error && <p style={{color: 'red'}}>Error: {error}</p>}
      </div>
    );
  }

  return (
    <div>
      <h1>VHConsultor - Customers</h1>
      <button onClick={logout}>Cerrar Sesión</button>
      <button onClick={loadCustomers}>Cargar Customers</button>
      
      <ul>
        {customers.map(customer => (
          <li key={customer.customerId}>
            {customer.companyName} - {customer.nit}
          </li>
        ))}
      </ul>
    </div>
  );
}

export default App;
```

### 2. Uso Directo de la API

```javascript
import { customerApi } from './apiClient';

// Ejemplos de uso de la API
async function examples() {
  try {
    // Obtener todos los customers
    const allCustomers = await customerApi.getCustomers();
    console.log('Todos los customers:', allCustomers);

    // Buscar por ID
    const customer = await customerApi.getCustomerById(123);
    console.log('Customer por ID:', customer);

    // Buscar por NIT
    const customerByNIT = await customerApi.getCustomerByNIT('12345678');
    console.log('Customer por NIT:', customerByNIT);

    // Buscar por nombre
    const searchResults = await customerApi.searchCustomers('Microsoft');
    console.log('Búsqueda por nombre:', searchResults);

    // Filtros avanzados
    const filteredCustomers = await customerApi.getCustomers({
      clientStatus: 'Active',
      priority: 'High',
      city: 'San José'
    });
    console.log('Customers filtrados:', filteredCustomers);

    // Crear nuevo customer
    const newCustomer = await customerApi.createCustomer({
      companyName: 'Nueva Empresa',
      nit: '123456789',
      primaryEmail: 'contacto@nuevaempresa.com'
    });
    console.log('Customer creado:', newCustomer);

  } catch (error) {
    console.error('Error en operaciones API:', error);
  }
}
```

### 3. Uso con Axios (Alternativo)

```javascript
import axios from 'axios';
import { getValidToken } from './authService';

// Configurar interceptor de axios
const api = axios.create({
  baseURL: 'https://vh-apimanagement.azure-api.net/shared-vh/api'
});

// Interceptor para agregar token automáticamente
api.interceptors.request.use(async (config) => {
  const token = await getValidToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Interceptor para manejar errores 401
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Token expirado, intentar renovar
      const newToken = await getValidToken();
      if (newToken) {
        // Reintentar petición original
        return api.request(error.config);
      }
    }
    return Promise.reject(error);
  }
);

export default api;
```

## 🔧 Configuración de Variables de Entorno

### Archivo: `.env`

```env
REACT_APP_API_BASE_URL=https://vh-apimanagement.azure-api.net/shared-vh/api
REACT_APP_JWT_SECRET_KEY=VHC0n5ult0r!2024$S3cur3K3y#F0rJWT@T0k3nG3n3r4t10n&V4l1d4t10n
REACT_APP_JWT_VALIDATION_KEY=AES256V4l1d4t10nK3y!2024@VHC0n5ult0r#Encryp7&D3cryp7$S3cur3
```

### Uso de Variables de Entorno

```javascript
// En authService.js
const API_CONFIG = {
  baseUrl: process.env.REACT_APP_API_BASE_URL,
  secretKey: process.env.REACT_APP_JWT_SECRET_KEY,
  validationKey: process.env.REACT_APP_JWT_VALIDATION_KEY,
  endpoint: '/Security/generate-token'
};
```

## 🐛 Debugging y Logs

El servicio incluye logs detallados para facilitar el debugging:

- `🔄` - Operaciones en progreso
- `✅` - Operaciones exitosas
- `❌` - Errores
- `⚠️` - Advertencias
- `📅` - Timestamps
- `📦` - Payloads
- `🔐` - Encriptación
- `📡` - Respuestas de API
- `💾` - Almacenamiento
- `🚪` - Logout

## ⚠️ Consideraciones de Seguridad

1. **Nunca expongas las claves en el código del frontend** - Usa variables de entorno
2. **El token expira en 60 minutos** - Implementa renovación automática
3. **El timestamp tiene ventana de 1 minuto** - Genera timestamps precisos
4. **Usa HTTPS siempre** - La API ya está en HTTPS
5. **Limpia tokens al cerrar sesión** - Implementa logout completo

## 📚 Estructura de Archivos Recomendada

```
src/
├── services/
│   ├── authService.js
│   ├── apiClient.js
│   └── useAuth.js
├── components/
│   ├── LoginButton.jsx
│   ├── CustomerList.jsx
│   └── CustomerForm.jsx
├── pages/
│   ├── LoginPage.jsx
│   └── DashboardPage.jsx
└── App.jsx
```

## 🚀 Implementación Paso a Paso

1. **Instalar dependencias**: `npm install crypto-js`
2. **Crear archivos de servicio**: Copiar `authService.js`, `apiClient.js`, `useAuth.js`
3. **Configurar variables de entorno**: Crear `.env` con las claves
4. **Implementar en componentes**: Usar `useAuth` hook
5. **Probar autenticación**: Verificar logs en consola
6. **Implementar funcionalidades**: Usar `customerApi` para operaciones

## 📞 Soporte

Si encuentras problemas:

1. Revisa los logs en la consola del navegador
2. Verifica que las claves sean correctas
3. Confirma que el timestamp sea UTC-6
4. Verifica que la encriptación sea AES-256-CBC
5. Revisa la respuesta de la API en Network tab

---

**¡Listo para implementar! 🎉**
