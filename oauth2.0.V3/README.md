# oauth2.0.V3 — Servidor de identidade (protótipo)

Solução com:

- **`lrfdev.auth.api`** — API de autenticação (login/senha, JWT, EF Core + MySQL)
- **`lrfdev.auth.web`** — tela de login (MVC)

> Evolução planejada: OAuth 2.0, OpenID Connect, Authorization Code + PKCE, Client Credentials, Device Authorization.

---

## Banco de dados (`auth-api`)

Estrutura **RBAC híbrida**: permissões podem ser atribuídas **ao usuário** ou **ao grupo** (o usuário herda as do grupo).

### Tabelas

| Tabela | Função |
|--------|--------|
| `users` | Usuários: `Id`, `Username`, `Email`, `PasswordHash`, `IsActive` |
| `permissions` | Catálogo de permissões: `Id`, `Name`, `Description` |
| `permission_groups` | Grupos: `Id`, `Name` |
| `user_permissions` | N:N usuário → permissão **direta** (`UserId`, `PermissionId`) |
| `user_groups` | N:N usuário → grupo (`UserId`, `GroupId`) |
| `group_permissions` | N:N grupo → permissão (`GroupId`, `PermissionId`) |

### Índices únicos

- `users.Username`, `users.Email`
- `permissions.Name`
- `permission_groups.Name`

### Resolução no login (`POST /api/auth/login`)

1. Valida usuário e senha.
2. Coleta permissões de `user_permissions`.
3. Coleta permissões via `user_groups` → `group_permissions`.
4. Une e remove duplicatas (`DISTINCT`).
5. Inclui as permissões no JWT como claim `permission`.

### Seed inicial (desenvolvimento)

- Usuário: `admin` / senha: `Admin@123`
- Grupo: `admins`
- Permissões: `auth.manage`, `auth.read`

---

## Scripts SQL (MySQL)

Use o banco `auth-api` (ou ajuste o `USE`).

### 1) Permissões efetivas por usuário (origem: direta ou grupo)

```sql
USE `auth-api`;

WITH direct AS (
    SELECT
        u.Id AS user_id,
        u.Username,
        u.Email,
        p.Name AS permission_name,
        'direct' AS source
    FROM users u
    JOIN user_permissions up ON up.UserId = u.Id
    JOIN permissions p ON p.Id = up.PermissionId
),
from_groups AS (
    SELECT
        u.Id AS user_id,
        u.Username,
        u.Email,
        p.Name AS permission_name,
        'group' AS source
    FROM users u
    JOIN user_groups ug ON ug.UserId = u.Id
    JOIN group_permissions gp ON gp.GroupId = ug.GroupId
    JOIN permissions p ON p.Id = gp.PermissionId
)
SELECT * FROM direct
UNION ALL
SELECT * FROM from_groups
ORDER BY Username, permission_name, source;
```

### 2) Mesma ideia com `UNION` (sem CTE)

```sql
USE `auth-api`;

-- Permissões diretas
SELECT
    u.Id AS user_id,
    u.Username,
    p.Name AS permission_name,
    'direct' AS source
FROM users u
JOIN user_permissions up ON up.UserId = u.Id
JOIN permissions p ON p.Id = up.PermissionId

UNION

-- Permissões herdadas do grupo
SELECT
    u.Id AS user_id,
    u.Username,
    p.Name AS permission_name,
    'group' AS source
FROM users u
JOIN user_groups ug ON ug.UserId = u.Id
JOIN group_permissions gp ON gp.GroupId = ug.GroupId
JOIN permissions p ON p.Id = gp.PermissionId

ORDER BY Username, permission_name, source;
```

### 3) Lista única de permissões por usuário (sem duplicar `direct`/`group`)

```sql
USE `auth-api`;

SELECT
    u.Id AS user_id,
    u.Username,
    p.Name AS permission_name
FROM users u
JOIN (
    SELECT up.UserId AS UserId, up.PermissionId AS PermissionId
    FROM user_permissions up
    UNION
    SELECT ug.UserId, gp.PermissionId
    FROM user_groups ug
    JOIN group_permissions gp ON gp.GroupId = ug.GroupId
) AS effective ON effective.UserId = u.Id
JOIN permissions p ON p.Id = effective.PermissionId
ORDER BY u.Username, p.Name;
```

### 4) Grupos e permissões de um usuário específico

```sql
USE `auth-api`;

SET @username = 'admin';

SELECT g.Name AS group_name, p.Name AS permission_name
FROM users u
JOIN user_groups ug ON ug.UserId = u.Id
JOIN permission_groups g ON g.Id = ug.GroupId
JOIN group_permissions gp ON gp.GroupId = g.Id
JOIN permissions p ON p.Id = gp.PermissionId
WHERE u.Username = @username
ORDER BY g.Name, p.Name;
```

---

## Migrations (EF Core)

Na pasta do projeto da API:

```bash
dotnet ef migrations add NomeDaMigration --project lrfdev.auth.api/lrfdev.auth.api.csproj --startup-project lrfdev.auth.api/lrfdev.auth.api.csproj --output-dir Data/Migrations
dotnet ef database update --project lrfdev.auth.api/lrfdev.auth.api.csproj --startup-project lrfdev.auth.api/lrfdev.auth.api.csproj
```

---

## URLs locais típicas (launch profiles)

| Projeto | HTTPS |
|---------|--------|
| `lrfdev.auth.api` | `https://localhost:7286` |
| `lrfdev.auth.web` | `https://localhost:7040` |

### Acesso pelo celular (mesma rede Wi‑Fi)

