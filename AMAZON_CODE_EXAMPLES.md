# Amazon SP-API - Ejemplos de Código

## 📋 Contenido

1. [Ejemplo C# - Uso del Servicio](#ejemplo-c---uso-del-servicio)
2. [Ejemplo JavaScript/TypeScript - Frontend](#ejemplo-javascripttypescript---frontend)
3. [Ejemplo Python - Scripts](#ejemplo-python---scripts)
4. [Ejemplo cURL - Testing](#ejemplo-curl---testing)

---

## Ejemplo C# - Uso del Servicio

### 1. Generar Token desde otro Servicio

```csharp
using ApplicationLayer.Amazon;
using BusinessLayer.Amazon.Commands;

public class MyAmazonService
{
    private readonly AmazonAuthService _amazonAuthService;
    
    public MyAmazonService(AmazonAuthService amazonAuthService)
    {
        _amazonAuthService = amazonAuthService;
    }
    
    public async Task<string> GetAccessTokenAsync()
    {
        // Opción 1: Obtener token activo existente
        var activeToken = await _amazonAuthService.GetActiveTokenAsync();
        
        if (activeToken != null && activeToken.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
        {
            return activeToken.AccessToken;
        }
        
        // Opción 2: Generar nuevo token si no hay activo o está por expirar
        var command = new GenerateAccessTokenCommand
        {
            RefreshToken = "Atzr|IwEBIFQVzXIt...",
            ClientId = "amzn1.application-oa2-client...",
            ClientSecret = "amzn1.oa2-cs..."
        };
        
        var result = await _amazonAuthService.GenerateAccessTokenAsync(command);
        
        if (result.Success)
        {
            return result.AccessToken!;
        }
        
        throw new Exception($"Error al generar token: {result.Message}");
    }
}
```

### 2. Usar Token para Llamar a Amazon SP-API

```csharp
using System.Net.Http.Headers;

public class AmazonOrdersService
{
    private readonly AmazonAuthService _amazonAuthService;
    private readonly HttpClient _httpClient;
    
    public AmazonOrdersService(
        AmazonAuthService amazonAuthService,
        HttpClient httpClient)
    {
        _amazonAuthService = amazonAuthService;
        _httpClient = httpClient;
    }
    
    public async Task<string> GetOrdersAsync()
    {
        // Obtener token activo
        var token = await _amazonAuthService.GetActiveTokenAsync();
        
        if (token == null)
        {
            throw new Exception("No hay token activo disponible");
        }
        
        // Configurar headers
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token.AccessToken);
        
        _httpClient.DefaultRequestHeaders.Add("x-amz-access-token", token.AccessToken);
        
        // Llamar a Amazon SP-API
        var response = await _httpClient.GetAsync(
            "https://sellingpartnerapi-na.amazon.com/orders/v0/orders");
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }
        
        throw new Exception($"Error al obtener órdenes: {response.StatusCode}");
    }
}
```

### 3. Job para Renovar Tokens Automáticamente

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class AmazonTokenRenewalJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AmazonTokenRenewalJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(50); // Renovar cada 50 minutos
    
    public AmazonTokenRenewalJob(
        IServiceProvider serviceProvider,
        ILogger<AmazonTokenRenewalJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Amazon Token Renewal Job iniciado");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var amazonAuthService = scope.ServiceProvider
                    .GetRequiredService<ApplicationLayer.Amazon.AmazonAuthService>();
                
                // Verificar si hay un token activo
                var activeToken = await amazonAuthService.GetActiveTokenAsync();
                
                // Si no hay token o está por expirar (menos de 10 minutos)
                if (activeToken == null || 
                    activeToken.ExpiresAt <= DateTime.UtcNow.AddMinutes(10))
                {
                    _logger.LogInformation("Renovando token de Amazon...");
                    
                    var command = new BusinessLayer.Amazon.Commands.GenerateAccessTokenCommand
                    {
                        RefreshToken = "Atzr|IwEBIFQVzXIt...",
                        ClientId = "amzn1.application-oa2-client...",
                        ClientSecret = "amzn1.oa2-cs..."
                    };
                    
                    var result = await amazonAuthService.GenerateAccessTokenAsync(command);
                    
                    if (result.Success)
                    {
                        _logger.LogInformation("Token renovado exitosamente. Expira: {ExpiresAt}", 
                            result.ExpiresAt);
                    }
                    else
                    {
                        _logger.LogError("Error al renovar token: {Message}", result.Message);
                    }
                }
                else
                {
                    _logger.LogInformation("Token activo válido hasta: {ExpiresAt}", 
                        activeToken.ExpiresAt);
                }
                
                // Desactivar tokens expirados
                var deactivated = await amazonAuthService.DeactivateExpiredTokensAsync();
                if (deactivated > 0)
                {
                    _logger.LogInformation("{Count} token(s) expirado(s) desactivado(s)", deactivated);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en Amazon Token Renewal Job");
            }
            
            await Task.Delay(_interval, stoppingToken);
        }
    }
}

// Registrar en Program.cs
builder.Services.AddHostedService<AmazonTokenRenewalJob>();
```

---

## Ejemplo JavaScript/TypeScript - Frontend

### 1. Servicio de Amazon en React/Next.js

```typescript
// services/amazonService.ts

interface AmazonTokenResponse {
  success: boolean;
  message: string;
  data: {
    tokenId: number;
    accessToken: string;
    refreshToken: string;
    tokenType: string;
    expiresIn: number;
    expiresAt: string;
  };
}

interface ActiveTokenResponse {
  success: boolean;
  message: string;
  data: {
    tokenId: number;
    accessToken: string;
    tokenType: string;
    expiresIn: number;
    createdAt: string;
    expiresAt: string;
    clientId: string;
  };
}

class AmazonService {
  private baseUrl = 'https://localhost:5001/api/amazon/amazonauth';
  private jwtToken: string | null = null;

  setJwtToken(token: string) {
    this.jwtToken = token;
  }

  async generateToken(
    refreshToken: string,
    clientId: string,
    clientSecret: string
  ): Promise<AmazonTokenResponse> {
    const response = await fetch(`${this.baseUrl}/generate-token`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        refreshToken,
        clientId,
        clientSecret,
      }),
    });

    if (!response.ok) {
      throw new Error(`Error: ${response.statusText}`);
    }

    return await response.json();
  }

  async getActiveToken(): Promise<ActiveTokenResponse> {
    if (!this.jwtToken) {
      throw new Error('JWT Token no configurado');
    }

    const response = await fetch(`${this.baseUrl}/active-token`, {
      headers: {
        'Authorization': `Bearer ${this.jwtToken}`,
      },
    });

    if (!response.ok) {
      throw new Error(`Error: ${response.statusText}`);
    }

    return await response.json();
  }

  async getOrGenerateToken(): Promise<string> {
    try {
      // Intentar obtener token activo
      const activeTokenResponse = await this.getActiveToken();
      
      if (activeTokenResponse.success) {
        const expiresAt = new Date(activeTokenResponse.data.expiresAt);
        const now = new Date();
        const minutesUntilExpiry = (expiresAt.getTime() - now.getTime()) / 60000;

        // Si el token expira en menos de 5 minutos, generar uno nuevo
        if (minutesUntilExpiry > 5) {
          return activeTokenResponse.data.accessToken;
        }
      }
    } catch (error) {
      console.log('No hay token activo, generando uno nuevo...');
    }

    // Generar nuevo token
    const newTokenResponse = await this.generateToken(
      process.env.AMAZON_REFRESH_TOKEN!,
      process.env.AMAZON_CLIENT_ID!,
      process.env.AMAZON_CLIENT_SECRET!
    );

    if (newTokenResponse.success) {
      return newTokenResponse.data.accessToken;
    }

    throw new Error('No se pudo obtener un token de Amazon');
  }
}

