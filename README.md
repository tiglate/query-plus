# QueryPlus

Execução governada de stored procedures do SQL Server para usuários de negócio — com catálogo, RBAC, trilha de auditoria e exportação para Excel.

Construído com **.NET 10**, **ASP.NET Core Web API (controllers)**, **React 19 + TypeScript (SPA com Vite)**, **EF Core**, **Dapper**, **Tailwind 4** e **Keycloak** (OpenID Connect).

**Idioma padrão:** Português do Brasil (`pt-BR`), com suporte a inglês (`en`).

## ✨ Funcionalidades

- 🏠 **Início** — escolha uma procedure catalogada, defina os parâmetros, execute, pagine grandes resultados no servidor e exporte para Excel
- 🗂️ **Admin** — gerencie categorias e procedures (parâmetros, colunas, sincronização de metadados a partir do SQL Server)
- 🔐 **Segurança** — OIDC via Keycloak (sessão em cookie + antiforgery); entitlements de papel (role) por procedure; argumentos reservados de paginação nunca expostos aos usuários finais
- 📋 **Operação** — log de execuções, tabelas de auditoria de configuração, dados de demonstração semeados na inicialização

## 📦 Estrutura da solução

```
QueryPlus.sln
src/
  QueryPlus.Api               # Web API do ASP.NET Core (controllers) + OIDC + host estático da SPA
  QueryPlus.Application       # Serviços de aplicação, DTOs, validadores FluentValidation
  QueryPlus.Domain            # Entidades, contratos de repositório (PKs INT)
  QueryPlus.Infrastructure    # Composition root para integrações externas
  QueryPlus.Data              # CRUD via EF Core + execução de stored procedures via Dapper
tests/
  QueryPlus.Application.Tests
  QueryPlus.Data.Tests
  QueryPlus.Api.Tests         # testes unitários de controllers + testes de integração HTTP via WebApplicationFactory
client/
  queryplus-react/            # SPA em React 19 + Vite + TS (Tailwind 4, TanStack Query, Radix/shadcn)
docker/
  keycloak/realm-export.json  # Realm de desenvolvimento (usuários: demo/demo, admin/admin)
.devcontainer/                # Dev Containers do VS Code / Codespaces
docs/
  SPECIFICATION.md
  database/                   # espelho do schema + SQL de demonstração
```

### Camadas

| Camada | Responsabilidade |
|-------|----------------|
| **Domain** | Entidades (PKs `int`), interfaces de repositório/UoW |
| **Application** | Casos de uso, interfaces de serviço, DTOs, validadores FluentValidation (`SqlIdentifier`, `ParameterSecurity`, `ProcedurePagination`) |
| **Data** | `DbContext`/repositórios do EF Core (CRUD + migrations), executor Dapper (`DataTable`), `DemoDataSeeder`, `AuditSaveChangesInterceptor` |
| **Infrastructure** | Conecta a Data + futuras integrações à injeção de dependência |
| **Api** | Controllers da Web API, autenticação OIDC, ProblemDetails, host estático da SPA, composição de DI |

- **EF Core** — CRUD do catálogo e migrations
- **Dapper / ADO.NET** — resultados dinâmicos de stored procedures como `DataTable`
- **SPA em React** — `client/queryplus-react/`, Vite em modo dev na porta `:5173` (faz proxy de `/api` e `/login` para a API), o build de produção é gerado em `src/QueryPlus.Api/wwwroot/` e servido pela API como arquivos estáticos

