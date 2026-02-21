# Script de Teste - OAuth do Zero

## 📋 Resumo do Projeto

✅ **Servidor de Identidade OAuth 2.0 / OpenID Connect** criado do zero em .NET 8

### 🎯 Funcionalidades Implementadas

- **🔐 OAuth 2.0 Authorization Code Flow** com suporte a PKCE
- **🆔 OpenID Connect** com claim mapping completo  
- **🛡️ Segurança**: BCrypt para senhas, tokens seguros, validação PKCE
- **📄 JWT**: Geração e validação de access tokens
- **💾 Banco de Dados**: Entity Framework Core + MySQL
- **🖥️ Interface Web**: Login e páginas de consentimento

### 🏗️ Arquitetura

```
📦 OAuthDoZero.Server
├── 📁 Models/              # Entidades do banco (User, Client, Token, etc.)
├── 📁 Services/            # Lógica de negócio (OAuth, JWT, Crypto)
├── 📁 Controllers/         # APIs REST + MVCs (Auth, Token, UserInfo, etc.)
├── 📁 ViewModels/          # DTOs para as telas web
├── 📁 Views/               # Páginas HTML/CSS (Login, Consent, Home)
└── 📁 Data/                # Context EF Core + Migrations
```

### 🔗 Endpoints Disponíveis

| Endpoint | Descrição |
|----------|-----------|
| `GET /` | 🏠 Página inicial |
| `GET /Account/Login` | 🔑 Página de login |
| `GET /oauth/authorize` | 🚪 Autorização OAuth |
| `POST /oauth/token` | 🪙 Geração de tokens |
| `GET /oauth/userinfo` | 👤 Informações do usuário |
| `GET /.well-known/openid-configuration` | 📄 Descoberta OIDC |

### ⚡ Como Testar

1. **Iniciar o servidor**:
   ```bash
   cd src/OAuthDoZero.Server
   dotnet run --urls "http://localhost:5041"
   ```

2. **Criar um usuário** (POST para `/api/users`):
   ```json
   {
     "username": "admin",
     "email": "admin@teste.com",
     "password": "MinhaSenh@123"
   }
   ```

3. **Criar um cliente OAuth** (inserir no banco):
   ```sql
   INSERT INTO Clients (Id, ClientId, ClientSecret, GrantTypes, RedirectUris, Scopes, RequirePkce)
   VALUES (1, 'testapp', 'secret123', 'authorization_code', 'http://localhost:3000/callback', 'openid profile email', 1);
   ```

4. **Testar fluxo OAuth**:
   - Navegue para: `http://localhost:5041/oauth/authorize?client_id=testapp&redirect_uri=http://localhost:3000/callback&response_type=code&scope=openid%20profile%20email&code_challenge=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk&code_challenge_method=S256&state=xyz123`
   - Faça login
   - Autorize o acesso
   - Receba o código de autorização
   - Troque por access token

### 🎨 Destaques da Implementação

- **✨ Interface moderna**: CSS com gradientes e glassmorphism
- **🔒 Segurança robusta**: PKCE obrigatório, validação completa
- **📱 Responsivo**: Layout adaptável para mobile
- **🧩 Modular**: Separação clara de responsabilidades
- **💡 Educativo**: Código limpo e bem documentado

### 🚀 Tecnologias Utilizadas

- **.NET 8** - Framework principal
- **ASP.NET Core MVC** - Interface web
- **Entity Framework Core** - ORM
- **MySQL** - Banco de dados  
- **BCrypt.Net** - Hash de senhas
- **System.IdentityModel.Tokens.Jwt** - JWT
- **CSS3** - Estilização moderna

---

**🎯 Objetivo Alcançado**: Servidor OAuth 2.0 / OpenID Connect completo e funcional, criado 100% do zero para fins educativos, com todas as funcionalidades de um servidor de identidade profissional!
