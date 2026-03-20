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

Login na web: `/Account/Login`  
API de login: `POST /api/auth/login`
