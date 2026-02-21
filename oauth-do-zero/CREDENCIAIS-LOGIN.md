# 🔑 Credenciais de Login - OAuth do Zero

## 👤 **USUÁRIO DE TESTE CRIADO:**

| Campo | Valor |
|-------|--------|
| **👤 Usuário** | `admin` |
| **🔑 Senha** | `Admin123!` |
| **📧 Email** | `admin@teste.local` |
| **👨‍💼 Nome** | `Administrador Sistema` |

## 🌐 **COMO FAZER LOGIN:**

1. **Acesse a página de login:**
   ```
   http://localhost:9000/Account/Login
   ```

2. **Digite as credenciais:**
   - **Username:** `admin`
   - **Password:** `Admin123!`

3. **Clique em "Entrar"**

## 🚀 **TESTAR FLUXO OAUTH COMPLETO:**

### 1. **Primeiro - Criar Cliente no Banco:**
Execute este SQL no MySQL:
```sql
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
```

### 2. **Teste Authorization Code Flow:**
Acesse esta URL (substitua as quebras de linha):
```
http://localhost:9000/oauth/authorize?client_id=testapp&redirect_uri=http://localhost:3000/callback&response_type=code&scope=openid%20profile%20email&state=test123&code_challenge=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk&code_challenge_method=S256
```

### 3. **Fluxo Esperado:**
1. **Redirecionado para login** → Use `admin` / `Admin123!`
2. **Página de consentimento** → Autorize os escopos
3. **Redirect com código** → Receba o código de autorização
4. **Trocar por token** → Use `/oauth/token` endpoint

## 🔧 **ENDPOINTS DISPONÍVEIS:**

| Endpoint | Descrição |
|----------|-----------|
| `/Account/Login` | 🔑 Página de login |
| `/oauth/authorize` | 🚪 Autorização OAuth |
| `/oauth/token` | 🪙 Obter tokens |
| `/oauth/userinfo` | 👤 Informações do usuário |
| `/api/users` | 👥 Listar usuários (dev) |

## 📝 **EXEMPLO DE TESTE COM CURL:**

```bash
# Obter access token (após ter o código de autorização) 
curl -X POST http://localhost:9000/oauth/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code&client_id=testapp&client_secret=secret123&code=SEU_CODIGO&redirect_uri=http://localhost:3000/callback&code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"

# Usar o access token
curl -X GET http://localhost:9000/oauth/userinfo \
  -H "Authorization: Bearer SEU_ACCESS_TOKEN"
```

## 🤖 **AUTENTICAÇÃO MACHINE-TO-MACHINE:**

✅ **O projeto agora suporta M2M!** Veja [MACHINE-TO-MACHINE.md](MACHINE-TO-MACHINE.md)

```bash
# Teste M2M (após criar cliente confidencial)
curl -X POST http://localhost:9000/oauth/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=service-api&client_secret=my-super-secret-key&scope=api:read api:write"
```

---

**✅ Pronto para testar o sistema OAuth 2.0 / OpenID Connect!** 🚀