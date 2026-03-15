using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Application.Services;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Entities;
using BuscaPreco.Infrastructure.Data;
using BuscaPreco.Infrastructure.Repositories;
using BuscaPreco.Infrastructure.Scrapers;
using Microsoft.Extensions.Options;
using Serilog;
using Xunit;

namespace BuscaPreco.E2E;

public class BuscaPrecoSocketE2ETests
{
    [Fact]
    public async Task Deve_ConsultarProdutoNoDbf_E_RetornarRespostaCorretaViaSocket()
    {
        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().CreateLogger();

        var logger = new Logger();
        var fixtureDbfPath = MaterializarFixtureDbf();
        var dbfDatabase = new DbfDatabase(fixtureDbfPath, logger);
        var repository = new ProdutoRepository(dbfDatabase);

        var buscaPrecosService = new BuscaPrecosService(
            repository,
            new NullAlertService(),
            new NullProdutoCacheTracker(),
            new NullTerminalActivityMonitor(),
            Options.Create(new FeatureConfig { CacheTTLMinutes = 10 }),
            logger);

        var porta = GetFreePort();
        var servidor = new Servidor(
            Options.Create(new TerminalConfig { Porta = porta, ReconnectDelayMs = 100 }),
            logger);

        servidor.onReceive += (_, comandoRecebido) =>
        {
            var resultado = buscaPrecosService.BuscarPorCodigo(comandoRecebido);
            var metodo = _.GetType().GetMethod(!string.IsNullOrWhiteSpace(resultado.des) ? "SendProcPrice" : "SendProdNFound", BindingFlags.Instance | BindingFlags.Public);

            if (metodo is null)
            {
                throw new InvalidOperationException("Não foi possível localizar método de resposta no terminal.");
            }

            if (!string.IsNullOrWhiteSpace(resultado.des))
            {
                metodo.Invoke(_, [resultado.des, resultado.vlrVenda1.ToString("N2", new CultureInfo("pt-BR"))]);
            }
            else
            {
                metodo.Invoke(_, null);
            }
        };

        servidor.startServer();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, porta);
        using var stream = client.GetStream();

        Assert.Contains("#ok", await ReadAsciiAsync(stream));
        await WriteAsciiAsync(stream, "#TM|1.0");

        Assert.Contains("#config02?", await ReadAsciiAsync(stream));
        await WriteAsciiAsync(stream, BuildConfigResponse());

        Assert.Contains("#paramconfig?", await ReadAsciiAsync(stream));
        await WriteAsciiAsync(stream, BuildParamResponse());

        Assert.Contains("#updconfig?", await ReadAsciiAsync(stream));
        await WriteAsciiAsync(stream, BuildUpdateResponse());

        await Task.Delay(200);
        await WriteAsciiAsync(stream, "20001");

        var resposta = await ReadAsciiAsync(stream, timeoutMs: 5000);
        Assert.Equal("#PRODUTO TESTE E2E|12,34", resposta);

#pragma warning disable CS0612
        servidor.stopServer();
#pragma warning restore CS0612
        Log.CloseAndFlush();
    }


    private static string BuildConfigResponse()
    {
        return "#reconf02" + "00000000";
    }

    private static string BuildParamResponse()
    {
        return "#rparamconfig" + "00";
    }

    private static string BuildUpdateResponse()
    {
        return "#rupdconfig" + "000000";
    }

    private static async Task<string> ReadAsciiAsync(NetworkStream stream, int timeoutMs = 3000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        var buffer = new byte[512];
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token);
        return Encoding.ASCII.GetString(buffer, 0, bytesRead).TrimEnd('\0');
    }

    private static Task WriteAsciiAsync(NetworkStream stream, string payload)
    {
        var bytes = Encoding.ASCII.GetBytes(payload);
        return stream.WriteAsync(bytes, 0, bytes.Length);
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string MaterializarFixtureDbf()
    {
        var fixturesDir = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        Directory.CreateDirectory(fixturesDir);

        var base64Path = Path.Combine(fixturesDir, "produtos.dbf.base64.txt");
        var dbfPath = Path.Combine(fixturesDir, "produtos.dbf");

        var base64 = File.ReadAllText(base64Path).Trim();
        var bytes = Convert.FromBase64String(base64);
        File.WriteAllBytes(dbfPath, bytes);

        return dbfPath;
    }

    private sealed class NullAlertService : IAlertService
    {
        public Task NotifyProdutoNaoEncontradoAsync(string codigo) => Task.CompletedTask;
    }

    private sealed class NullProdutoCacheTracker : IProdutoCacheTracker
    {
        public void Remove(string codigo) { }
        public IReadOnlyCollection<Produto> SnapshotProdutos() => Array.Empty<Produto>();
        public void Track(string codigo, Produto produto) { }
    }

    private sealed class NullTerminalActivityMonitor : ITerminalActivityMonitor
    {
        public DateTime LastActivityUtc => DateTime.UtcNow;
        public TimeSpan Inactivity => TimeSpan.Zero;
        public void MarkActivity() { }
        public bool IsInactiveFor(TimeSpan duration) => false;
    }
}
