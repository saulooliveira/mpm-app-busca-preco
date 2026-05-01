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

## Gertec BuscaPreço TCP Protocol

The reference manual is `Manual_Desenvolvedor_JavaC-V1.3.0_R00.22` (Gertec). Protocol is plain ASCII over TCP, no framing delimiters.

### Handshake sequence (on every new connection)

| Step | Direction | Data |
|---|---|---|
| 1 | Server → Terminal | `#ok` |
| 2 | Terminal → Server | `#<tipo>\|<versao>` (e.g. `#tc406\|3.3.1 S`) |
| 3 | Server → Terminal | `#alwayslive` (manual p.6) |
| 4 | Terminal → Server | `#alwayslive_ok` |
| 5 | Server → Terminal | `#config02?` |
| 6 | Terminal → Server | `#config02<blob>` |
| 7 | Server → Terminal | `#paramconfig?` |
| 8 | Terminal → Server | `#paramconfig<2 bytes>` |
| 9 | Server → Terminal | `#updconfig?` |
| 10 | Terminal → Server | `#updconfig<blob>` |
| 11 | Server → Terminal | `#macaddr?` (manual p.34) |
| 12 | Terminal → Server | `#macaddr<1b-iface><1b-len+48><MAC>` |
| — | Main loop | Terminal sends barcode; server responds with price or `#nfound` |

### Main loop commands

| Message | Direction | Meaning |
|---|---|---|
| `#<barcode>` | Terminal → Server | Barcode scanned — trimmed and looked up |
| `#<desc>\|<price>` | Server → Terminal | Product found; desc ≤ 20 chars, `#` stripped from price |
| `#nfound` | Server → Terminal | Product not found |
| `#live?` | Server → Terminal | Keepalive probe (sent after 5 s idle timeout) |
| `#live` | Terminal → Server | Keepalive reply |
| `#queryprocessfailure` | Terminal → Server | Terminal did not receive a query response in time — server silently ignores, does NOT forward to `onReceive` |
| `#mesg<l1len><l1><l2len><l2><tempo><0>` | Server → Terminal | Display arbitrary text (manual p.19) |

**Keepalive timing:** `RecebeDoTerminal` has a 5 s `Socket.Select` timeout. After 2 consecutive unanswered `#live?` probes (`contLive >= 2`) the connection closes (~15 s max idle). When `#alwayslive` is accepted, the terminal maintains the connection without needing probes; the server still sends `#live?` as a safety net and the terminal should still reply with `#live`.

### Terminal model identifiers

| `tipo` | Model | Audio |
|---|---|---|
| `tc406` + firmware `3.*` | G2 S | Yes — `#playaudiowithmessage` (manual p.33) |
| `tc406` + other firmware | G2 S (older) | No |
| `tc502` | Older model | No |

### `#playaudiowithmessage` format (G2 S with audio only)

```
#playaudiowithmessage<6-char hex WAV size><duracao+48><volume+48><2-digit desc len><desc><2-digit price len><price><WAV bytes>
```
- WAV: 8 kHz, Mono, 8-bit PCM_U8, 16 KB–68 KB, duration 2–7 s
- Falls back to `SendProcPrice()` automatically for non-G2 S terminals

### Config blob encoding (`#config02`, `#reconf02`, `#updconfig`)

Length-prefixed ASCII: each field is preceded by `(char)(length + 48)`. For example, a 7-char IP `1.2.3.4` is encoded as `(char)(7+48)` + `"1.2.3.4"` = `'7' + "1.2.3.4"`. All fields use this scheme.

### `NormalizeCodigo` (in `TrayApplicationContext`)

Barcodes received from the terminal go through `.Trim('\0',' ','\r','\n').TrimStart('#')` before lookup, so `#7891000315507\0` becomes `7891000315507`.

## Git Workflow

Branch strategy: `feature/*` → `develop` → `release/*` → `master`

Commit format (Conventional Commits, in Portuguese):
```
feat(application): adiciona serviço de consulta de preço por código de barras
fix(infrastructure): corrige tratamento de timeout no listener TCP
test(e2e): implementa CT-010 validação de produto existente
```

Changelog is generated automatically by `git-cliff` (`cliff.toml`) from commit messages. Version is managed in the `VERSION` file (MAJOR.MINOR.PATCH); the CI pipeline bumps it automatically.