export const amazonService = new AmazonService();
```

### 2. Hook de React para Usar el Servicio

```typescript
// hooks/useAmazonToken.ts

import { useState, useEffect } from 'react';
import { amazonService } from '../services/amazonService';

export function useAmazonToken() {
  const [token, setToken] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadToken();
  }, []);

  const loadToken = async () => {
    try {
      setLoading(true);
      const accessToken = await amazonService.getOrGenerateToken();
      setToken(accessToken);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error desconocido');
      setToken(null);
    } finally {
      setLoading(false);
    }
  };

  const refreshToken = async () => {
    await loadToken();
  };

  return { token, loading, error, refreshToken };
}

// Uso en un componente
function MyComponent() {
  const { token, loading, error, refreshToken } = useAmazonToken();

  if (loading) return <div>Cargando token...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      <p>Token: {token?.substring(0, 20)}...</p>
      <button onClick={refreshToken}>Renovar Token</button>
    </div>
  );
}
```

---

## Ejemplo Python - Scripts

### Script para Generar Token

```python
# amazon_token_generator.py

import requests
import json
from datetime import datetime

class AmazonTokenManager:
    def __init__(self, base_url, refresh_token, client_id, client_secret):
        self.base_url = base_url
        self.refresh_token = refresh_token
        self.client_id = client_id
        self.client_secret = client_secret
        self.jwt_token = None

    def set_jwt_token(self, token):
        """Configura el JWT token para endpoints protegidos"""
        self.jwt_token = token

    def generate_token(self):
        """Genera un nuevo access token"""
        url = f"{self.base_url}/api/amazon/amazonauth/generate-token"
        
        payload = {
            "refreshToken": self.refresh_token,
            "clientId": self.client_id,
            "clientSecret": self.client_secret
        }
        
        headers = {
            "Content-Type": "application/json"
        }
        
        response = requests.post(url, json=payload, headers=headers, verify=False)
        
        if response.status_code == 200:
            data = response.json()
            if data["success"]:
                print(f"✅ Token generado exitosamente")
                print(f"Token ID: {data['data']['tokenId']}")
                print(f"Access Token: {data['data']['accessToken'][:50]}...")
                print(f"Expira: {data['data']['expiresAt']}")
                return data["data"]
            else:
                print(f"❌ Error: {data['message']}")
                return None
        else:
            print(f"❌ Error HTTP {response.status_code}: {response.text}")
            return None

    def get_active_token(self):
        """Obtiene el token activo más reciente"""
        if not self.jwt_token:
            print("⚠️  JWT Token no configurado")
            return None
            
        url = f"{self.base_url}/api/amazon/amazonauth/active-token"
        
        headers = {
            "Authorization": f"Bearer {self.jwt_token}"
        }
        
        response = requests.get(url, headers=headers, verify=False)
        
        if response.status_code == 200:
            data = response.json()
            if data["success"]:
                print(f"✅ Token activo obtenido")
                print(f"Access Token: {data['data']['accessToken'][:50]}...")
                print(f"Expira: {data['data']['expiresAt']}")
                return data["data"]
            else:
                print(f"❌ Error: {data['message']}")
                return None
        elif response.status_code == 404:
            print("⚠️  No hay tokens activos disponibles")
            return None
        else:
            print(f"❌ Error HTTP {response.status_code}: {response.text}")
            return None

    def get_all_tokens(self, is_active=None, limit=10):
        """Obtiene lista de tokens con filtros"""
        if not self.jwt_token:
            print("⚠️  JWT Token no configurado")
            return None
            
        url = f"{self.base_url}/api/amazon/amazonauth/tokens"
        
        params = {"limit": limit}
        if is_active is not None:
            params["isActive"] = str(is_active).lower()
        
        headers = {
            "Authorization": f"Bearer {self.jwt_token}"
        }
        
        response = requests.get(url, headers=headers, params=params, verify=False)
        
        if response.status_code == 200:
            data = response.json()
            if data["success"]:
                tokens = data["data"]
                print(f"✅ {len(tokens)} token(s) encontrado(s)")
                for token in tokens:
                    print(f"\n  Token ID: {token['tokenId']}")
                    print(f"  Activo: {token['isActive']}")
                    print(f"  Expira: {token['expiresAt']}")
                return tokens
            else:
                print(f"❌ Error: {data['message']}")
                return None
        else:
            print(f"❌ Error HTTP {response.status_code}: {response.text}")
            return None

