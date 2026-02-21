# 🤖 Autenticação Machine-to-Machine (M2M) - OAuth do Zero

## ✅ **RESPOSTA:** SIM! O projeto agora suporta autenticação Machine-to-Machine!

### 🚀 **Funcionalidade Implementada:**

**📋 Grant Type:** `client_credentials` (RFC 6749 Section 4.4)  
**🎯 Uso:** Autenticação de serviço para serviço (sem usuário humano)  
**🔒 Segurança:** Requer cliente confidencial com client_secret  

---

## 🛠️ **Como Configurar Cliente M2M:**

### 1. **Criar Cliente no Banco de Dados:**
```sql
INSERT INTO Clients (
    Id, ClientId, ClientSecret, Name, 
    ClientType, GrantTypes, AllowedScopes,
    RequirePkce, RequireClientSecret, AllowOfflineAccess,
    AccessTokenLifetime, CreatedAt, UpdatedAt
) VALUES (
    UUID(), 'service-api', 'my-super-secret-key', 'API Service Client',
    'confidential', 'client_credentials', 'api:read api:write data:access',
    0, 1, 0,
    7200, NOW(), NOW()
);
```

### 2. **Campos Importantes para M2M:**
| Campo | Valor M2M | Explicação |
|-------|-----------|------------|
| `ClientType` | `'confidential'` | Obrigatório para M2M |
| `GrantTypes` | `'client_credentials'` | Habilita M2M |
| `AllowedScopes` | `'api:read api:write'` | Escopos de API (não user) |
| `RequirePkce` | `0` | PKCE não é usado em M2M |
| `RequireClientSecret` | `1` | Obrigatório para segurança |

---

## 🔧 **Como Usar (Cliente M2M):**

### **Obter Access Token:**
```bash
curl -X POST http://localhost:9000/oauth/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=service-api&client_secret=my-super-secret-key&scope=api:read api:write"
```

### **Resposta Esperada:**
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 7200,
  "scope": "api:read api:write"
}
```

### **Usar Access Token:**
```bash
curl -X GET http://localhost:9000/oauth/userinfo \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

---

## 🧪 **Script de Teste PowerShell:**

```powershell
# Teste Machine-to-Machine
$tokenBody = @{
    grant_type = "client_credentials"
    client_id = "service-api"
    client_secret = "my-super-secret-key"
    scope = "api:read api:write"
} | ConvertTo-Json

# Obter token M2M
$response = Invoke-RestMethod -Uri "http://localhost:9000/oauth/token" `
    -Method POST -Body $tokenBody -ContentType "application/json"

Write-Host "🎁 Access Token: $($response.access_token)"
Write-Host "⏰ Expira em: $($response.expires_in) segundos"

# Usar token para chamar API
$headers = @{ Authorization = "Bearer $($response.access_token)" }
$userInfo = Invoke-RestMethod -Uri "http://localhost:9000/oauth/userinfo" `
    -Headers $headers

Write-Host "✅ Token M2M funcional!"
```

---

## 🔍 **Diferenças M2M vs User Auth:**

| Aspecto | User Auth | Machine-to-Machine |
|---------|-----------|-------------------|
| **Grant Type** | `authorization_code` | `client_credentials` |
| **Subject (sub)** | User ID | Client ID |
| **Scopes** | `openid profile email` | `api:read api:write` |
| **ID Token** | ✅ Sim | ❌ Não |
| **Refresh Token** | ✅ Sim | ❌ Não |
| **Interactive Login** | ✅ Sim | ❌ Não |
| **PKCE** | ✅ Requerido | ❌ Não usado |

---

## 🎯 **JWT Token Claims (M2M):**

```json
{
  "sub": "service-api",           // Client ID como subject
  "client_id": "service-api",     // ID do cliente
  "client_type": "machine",       // Indica M2M
  "scope": "api:read api:write",  // Escopos permitidos
  "jti": "unique-jwt-id",         // Token ID único
  "iat": 1645123456,              // Emitido em
  "exp": 1645130656               // Expira em
}
```

---

## ⚡ **Vantagens da Implementação:**

- ✅ **RFC 6749 Compliant** - Segue padrão OAuth 2.0
- ✅ **Seguro** - Requer cliente confidencial + secret
- ✅ **Escalável** - Tokens JWT stateless
- ✅ **Flexível** - Escopos customizáveis por cliente
- ✅ **Auditável** - Claims incluem tipo de autenticação
- ✅ **Performance** - Sem interação humana necessária

---

## 🚨 **Segurança M2M:**

1. **Client Secret:** Deve ser guardado de forma segura
2. **HTTPS Only:** Comunicação sempre criptografada
3. **Scopes Limitados:** Dar apenas permissões necessárias
4. **Token Lifetime:** Configurar expiração adequada
5. **Rotação:** Rotacionar client secrets periodicamente

---

**🎉 Pronto! Seu servidor OAuth agora suporta autenticação Machine-to-Machine completa!**