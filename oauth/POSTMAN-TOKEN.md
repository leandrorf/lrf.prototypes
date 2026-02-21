# Como obter um token no Postman

O servidor usa o fluxo **Authorization Code com PKCE**. São dois passos: (1) obter um **código** no navegador e (2) trocar esse código por **token** no Postman.

---

## Passo 1: Obter o authorization code no navegador

1. **Certifique-se de que a aplicação está rodando** (por exemplo: `dotnet run` em `src\OAuthIdentityServer`).

2. **Abra esta URL no navegador** (tudo em uma linha):

   ```
   http://localhost:5185/oauth/authorize?response_type=code&client_id=demo-client&redirect_uri=https://oidcdebugger.com/debug&scope=openid%20profile%20email%20offline_access&state=postman&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256
   ```

3. **Faça login** quando aparecer a tela:
   - **Usuário:** `admin`
   - **Senha:** `Admin@123`

4. Você será redirecionado para algo como:
   ```
   https://oidcdebugger.com/debug?code=XXXXXXXX&state=postman
   ```
   **Copie o valor do parâmetro `code`** (só o código, sem `code=` e sem `&state=...`).  
   Esse código é válido por poucos minutos (ex.: 10). Use-o logo no Passo 2.

---

## Passo 2: Trocar o código por token no Postman

1. **Método e URL**
   - **Método:** `POST`
   - **URL:** `http://localhost:5185/oauth/token`

2. **Body**
   - Aba **Body** → marque **x-www-form-urlencoded**.
   - Adicione estes pares chave/valor (use exatamente o mesmo `redirect_uri` do Passo 1 e o **code** que você copiou):

   | Key            | Value                                                                 |
   |----------------|-----------------------------------------------------------------------|
   | grant_type     | `authorization_code`                                                  |
   | code           | *(cole aqui o código copiado no Passo 1)*                             |
   | redirect_uri   | `https://oidcdebugger.com/debug`                                      |
   | client_id      | `demo-client`                                                         |
   | client_secret  | `secret`                                                              |
   | code_verifier  | `dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk`                         |

   O `code_verifier` acima é o par do `code_challenge` usado na URL do Passo 1 (PKCE). Não altere esse valor.

3. **Headers**
   - Não é obrigatório, mas pode definir:  
     **Content-Type:** `application/x-www-form-urlencoded`

4. Clique em **Send**.

5. **Resposta esperada (200 OK):** algo como:
   ```json
   {
     "access_token": "eyJhbG...",
     "token_type": "Bearer",
     "expires_in": 3600,
     "refresh_token": "...",
     "scope": "openid profile email offline_access",
     "id_token": "eyJhbG..."
   }
   ```

Use o `access_token` onde precisar (por exemplo, no header **Authorization: Bearer &lt;access_token&gt;** para chamar `/oauth/userinfo` ou outras APIs).

---

## Usar o access token no Postman (ex.: UserInfo)

- **Método:** `GET`
- **URL:** `http://localhost:5185/oauth/userinfo`
- **Authorization:** aba **Authorization** → Type **Bearer Token** → cole o `access_token` obtido acima.

---

## Renovar o token (refresh_token)

Quando o `access_token` expirar, você pode obter um novo sem passar de novo pelo navegador:

- **Método:** `POST`
- **URL:** `http://localhost:5185/oauth/token`
- **Body (x-www-form-urlencoded):**

  | Key           | Value            |
  |---------------|------------------|
  | grant_type    | `refresh_token`  |
  | refresh_token | *(o refresh_token da resposta anterior)* |
  | client_id     | `demo-client`    |
  | client_secret | `secret`         |

---

## Resumo rápido

| Item            | Valor |
|-----------------|--------|
| Token URL      | `http://localhost:5185/oauth/token` |
| Client ID      | `demo-client` |
| Client Secret  | `secret` |
| Redirect URI   | `https://oidcdebugger.com/debug` (ou outro permitido no servidor) |
| Usuário teste | `admin` / `Admin@123` |
| code_verifier (PKCE) | `dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk` |

Se aparecer erro `invalid_grant` ao trocar o código, o código pode ter expirado ou já ter sido usado — repita o Passo 1 para obter um código novo.