# Uso
if __name__ == "__main__":
    # Configuración
    BASE_URL = "https://localhost:5001"
    REFRESH_TOKEN = "Atzr|IwEBIFQVzXIt..."
    CLIENT_ID = "YOUR_AMAZON_CLIENT_ID"
    CLIENT_SECRET = "YOUR_AMAZON_CLIENT_SECRET"
    
    # Crear manager
    manager = AmazonTokenManager(BASE_URL, REFRESH_TOKEN, CLIENT_ID, CLIENT_SECRET)
    
    # Generar token
    print("=== Generando Token ===")
    token_data = manager.generate_token()
    
    # Si tienes JWT token, puedes obtener el token activo
    # manager.set_jwt_token("tu_jwt_token_aqui")
    # print("\n=== Obteniendo Token Activo ===")
    # active_token = manager.get_active_token()
```

---

## Ejemplo cURL - Testing

### 1. Generar Token

```bash
curl -X POST https://localhost:5001/api/amazon/amazonauth/generate-token \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "refreshToken": "YOUR_AMAZON_REFRESH_TOKEN",
    "clientId": "YOUR_AMAZON_CLIENT_ID",
    "clientSecret": "YOUR_AMAZON_CLIENT_SECRET"
  }' | jq
```

### 2. Obtener Token Activo

```bash
curl -X GET https://localhost:5001/api/amazon/amazonauth/active-token \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -k | jq
```

### 3. Listar Tokens

```bash
curl -X GET "https://localhost:5001/api/amazon/amazonauth/tokens?isActive=true&limit=5" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -k | jq
```

### 4. Desactivar Tokens Expirados

```bash
curl -X POST https://localhost:5001/api/amazon/amazonauth/deactivate-expired \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -k | jq
```

### 5. Script Bash Completo

```bash
#!/bin/bash

