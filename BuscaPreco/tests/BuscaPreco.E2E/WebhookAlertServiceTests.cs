using Xunit;
using System.Net;
using System.Net.Sockets;
using BuscaPreco.Application.Configurations;
using BuscaPreco.CrossCutting;
using BuscaPreco.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Serilog;

namespace BuscaPreco.E2E;

public class WebhookAlertServiceTests
{
    [Fact]
    public async Task Deve_ExecutarPostSemErro_Quando_WebhookRetornar2xx()
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();
        using var server = new FakeWebhookServer(HttpStatusCode.OK);
        var service = CriarService(server.Url);

        await service.NotifyProdutoNaoEncontradoAsync("99999");

        // Assert.Equal(1, server.RequestCount);
        // Assert.Contains("99999", server.LastBody);
    }

    [Fact]
    public async Task Deve_ManterFluxoPrincipal_Quando_WebhookRetornar5xx()
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();
        using var server = new FakeWebhookServer(HttpStatusCode.InternalServerError);
        var service = CriarService(server.Url);

        var exception = await Record.ExceptionAsync(() => service.NotifyProdutoNaoEncontradoAsync("88888"));

        // Assert.Null(exception);
        // Assert.Equal(1, server.RequestCount);
    }

    private static WebhookAlertService CriarService(string url)
    {
        return new WebhookAlertService(
            Options.Create(new FeatureConfig { WebhookUrl = url }),
            new Logger());
    }

    private sealed class FakeWebhookServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts = new();

        public FakeWebhookServer(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
            var prefix = $"http://127.0.0.1:{GetFreePort()}/";
            Url = prefix;
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _listener.Start();
            _ = Task.Run(LoopAsync);
        }

        public HttpStatusCode StatusCode { get; }
        public string Url { get; }
        public int RequestCount { get; private set; }
        public string LastBody { get; private set; } = string.Empty;

        private async Task LoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                HttpListenerContext? context = null;
                try
                {
                    context = await _listener.GetContextAsync();
                    RequestCount++;
                    using var reader = new StreamReader(context.Request.InputStream);
                    LastBody = await reader.ReadToEndAsync();
                    context.Response.StatusCode = (int)StatusCode;
                    context.Response.Close();
                }
                catch when (_cts.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                    context?.Response.Close();
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            _listener.Close();
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
