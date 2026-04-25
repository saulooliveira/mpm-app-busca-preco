# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**BuscaPreco** is a .NET 8 Windows desktop application (WinForms + system tray) that acts as a backend for physical price-checking terminals (Gertec Busca Preço G2). It listens on a TCP socket for barcode queries from hardware terminals, looks up product prices from legacy DBF databases, and returns formatted results. All queries are audited via SQLite.

Target runtime: **Windows x64** (`net8.0-windows`).

## Commands

```powershell
# Restore dependencies
dotnet restore BuscaPreco.sln

# Build
dotnet build BuscaPreco.sln -c Release

# Run all tests
dotnet test BuscaPreco.sln --configuration Release

# Run a single test project
dotnet test BuscaPreco/tests/BuscaPreco.E2E/BuscaPreco.E2E.csproj --configuration Release

# Run a single test by name
dotnet test BuscaPreco/tests/BuscaPreco.E2E/BuscaPreco.E2E.csproj --filter "FullyQualifiedName~Deve_RetornarDescricaoEPrecoFormatado"

# Publish self-contained Windows executable
dotnet publish BuscaPreco/BuscaPreco.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Architecture

The project follows Clean Architecture with five layers under `BuscaPreco/src/`:

```
Domain/          → Produto entity, IProdutoRepository interface (no framework deps)
Application/     → Use cases, service interfaces, config classes, BuscaPrecosService
Infrastructure/  → DBF access, SQLite, TCP server, HTTP webhook, email, audio
Presentation/    → WinForms (TrayApplicationContext, ConfiguracaoForm, RelatorioForm)
CrossCutting/    → Logger (Serilog wrapper), Validators
```

**Request flow for a barcode lookup:**

1. `Infrastructure/Scrapers/Servidor.cs` — TCP socket listener, one `Terminal` instance per connection
2. `Terminal.cs` reads the barcode and calls `BuscaPrecosService.BuscarPorCodigo()`
3. `Application/Services/BuscaPrecosService` checks `ProdutoCacheService` (L1: `ConcurrentDictionary`, L2: SQLite)
4. Cache miss → `ProdutoRepository` reads the DBF file via `dBASE.NET`
5. `ConsultaRepository` writes an audit record to SQLite (fire-and-forget)
6. Product not found → `WebhookAlertService` sends an async HTTP POST
7. `Terminal` formats and sends the response string back to the hardware device

**Background services (hosted, registered in DI):**
- `RelatorioDiarioBackgroundService` — sends a daily SMTP report at a configured time
- `ScreensaverPromocionalBackgroundService` — rotates promotional content on idle terminals

**DBF cache invalidation:** `DbfFileManager` watches the source DBF with `FileSystemWatcher` (3 s debounce). On change it copies the file to the app directory and invalidates the SQLite L2 cache.

## Configuration

Copy `BuscaPreco/config.example.yaml` → `BuscaPreco/config.yaml` (never commit real credentials). The app reads YAML via `NetEscapades.Configuration.Yaml` and falls back to `appsettings.json`. Config is bound to typed classes in `Application/Configurations/`:

| Class | Section | Key settings |
|---|---|---|
| `DbfConfig` | `DbfConfig` | `DbfFilePath` |
| `TerminalConfig` | `Terminal` | `Porta` (TCP port), `ReconnectDelayMs` |
| `FeatureConfig` | `Features` | `CacheTTLMinutes`, `WebhookUrl`, `IdleTimeoutSeconds` |
| `EmailConfig` | `Email` | SMTP credentials, `DailyReportTime`, `LogDirectory` |
| `AudioConfig` | `AudioConfig` | `WavFilePath`, `Volume` (0–3), `DuracaoSegundos` |
| `ProdutosFixadosConfig` | `ProdutosFixados` | `Codigos` (pinned product list) |

## Key Conventions

**Naming:**
- Interfaces: `I`-prefix (`IBuscaPrecosService`, `IProdutoRepository`)
- Config classes: `*Config` suffix
- Background services: `*BackgroundService` suffix
- Test names: `Deve_<Behavior>_Quando_<Condition>()` (Portuguese)

**Dependency injection:** All services registered in `Program.cs` using `Microsoft.Extensions.DependencyInjection`. Infrastructure implements Application interfaces; Domain has no infrastructure dependencies.

**Thread safety:** `ProdutoCacheService` uses `ConcurrentDictionary` for L1. `TerminalActivityMonitor` uses `Interlocked`. Background services use `async`/`await`. The audit `ConsultaRepository` swallows exceptions intentionally (logged, never thrown) to avoid blocking the main lookup path.

**Logging:** Use the `CrossCutting.Logger` wrapper (thin Serilog abstraction), not `ILogger<T>` directly. Structured with context enrichment; rolling file output configured in `config.yaml` under `Serilog`.

## Testing

Tests live in `BuscaPreco/tests/BuscaPreco.E2E/` (xUnit). Test doubles used:
- `FakeProdutoRepository` — in-memory product store
- `FakeProdutoCacheService` — bypass cache
- `SpyAlertService` — captures webhook calls, exposes `WaitCallAsync()` for async assertions
- `InMemorySink` — captures Serilog log events for assertion

The E2E tests include real TCP socket tests (`BuscaPrecoSocketE2ETests`) and cache invalidation tests (`DbfDatabaseCacheInvalidationTests`). Many assertions in existing tests are commented out pending completion — check `PROMPT_GERACAO_TESTES_CT_PRIORITARIOS.md` for the 10 critical scenarios to implement.

## Git Workflow

Branch strategy: `feature/*` → `develop` → `release/*` → `master`

Commit format (Conventional Commits, in Portuguese):
```
feat(application): adiciona serviço de consulta de preço por código de barras
fix(infrastructure): corrige tratamento de timeout no listener TCP
test(e2e): implementa CT-010 validação de produto existente
```

Changelog is generated automatically by `git-cliff` (`cliff.toml`) from commit messages. Version is managed in the `VERSION` file (MAJOR.MINOR.PATCH); the CI pipeline bumps it automatically.