# amazon_token_test.sh

BASE_URL="https://localhost:5001"
REFRESH_TOKEN="YOUR_AMAZON_REFRESH_TOKEN"
CLIENT_ID="YOUR_AMAZON_CLIENT_ID"
CLIENT_SECRET="YOUR_AMAZON_CLIENT_SECRET"

echo "=== Generando Token de Amazon ==="
RESPONSE=$(curl -s -X POST "${BASE_URL}/api/amazon/amazonauth/generate-token" \
  -H "Content-Type: application/json" \
  -k \
  -d "{
    \"refreshToken\": \"${REFRESH_TOKEN}\",
    \"clientId\": \"${CLIENT_ID}\",
    \"clientSecret\": \"${CLIENT_SECRET}\"
  }")

echo "$RESPONSE" | jq

# Extraer access token
ACCESS_TOKEN=$(echo "$RESPONSE" | jq -r '.data.accessToken')

if [ "$ACCESS_TOKEN" != "null" ]; then
  echo ""
  echo "✅ Token generado exitosamente"
  echo "Access Token: ${ACCESS_TOKEN:0:50}..."
else
  echo ""
  echo "❌ Error al generar token"
  exit 1
fi
```

---

## 💡 Tips y Mejores Prácticas

### 1. Caché de Tokens

```csharp
// Usar IMemoryCache para cachear tokens
public class CachedAmazonAuthService
{
    private readonly AmazonAuthService _amazonAuthService;
    private readonly IMemoryCache _cache;
    private const string CACHE_KEY = "amazon_active_token";
    
    public async Task<string> GetAccessTokenAsync()
    {
        if (_cache.TryGetValue(CACHE_KEY, out string cachedToken))
        {
            return cachedToken;
        }
        
        var token = await _amazonAuthService.GetActiveTokenAsync();
        
        if (token != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(token.ExpiresAt.AddMinutes(-5));
            
            _cache.Set(CACHE_KEY, token.AccessToken, cacheOptions);
            return token.AccessToken;
        }
        
        throw new Exception("No hay token disponible");
    }
}
```

### 2. Retry Logic

```csharp
using Polly;

public async Task<string> GetTokenWithRetryAsync()
{
    var retryPolicy = Policy
        .Handle<HttpRequestException>()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    
    return await retryPolicy.ExecuteAsync(async () =>
    {
        var token = await _amazonAuthService.GetActiveTokenAsync();
        return token?.AccessToken ?? throw new Exception("No token available");
    });
}
```

---

¡Estos ejemplos te ayudarán a integrar el servicio de Amazon en diferentes contextos! 🚀
