using Xunit;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BuscaPreco.Application.Configurations;
using BuscaPreco.CrossCutting;
using BuscaPreco.Infrastructure.Scrapers;
using BuscaPreco.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Serilog;

namespace BuscaPreco.E2E;

public class InfrastructureAndSecurityTests
{
    [Fact]
    public async Task Deve_EnviarResumoConsistentePorEmail_Quando_LogDiarioExistir()
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();

        var referenceDate = new DateTime(2026, 3, 20);
        var baseDir = AppContext.BaseDirectory;
        var logDirectoryName = $"logs-{Guid.NewGuid():N}";
        var fullLogDirectory = Path.Combine(baseDir, logDirectoryName);
        Directory.CreateDirectory(fullLogDirectory);

        var logPath = Path.Combine(fullLogDirectory, $"consultas-{referenceDate:yyyyMMdd}.txt");
        await File.WriteAllLinesAsync(logPath,
        [
            "Status=Encontrado Codigo=1",
            "Status=Encontrado Codigo=2",
            "Status=Não Cadastrado Codigo=3",
        ]);

        using var smtpServer = new FakeSmtpServer();

        var config = new EmailConfig
        {
            SmtpHost = "127.0.0.1",
            SmtpPort = smtpServer.Port,
            EnableSsl = false,
            Username = "u",
            Password = "p",
            Remetente = "origem@test.local",
            Destinatario = "destino@test.local",
            LogDirectory = logDirectoryName
        };

        var service = new EmailService(Options.Create(config), new Logger());

        await service.SendDailyReportAsync(referenceDate, CancellationToken.None);

        var body = await smtpServer.WaitForBodyAsync();
        Assert.Contains("Total de consultas: 3", body);
        Assert.Contains("Encontrados: 2", body);
        Assert.Contains("Não cadastrados: 1", body);
    }

    [Fact]
    public async Task Deve_LiberarPortaSemExcecao_Quando_ServidorForEncerrado()
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();
        var porta = GetFreePort();
        var servidor = new Servidor(
            Options.Create(new TerminalConfig { Porta = porta, ReconnectDelayMs = 100 }),
            new Logger());

        servidor.Start();
        await Task.Delay(300);
        servidor.Stop();
        await Task.Delay(300);

        using var probe = new TcpListener(IPAddress.Loopback, porta);
        var exception = Record.Exception(probe.Start);
        probe.Stop();

        Assert.Null(exception);
    }

    [Fact]
    [Trait("Security", "Risk")]
    public void Deve_EvidenciarRiscoDeTransporteSemAutenticacao_Quando_AvaliarImplementacaoSocketAtual()
    {
        var servidorPath = Path.Combine("..", "..", "..", "..", "src", "Infrastructure", "Scrapers", "Servidor.cs");
        var terminalPath = Path.Combine("..", "..", "..", "..", "src", "Infrastructure", "Scrapers", "Terminal.cs");

        var servidorCode = File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, servidorPath)));
        var terminalCode = File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, terminalPath)));

        Assert.Contains("IPAddress.Any", servidorCode);
        Assert.DoesNotContain("Authenticate", terminalCode);
        Assert.DoesNotContain("Authorization", terminalCode);
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class FakeSmtpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loopTask;
        private readonly TaskCompletionSource<string> _bodyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public FakeSmtpServer()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _loopTask = Task.Run(() => AcceptLoopAsync(_cts.Token));
        }

        public int Port { get; }

        public async Task<string> WaitForBodyAsync()
        {
            var completed = await Task.WhenAny(_bodyTcs.Task, Task.Delay(5000));
            return completed == _bodyTcs.Task ? _bodyTcs.Task.Result : string.Empty;
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(ct);
                    _ = Task.Run(() => HandleClientAsync(client, ct), ct);
                }
            }
            catch when (ct.IsCancellationRequested)
            {
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII))
            using (var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true })
            {
                await writer.WriteLineAsync("220 localhost test smtp");
                var dataBuilder = new StringBuilder();
                var inData = false;

                while (!ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line is null)
                    {
                        break;
                    }

                    if (inData)
                    {
                        if (line == ".")
                        {
                            inData = false;
                            _bodyTcs.TrySetResult(dataBuilder.ToString());
                            await writer.WriteLineAsync("250 OK");
                            continue;
                        }

                        dataBuilder.AppendLine(line);
                        continue;
                    }

                    if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
                    {
                        inData = true;
                        await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                    }
                    else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("221 Bye");
                        break;
                    }
                    else
                    {
                        await writer.WriteLineAsync("250 OK");
                    }
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            try
            {
                _loopTask.Wait(500);
            }
            catch
            {
                // Ignorado no encerramento de fixture.
            }
        }
    }
}
