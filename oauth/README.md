# Servidor de Identidade OAuth 2.0 e OpenID Connect (OIDC)

Servidor de identidade **próprio** em .NET 8, sem IdentityServer nem outros frameworks. Utiliza **OAuth 2.0**, **OpenID Connect**, **Entity Framework Core** e **MySQL**.

## Funcionalidades

- **OAuth 2.0**
  - Fluxo **Authorization Code** (com suporte a **PKCE**)
  - **Refresh token**
  - Endpoints: `/oauth/authorize`, `/oauth/token`
- **OpenID Connect**
  - Discovery: `/.well-known/openid-configuration`
  - UserInfo: `/oauth/userinfo`
  - ID Token (JWT)
  - JWKS: `/oauth/jwks`
- **Persistência**: MySQL com Entity Framework Core
- **Segurança**: senhas com BCrypt; tokens JWT assinados (HS256); PKCE opcional por cliente

## Pré-requisitos

- .NET 8 SDK
- MySQL (local ou remoto)

## Configuração

1. **Connection string** em `appsettings.json` ou `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3306;Database=OAuthIdentity;User=root;Password=SUA_SENHA;"
}
```

2. **OAuth** em `appsettings.json`:

```json
"OAuth": {
  "Issuer": "https://localhost:7001",
  "SigningKey": "sua-chave-secreta-muito-longa-minimo-32-caracteres-para-hs256",
  "AccessTokenLifetimeMinutes": 60,
  "RefreshTokenLifetimeDays": 30,
  "AuthorizationCodeLifetimeMinutes": 10
}
```

- **Issuer**: URL base do servidor (deve bater com a URL em que a API sobe).
- **SigningKey**: chave para assinar os JWTs (mínimo 32 caracteres para HS256).

3. Crie o banco no MySQL (opcional; o EF pode criar só o schema):

```sql
CREATE DATABASE IF NOT EXISTS OAuthIdentity CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

## Executando

```bash
cd src/OAuthIdentityServer
dotnet run
```

Na primeira execução, as **migrations** são aplicadas. Em **Development**, um usuário e um cliente de teste são criados automaticamente (seed).

### Dados de teste (seed em Development)

| Tipo   | Valor        | Detalhe              |
|--------|--------------|----------------------|
| Usuário | `admin`      | Senha: `Admin@123`   |
| Cliente | `demo-client`| Secret: `secret`     |
| Redirect URIs | -      | `https://localhost:5001/callback`, `http://localhost:5000/callback`, `https://oidcdebugger.com/debug` |

## Endpoints

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/.well-known/openid-configuration` | Documento de descoberta OIDC |
| GET | `/oauth/authorize` | Início do fluxo (exibe login) |
| POST | `/oauth/authorize` | Login e emissão do authorization code |
| POST | `/oauth/token` | Troca de code por tokens / refresh token |
| GET/POST | `/oauth/userinfo` | Claims do usuário (Bearer token) |
| GET | `/oauth/jwks` | Chaves para validação de assinatura |

## Fluxo Authorization Code (exemplo)

1. **Autorização (navegador)**  
   Abra:
   ```
   https://localhost:7001/oauth/authorize?response_type=code&client_id=demo-client&redirect_uri=https://oidcdebugger.com/debug&scope=openid%20profile%20email%20offline_access&state=abc123&code_challenge=CODECHALLENGE&code_challenge_method=S256
   ```
   Faça login (ex.: `admin` / `Admin@123`). Será redirecionado para `redirect_uri` com `?code=...&state=abc123`.

2. **Token (POST)**  
   Troque o `code` por tokens:
   ```
   POST /oauth/token
   Content-Type: application/x-www-form-urlencoded

   grant_type=authorization_code&code=CODIGO_RECEBIDO&redirect_uri=https://oidcdebugger.com/debug&client_id=demo-client&client_secret=secret&code_verifier=CODE_VERIFIER_USADO_PARA_GERAR_CODE_CHALLENGE
   ```

3. **UserInfo (GET)**  
   Use o `access_token`:
   ```
   GET /oauth/userinfo
   Authorization: Bearer SEU_ACCESS_TOKEN
   ```

## Estrutura do projeto

```
src/OAuthIdentityServer/
├── Configuration/     # OAuthOptions
├── Controllers/       # OAuthController, OidcDiscoveryController
├── Data/              # IdentityDbContext, SeedData
├── Dto/               # TokenResponse
├── Migrations/        # EF Core
├── Models/            # User, Client, AuthorizationCode, RefreshToken
├── Services/          # TokenService, UserService, ClientService
├── appsettings.json
└── Program.cs
```

## Observações

- **Produção**: troque a **SigningKey** por uma chave forte e única; considere **RS256** e expor a chave pública no JWKS.
- **HTTPS**: use sempre HTTPS em produção para o Issuer e para os redirect_uri.
- **Clientes**: cadastre novos clientes na tabela `Clients` (e ajuste redirect URIs, scopes, PKCE, etc.).

## Licença

Uso livre conforme o repositório.



docker run -d \
  --name mysql84_container \
  -e MYSQL_ROOT_PASSWORD=L3@ndr0 \
  -v mysql84_data:/var/lib/mysql \
  -p 3306:3306 \
  mysql:8.4.8