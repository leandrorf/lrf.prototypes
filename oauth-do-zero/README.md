# OAuth do Zero

Servidor de identidade próprio em **.NET 8**, sem IdentityServer nem outros frameworks, usando:

- **OAuth 2.0** (authorization code, tokens)
- **OpenID Connect** (discovery, userinfo, JWT)
- **Entity Framework Core** + **MySQL**

## Estrutura do projeto

- `src/OAuthDoZero.Server` – API do servidor de identidade

## Desenvolvimento passo a passo

1. **Passo 1** ✅ – Solução e projeto Web API .NET 8
2. **Passo 2** – Entity Framework Core + MySQL (DbContext e modelos base)
3. **Passo 3** – Tabelas OAuth (clientes, códigos de autorização, refresh tokens)
4. **Passo 4** – Endpoints OAuth 2.0 (`/authorize`, `/token`)
5. **Passo 5** – OpenID Connect (discovery, userinfo, assinatura JWT)

## Como rodar

```bash
cd src/OAuthDoZero.Server
dotnet run
```

A API responde em `https://localhost:7xxx` (ou a porta exibida no terminal). `GET /` retorna o nome e a versão do servidor.

## Pré-requisitos

- .NET 8 SDK
- MySQL (para os passos com EF Core)

## Erro ao compilar (arquivo em uso)

Se o build falhar com **MSB3027** / **MSB3021** ou "Access to the path ... is denied", a aplicação ainda está rodando e está bloqueando os arquivos em `bin/` e `obj/`.

**O que fazer:** feche a aplicação antes de compilar de novo:
- Se iniciou com `dotnet run`: pressione **Ctrl+C** no terminal.
- Se iniciou pelo IDE (F5): pare a depuração (Stop).
- Depois rode `dotnet build` de novo.