1. **`localhost` no celular é o próprio aparelho** — use o **IPv4 do PC** (ex.: `192.168.0.15`). No Windows: `ipconfig` → “Endereço IPv4”.
2. O HTTP precisa aceitar a **rede local**: no perfil **`https`** o bind já inclui `http://0.0.0.0:5067` (web) e `http://0.0.0.0:5036` (API). Alternativa: perfil **`network`** (só HTTP).
3. **Firewall do Windows**: permitir entrada TCP **5036** (API) e **5067** (web), ou desativar temporariamente para teste.
4. **HTTPS com certificado de dev** no celular costuma dar erro — o perfil `network` usa **HTTP** para facilitar.
5. **Tela em branco no celular com `http://IP:5067`**: o projeto **não força HTTPS em Development** (o redirect quebrava o acesso pelo IP + certificado de dev no celular). O HTTP do perfil `https` escuta em **`0.0.0.0:5067`** para aceitar a rede local.
6. **QR Code / Device flow**: a API monta o link com `DeviceAuthorization:VerificationBaseUrl`. Para o celular abrir a página certa, esse URL deve usar **o IP do PC**, não `localhost`. Ajuste em `lrfdev.auth.api/appsettings.Development.json`:

```json
"DeviceAuthorization": {
  "VerificationBaseUrl": "http://SEU_IP_AQUI:5067/Device/Authorize"
}
```

**Subir com rede (exemplo):**

```powershell
# Terminal 1 — API (HTTP em todas as interfaces, porta 5036)
dotnet run --project lrfdev.auth.api\lrfdev.auth.api.csproj --launch-profile network

# Terminal 2 — Web (HTTP em todas as interfaces, porta 5067)
dotnet run --project lrfdev.auth.web\lrfdev.auth.web.csproj --launch-profile network
```

No celular: `http://SEU_IP:5067` (ex.: `http://192.168.0.15:5067`).

Login na web: `/Account/Login`  
API de login: `POST /api/auth/login`

### OAuth (protótipo)

- **Authorization Code + PKCE**: `GET /connect/authorize` → `POST /connect/token` (`grant_type=authorization_code`)
- **Client Credentials (M2M)**: `POST /connect/token` com `grant_type=client_credentials`, `client_id`, `client_secret`, `scope` opcional
- **Device Authorization (TV / IoT, RFC 8628)**:
  1. `POST /connect/deviceauthorization` (`client_id`, `scope` opcional) → `device_code`, `user_code`, `verification_uri`, `verification_uri_complete`, `expires_in`, `interval`
  2. Usuário abre `verification_uri` no celular (ou escaneia QR com `verification_uri_complete`), informa `user_code` e login em `POST /connect/device`
  3. Dispositivo faz poll em `POST /connect/token` com `grant_type=urn:ietf:params:oauth:grant-type:device_code` + `device_code` + `client_id` até receber `access_token` ou erro (`authorization_pending`, `slow_down`, etc.)

Configuração na API: `DeviceAuthorization:VerificationBaseUrl` (ex.: `https://localhost:7040/Device/Authorize`).

Cliente público de demo (seed): `lrfdev.tv.device` com `AllowDeviceAuthorization = true`.

**Simulação com QR no celular (web `lrfdev.auth.web`):**

1. Abra `https://localhost:7040/Device/SimulateTv` (ou o link na tela de login).
2. Clique em **Iniciar fluxo na TV** — será exibido o **user code** e um **QR Code** (payload = `verification_uri_complete`).
3. No celular, escaneie o QR ou abra o link; faça login e autorize em `/Device/Authorize`.
4. A página da “TV” fará polling e mostrará o JSON com `access_token` quando o fluxo concluir.

Cliente M2M de desenvolvimento (seed):

- `client_id`: `lrfdev.service.m2m`
- `client_secret`: `m2m-secret-123`
- **Scopes permitidos**: `auth.read`, `auth.manage`, `orders.read`, `orders.write` (vários no mesmo token conforme o parâmetro `scope`)

### API recurso de demonstração (políticas por escopo)

Endpoints em `lrfdev.auth.api` (header `Authorization: Bearer <access_token>`):

| Método | Rota | Política |
|--------|------|----------|
| GET | `/api/demo/whoami` | apenas autenticado |
| GET | `/api/demo/read` | `Scope:auth.read` |
| GET | `/api/demo/manage` | `Scope:auth.manage` |
| GET | `/api/demo/orders` | `Scope:orders.read` |
| POST | `/api/demo/orders` | `Scope:orders.write` |

A autorização aceita claim **`permission`** (várias) e/ou **`scope`** (lista separada por espaço), como emitido nos JWTs atuais.

**Teste rápido (PowerShell)** — token só com `orders.read`, depois chamar recurso:

```powershell
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
$form = 'grant_type=client_credentials&client_id=lrfdev.service.m2m&client_secret=m2m-secret-123&scope=orders.read'
$tok = Invoke-RestMethod -Uri 'https://localhost:7286/connect/token' -Method Post -Body $form -ContentType 'application/x-www-form-urlencoded'
# Deve funcionar:
Invoke-RestMethod -Uri 'https://localhost:7286/api/demo/orders' -Headers @{ Authorization = "Bearer $($tok.access_token)" }
# Deve falhar (403):
Invoke-WebRequest -Uri 'https://localhost:7286/api/demo/read' -Headers @{ Authorization = "Bearer $($tok.access_token)" }
```

No **Swagger** (`/swagger`), use **Authorize** e cole `Bearer <token>`.