## ✅ Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (SQL Server + Keycloak)
- [Node.js 22+](https://nodejs.org/) + [pnpm](https://pnpm.io/) 10+ para a SPA em React — necessário para o primeiro build e qualquer trabalho no frontend

## 🚀 Início rápido (local)

### 0. Configure os segredos locais (obrigatório)

As credenciais **não** ficam em `appsettings*.json`. Copie o template e edite apenas se necessário:

```bash
cp .env.example .env
```

| Arquivo | No git? | Finalidade |
|------|---------|---------|
| `.env.example` | ✅ sim | Valores padrão fictícios para uso local (template) |
| `.env` | ❌ **nunca** (no gitignore) | Seus valores, apenas nesta máquina |

⚠️ **Esses valores são credenciais fictícias de desenvolvimento.** Elas existem apenas para que o Docker e um notebook consigam subir rapidamente. **Não as utilize em produção, homologação ou qualquer ambiente compartilhado.** Rotacione os segredos, utilize um gerenciador de segredos e garanta HTTPS/senhas fortes antes de qualquer implantação real.

O Docker Compose lê o `.env` para a substituição de variáveis. O `dotnet run` também carrega um `.env` na raiz do repositório no ambiente do processo (sem sobrescrever variáveis já definidas pelo shell ou pela CI).

### 1. Suba a infraestrutura

```bash
docker compose up -d sqlserver keycloak
```

| Serviço | URL / conexão (valores fictícios de `.env.example`) |
|---------|------------------|
| SQL Server | `localhost:1433` (sa / valor de `MSSQL_SA_PASSWORD`) |
| Keycloak | http://localhost:8080 (`KEYCLOAK_ADMIN` / `KEYCLOAK_ADMIN_PASSWORD`) |
| Realm | `queryplus` (importado automaticamente) |
| Usuários de demonstração | `demo` / `demo` (`ROLE_QUERY_EXEC`, `ROLE_CATEGORY_READ`, `ROLE_PROCEDURE_READ`), `admin` / `admin` (`ROLE_ADMIN`) — export do realm, uso local apenas |
| Client secret do OIDC | Deve coincidir com `Keycloak__ClientSecret` e com `docker/keycloak/realm-export.json` |

Os papéis (roles) do realm (`docker/keycloak/realm-export.json`) controlam o acesso à API, além do `RoleEntitlement` de cada procedure:

| Papel | Concede |
|------|--------|
| `ROLE_ADMIN` | Tudo abaixo, incondicionalmente |
| `ROLE_CATEGORY_READ` / `ROLE_CATEGORY_WRITE` | Visualizar/buscar categorias, ou CRUD completo de categorias |
| `ROLE_PROCEDURE_READ` / `ROLE_PROCEDURE_WRITE` | Visualizar/buscar o catálogo de procedures, ou CRUD completo de procedures |
| `ROLE_QUERY_EXEC` | Executar procedures e baixar exportações — ainda sujeito ao `RoleEntitlement` de cada procedure |

### 2. Aplique o schema do banco de dados

As migrations também rodam automaticamente pelo `DemoDataSeeder` na inicialização da aplicação. Para aplicar explicitamente:

```bash
dotnet tool install --global dotnet-ef   # uma vez
dotnet ef database update \
  --project src/QueryPlus.Data \
  --startup-project src/QueryPlus.Api
```

### 3. Faça o build da SPA em React (primeira vez / após alterações no frontend)

A SPA em React fica em `client/queryplus-react/` e o build é gerado em `src/QueryPlus.Api/wwwroot/` (fora do git) para que o `dotnet publish` e o `dotnet run` possam servi-la como conteúdo estático.

```bash
cd client/queryplus-react
pnpm install          # ou: vp install
pnpm run build        # → src/QueryPlus.Api/wwwroot/{assets,index.html}
```

#### Quando preciso rodar `pnpm run build`?

| Situação | Refazer o build da SPA? |
|-----------|---------------|
| Primeiro clone / `src/QueryPlus.Api/wwwroot/index.html` ausente | **Sim** — ou `dotnet build` / `dotnet run` (faz o build automaticamente quando `wwwroot/index.html` está ausente) |
| Você alterou arquivos em `client/queryplus-react/` | **Sim** — o `dotnet run` sozinho **não** refaz o build de um bundle já existente |
| Apenas alterações em .NET / C# | Não — o `dotnet run` já é suficiente |
| `dotnet publish` ou build da imagem Docker | Automático |

💡 **Dica:** após alterações em React/CSS, rode `pnpm run build` novamente (ou use o modo watch abaixo).

#### Desenvolvimento do frontend no dia a dia

```bash
# Terminal 1 — servidor Vite em http://localhost:5173 (faz proxy de /api e /login para a API)
cd client/queryplus-react
pnpm run dev          # ou: vp dev

# Terminal 2 — API do ASP.NET Core em http://localhost:5132
dotnet run --project src/QueryPlus.Api
```

```bash
cd client/queryplus-react

pnpm install && pnpm run build && pnpm test && pnpm run dev
```

Pule a etapa de build da SPA no MSBuild quando necessário:

```bash
dotnet publish ... /p:SkipClientAppBuild=true
```

O Vite está configurado com `server.port = 5173`, `strictPort`, e um proxy de desenvolvimento `/api` → `http://localhost:5132` (sobrescreva com `VITE_API_PROXY`). O bundle de produção é gerado como um único entry point não fragmentado em `assets/queryplus.js`.

### 4. Rode a API

```bash
dotnet run --project src/QueryPlus.Api
```

Abra a URL exibida pelo Kestrel — `http://localhost:5132` para o perfil de execução `http` (ou `https://localhost:7192` para o perfil `https`). A mesma origem serve a API JSON (`/api/...`), os endpoints de login/logout do Keycloak (`/login`, `/logout`, `/signout-callback`), e a SPA já compilada (`/`).

Configure as redirect URIs do client no Keycloak para corresponder a essa origem, se necessário (`docker/keycloak/realm-export.json`).

## 🧪 Build e testes

```bash
dotnet restore
dotnet build QueryPlus.sln    # faz o build da SPA apenas se src/QueryPlus.Api/wwwroot/index.html estiver ausente
dotnet test QueryPlus.sln

# Testes unitários da SPA (Vitest + jsdom)
cd client/queryplus-react && pnpm test
```

## 🐳 Docker (stack completa)

```bash
docker compose --profile full up --build
```

- API + SPA: http://localhost:5000
- Usa `appsettings.Docker.json` / variáveis de ambiente para SQL Server e Keycloak.

## 🧰 Dev Containers

1. Abra o repositório no VS Code / Cursor.
2. **Dev Containers: Reopen in Container**.
3. O SQL Server e o Keycloak sobem via Compose; o .NET 10 está disponível no serviço `app`.

```bash
dotnet run --project src/QueryPlus.Api --urls http://0.0.0.0:5000
```

## ⚙️ Configuração

Prefira **variáveis de ambiente** (incluindo as do `.env`) em vez de versionar segredos.

| Configuração / variável de ambiente | Descrição |
|-------------------|-------------|
| `ConnectionStrings__DefaultConnection` | String de conexão com o SQL Server |
| `Keycloak__Authority` | ex.: `http://localhost:8080/realms/queryplus` |
| `Keycloak__ClientId` | `queryplus-web` |
| `Keycloak__ClientSecret` | Client secret do OIDC (**fictício, apenas em `.env.example`**) |
| `Keycloak__RequireHttpsMetadata` | `false` para Keycloak local em HTTP |
| `MSSQL_SA_PASSWORD` | Senha do `sa` do SQL Server para o Compose |
| `KEYCLOAK_ADMIN` / `KEYCLOAK_ADMIN_PASSWORD` | Usuário admin do Keycloak para o Compose |

O `appsettings.json` contém apenas valores padrão não sensíveis (logging, Authority/ClientId públicos). Os segredos devem vir do `.env`, do ambiente do host, ou de um cofre de segredos de produção — nunca do controle de versão.

Localização: `?culture=pt-BR` ou `?culture=en` (também via cookie / `Accept-Language`).

## 🔑 Notas sobre autenticação

- Fluxo de código de autorização (authorization code) do OpenID Connect contra o Keycloak.
- Sessão em cookie após o login (`QueryPlus.Auth`).
- `/login` dispara o desafio OIDC; `/logout` e `/signout-callback` completam o logout nos canais front e back (proteção antiforgery para `POST /logout`).
- A SPA conversa com a API **na mesma origem** (produção) ou pelo proxy de desenvolvimento do Vite (desenvolvimento). A API exige um token antiforgery + o cabeçalho `X-CSRF-TOKEN` em requisições que alteram estado (padrão de bootstrap: `GET /api/csrf` e depois ecoar o valor via cabeçalho). Todos os endpoints da API, exceto `/api/auth/*`, `/api/health`, `/api/csrf` e `/login`, exigem autenticação; autenticação ausente ou inválida retorna JSON `401 Unauthorized` (o React intercepta e redireciona para `/login`).

### Rede em Dev Containers / Docker

O navegador nunca deve ser redirecionado para o nome DNS interno do Docker `keycloak`.

| Configuração | Finalidade |
|---------|---------|
| `Keycloak:Authority` | URL pública para o navegador (`http://localhost:8080/realms/queryplus`) |
| `Keycloak:MetadataAddress` | URL interna de discovery (`http://keycloak:8080/realms/.../.well-known/...`) |
| `Keycloak:BackchannelHost` | Reescreve as chamadas de token/JWKS do servidor de `localhost` → `keycloak` |

O Keycloak é iniciado com `KC_HOSTNAME=localhost` para que as URLs de issuer/authorize sejam alcançáveis pelo host.

```bash
docker compose down
docker compose up -d sqlserver keycloak
# ou: Dev Containers → Rebuild Container
```

Antes de qualquer ambiente que não seja de desenvolvimento: use senhas fortes e únicas, um client secret privado do Keycloak, desative ou substitua os usuários de demonstração, e nunca envie um `.env` real ou faça commit de strings de conexão de produção.

## 🔢 Chaves primárias

Todas as entidades de domínio usam chaves primárias de identidade do tipo **`int`**.

## 🌱 Dados de demonstração (automático na inicialização)

Na inicialização da aplicação, o `DemoDataSeeder`:

1. Aplica as migrations do EF Core
2. Instala as tabelas e stored procedures de demonstração a partir de `src/QueryPlus.Data/Seed/demo-objects.sql`
3. Registra categorias/procedures/parâmetros/colunas a partir de `demo-catalog.json` (idempotente)

### Destaques

| Objeto | Finalidade |
|--------|---------|
| `tb_usa_president` + SPs de listagem / paginação | Listagem de presidentes dos EUA com filtros |
| Demonstrações de paginação | `Sp_Demo_Numbers_Paged`, `Sp_Demo_Large_Result_Paged`, etc. |
| 30+ procedures `Sp_Demo_*` | FreeText, Numeric, Date, Time, DateTime, Boolean, Combo |
| Tabelas de apoio | clientes, produtos, pedidos, funcionários, … |

O entitlement de papel (role) das procedures de demonstração é **`ROLE_QUERY_EXEC`** (também funciona com `ROLE_ADMIN`, que implica todas as permissões).

Os scripts SQL também são espelhados em `docs/database/`.

## 📚 Documentação

- [Especificação do software](docs/SPECIFICATION.md)
- [Schema do banco de dados](docs/database/schema.sql)

## 📄 Licença

Este projeto está licenciado sob a [MIT License](LICENSE).
