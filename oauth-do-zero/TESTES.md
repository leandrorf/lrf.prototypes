# Testes do OAuth Server

## HTTP Client Tests (.http file)

```http
### 1. Criar um usuário
POST http://localhost:5041/api/users
Content-Type: application/json

{
  "username": "admin",
  "email": "admin@teste.com", 
  "password": "MinhaSenh@123",
  "firstName": "Administrador",
  "lastName": "Sistema"
}

### 2. Testar descoberta OpenID Connect
GET http://localhost:5041/.well-known/openid-configuration

### 3. Página inicial
GET http://localhost:5041/

### 4. Página de login
GET http://localhost:5041/Account/Login

### 5. Endpoint de autorização (deve redirecionar para login)
GET http://localhost:5041/oauth/authorize?client_id=testapp&redirect_uri=http://localhost:3000/callback&response_type=code&scope=openid%20profile%20email&state=xyz123&code_challenge=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk&code_challenge_method=S256

### 6. UserInfo endpoint (precisa de token)
GET http://localhost:5041/oauth/userinfo
Authorization: Bearer YOUR_ACCESS_TOKEN_HERE
```

## Comandos SQL para inserir cliente de teste

```sql
-- Conectar ao MySQL e usar a database oauth
USE oauth;

-- Inserir cliente de teste
INSERT INTO Clients (
    Id, ClientId, ClientSecret, Name, 
    GrantTypes, RedirectUris, Scopes, 
    RequirePkce, RequireClientSecret, AllowOfflineAccess,
    CreatedAt, UpdatedAt
) VALUES (
    UUID(), 'testapp', 'secret123', 'Aplicacao de Teste',
    'authorization_code', 'http://localhost:3000/callback', 'openid profile email',
    1, 1, 1,
    NOW(), NOW()
);

-- Verificar se foi inserido
SELECT * FROM Clients WHERE ClientId = 'testapp';
```

## PowerShell Scripts de Teste

```powershell
# Teste completo do fluxo OAuth 2.0

# 1. Criar usuário
$userBody = @{
    username = "admin"
    email = "admin@teste.com"
    password = "MinhaSenh@123"
    firstName = "Admin"
    lastName = "User"
} | ConvertTo-Json

$userResponse = Invoke-RestMethod -Uri "http://localhost:5041/api/users" -Method POST -Body $userBody -ContentType "application/json"
Write-Host "Usuário criado: $($userResponse.message)"

# 2. Testar descoberta OIDC
$discovery = Invoke-RestMethod -Uri "http://localhost:5041/.well-known/openid-configuration"
Write-Host "Endpoints descobertos:"
Write-Host "- Authorization: $($discovery.authorization_endpoint)"
Write-Host "- Token: $($discovery.token_endpoint)"
Write-Host "- UserInfo: $($discovery.userinfo_endpoint)"

# 3. Testar página inicial
try {
    $homeResponse = Invoke-WebRequest -Uri "http://localhost:5041/" -UseBasicParsing
    Write-Host "Página inicial: HTTP $($homeResponse.StatusCode)"
}
catch {
    Write-Host "Erro na página inicial: $($_.Exception.Message)"
}

Write-Host "`n✅ Testes básicos concluídos!"
Write-Host "🌐 Abra http://localhost:5041 no navegador para testar a interface completa"
```

## Fluxo Completo de Teste Manual

1. **Iniciar servidor**: `dotnet run --urls "http://localhost:5041"`
2. **Criar usuário**: Execute o POST para `/api/users`
3. **Inserir cliente**: Execute o SQL de inserção do cliente
4. **Testar fluxo**:
   - Acesse: `http://localhost:5041/oauth/authorize?client_id=testapp&redirect_uri=http://localhost:3000/callback&response_type=code&scope=openid%20profile%20email&code_challenge=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk&code_challenge_method=S256&state=xyz123`
   - Faça login com: admin / MinhaSenh@123
   - Autorize o acesso
   - Observe o redirect com o código de autorização

## Valores PKCE para Teste
- **Code Verifier**: `dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk`
- **Code Challenge**: `E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM`
- **Method**: `S256`

*Gerados com: `Base64UrlEncode(SHA256(ASCII(code_verifier)))`*