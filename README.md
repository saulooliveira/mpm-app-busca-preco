# BuscaPreco

Aplicação desktop em **C#/.NET WinForms** executando no **System Tray** como backend local para terminal Gertec Busca Preço G2 E.

## Configuração centralizada em YAML

A aplicação agora lê **todas** as configurações via `config.yaml` (raiz do projeto `BuscaPreco/`) usando `NetEscapades.Configuration.Yaml` + `IOptions<T>`.

### Exemplo de `config.yaml`

```yaml
DbfConfig:
  DbfFilePath: "C:/Users/Cadastro/Documents/Cadger/CADITE.DBF"

Terminal:
  Porta: 6500
  ReconnectDelayMs: 5000

Email:
  SmtpHost: "smtp.seuprovedor.com"
  SmtpPort: 587
  EnableSsl: true
  Username: "usuario@dominio.com"
  Password: "senha-segura"
  Remetente: "buscapreco@mercadoprogressomineiro.com"
  Destinatario: "gestor@mercadoprogressomineiro.com"
  DailyReportTime: "23:55"
  LogDirectory: "logs"

Serilog:
  Using:
    - Serilog.Sinks.File
  MinimumLevel:
    Default: Information
  WriteTo:
    - Name: File
      Args:
        path: "logs/consultas-.txt"
        rollingInterval: Day
        retainedFileCountLimit: 30
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} CodigoBarras={CodigoBarras} Nome={Nome} Preco={Preco} Status={Status} {Message:lj}{NewLine}"
  Enrich:
    - FromLogContext
```

## Novas funcionalidades

### 1) Reconexão automática do terminal

- Serviço de comunicação (`Servidor`) com laço de reconexão.
- Em falha de bind/conexão, registra log e aguarda `Terminal:ReconnectDelayMs` antes de nova tentativa.
- Aplicação continua ativa no tray.

### 2) Log de auditoria estruturado

- `IBuscaPrecosService` registra toda consulta de código de barras com:
  - Data/hora
  - Código
  - Nome
  - Preço
  - Status (Encontrado/Não Cadastrado)
- Serilog grava em arquivo diário em `logs/consultas-yyyyMMdd.txt`.

### 3) Relatório diário por e-mail

- `RelatorioDiarioBackgroundService` agenda envio conforme `Email:DailyReportTime`.
- `EmailService` lê o log do dia e envia resumo automático com assunto:
  - **Relatório de Consultas Diárias - Mercado Progresso Mineiro**

## Principais classes adicionadas/alteradas

- `Application/Configurations/TerminalConfig.cs`
- `Application/Configurations/EmailConfig.cs`
- `Application/Interfaces/IEmailService.cs`
- `Application/Services/RelatorioDiarioBackgroundService.cs`
- `Infrastructure/Services/EmailService.cs`
- `Infrastructure/Scrapers/Servidor.cs`
- `Application/Services/BuscaPrecosService.cs`
- `Program.cs`

## Build

```bash
nuget restore BuscaPreco.sln
msbuild BuscaPreco.sln /p:Configuration=Release
```
