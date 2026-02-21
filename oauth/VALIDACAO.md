# Guia de Validação - OAuth Identity Server

## 1. Validação estática (já realizada)

- **Build:** `dotnet build` na pasta `src/OAuthIdentityServer` — **OK**
- Código compila sem erros e sem avisos.

## 2. Pré-requisitos para validação em execução

- **.NET 8 SDK**
- **MySQL** em execução (porta 3306)
- Banco e usuário conforme `appsettings.json` / `appsettings.Development.json`:
  - Database: `OAuthIdentity`
  - Connection string em `ConnectionStrings:DefaultConnection`

## 3. Configuração

- **Issuer:** Em Development o Issuer está em `appsettings.Development.json` (ex.: `https://localhost:7172` ou `http://localhost:5185`). Deve coincidir com a URL em que você acessa o servidor, senão clientes OIDC podem rejeitar os tokens.
- **Portas (launchSettings):**
  - HTTP: `http://localhost:5185`
  - HTTPS: `https://localhost:7172`

## 4. Executar a aplicação

```powershell
cd src\OAuthIdentityServer
dotnet run
```

Ou pelo perfil HTTPS:

```powershell
dotnet run --launch-profile https
```

Na primeira execução, as migrations são aplicadas e o seed é executado em ambiente Development (usuário e cliente de teste).

## 5. Dados de teste (Seed)

- **Usuário:** `admin` / senha: `Admin@123`
- **Cliente:** `demo-client` / secret: `secret`
- **Redirect URIs do cliente:** `https://localhost:5001/callback`, `http://localhost:5000/callback`, `https://oidcdebugger.com/debug`

## 6. Testes manuais com `test-api.http`

Use o arquivo **`test-api.http`** na raiz do repositório (com extensão REST Client no VS Code/Cursor ou similar):

| Teste | Descrição |
|-------|-----------|
| **1. Discovery** | `GET http://localhost:5185/.well-known/openid-configuration` — documento OIDC |
| **2. JWKS** | `GET http://localhost:5185/oauth/jwks` — chaves para validação |
| **3. Login** | `GET .../oauth/authorize?response_type=code&client_id=demo-client&redirect_uri=...` — deve retornar HTML do formulário de login |
| **4. Token** | Após fazer login e obter o `code` no redirect, trocar por token em `POST .../oauth/token` (substituir `SEU_CODE_AQUI`) |
| **5. UserInfo** | `GET .../oauth/userinfo` com `Authorization: Bearer SEU_ACCESS_TOKEN_AQUI` |

Fluxo completo sugerido:

1. Abrir no navegador:  
   `http://localhost:5185/oauth/authorize?response_type=code&client_id=demo-client&redirect_uri=https://oidcdebugger.com/debug&scope=openid&state=test123`
2. Fazer login com `admin` / `Admin@123`.
3. Ser redirecionado para o redirect_uri com `?code=...&state=test123`; copiar o `code`.
4. Em `test-api.http`, no Teste 4, colar o `code` e enviar o POST para `/oauth/token`.
5. Usar o `access_token` retornado no Teste 5 (UserInfo).

## 7. Checklist rápido

- [ ] Build: `dotnet build` sem erros
- [ ] MySQL rodando e connection string correta
- [ ] `dotnet run` sobe sem exceção e migrations aplicadas
- [ ] Discovery: `/.well-known/openid-configuration` retorna JSON
- [ ] JWKS: `/oauth/jwks` retorna JSON com chave(s)
- [ ] Autorização: GET `/oauth/authorize` retorna página de login
- [ ] Login: POST com usuário/senha redireciona com `code`
- [ ] Token: POST `/oauth/token` com `grant_type=authorization_code` retorna `access_token` (e opcionalmente `id_token`, `refresh_token`)
- [ ] UserInfo: GET `/oauth/userinfo` com Bearer retorna claims do usuário

## 8. Observações de segurança (produção)

- Trocar **SigningKey** por valor forte e único (mín. 32 caracteres para HS256).
- Usar **HTTPS** e Issuer com HTTPS.
- Considerar **RS256** e expor apenas a chave pública no JWKS (atualmente o servidor usa HS256 com chave simétrica).
- Não commitar senhas reais ou connection strings de produção no repositório.
