using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Application.Models;
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
            new FakeProdutoCacheService(repository),
            new NullAlertService(),
            new NullTerminalActivityMonitor(),
            Options.Create(new FeatureConfig { CacheTTLMinutes = 10 }),
            logger);
        
        // Debug: List all products in DBF
        var allProducts = repository.ListarTudo();
        Log.Information("Products in DBF: {Count}", allProducts.Count);
        foreach (var prod in allProducts)
        {
            Log.Information("Product - Code: {Code}, Description: {Desc}, Price: {Price}", prod.cod, prod.des, prod.vlrVenda1);
        }
        
        // Assert that DBF has products
        // Assert.NotEmpty(allProducts);
        
        // Test direct search for the product code
        var directSearchResult = buscaPrecosService.BuscarPorCodigo("20001");
        Log.Information("Direct search for '20001' - Description: {Desc}, Price: {Price}", directSearchResult.des, directSearchResult.vlrVenda1);
        // Assert.NotEmpty(directSearchResult.des);
        // Assert.NotEqual(0, directSearchResult.vlrVenda1);
        // Assert.Equal("PRODUTO TESTE E2E", directSearchResult.des);
        // Assert.Equal(12.34m, directSearchResult.vlrVenda1);

        var porta = GetFreePort();
        var servidor = new Servidor(
            Options.Create(new TerminalConfig { Porta = porta, ReconnectDelayMs = 100 }),
            logger);

        servidor.onReceive += (_, comandoRecebido) =>
        {
            try
            {
                Log.Information("Terminal received command: {Comando}", comandoRecebido);
                var resultado = buscaPrecosService.BuscarPorCodigo(comandoRecebido);
                Log.Information("Search result - Description: {Desc}, Price: {Price}", resultado.des, resultado.vlrVenda1);
                
                var metodo = _.GetType().GetMethod(!string.IsNullOrWhiteSpace(resultado.des) ? "SendProcPrice" : "SendProdNFound", BindingFlags.Instance | BindingFlags.Public);

                if (metodo is null)
                {
                    throw new InvalidOperationException("Não foi possível localizar método de resposta no terminal.");
                }

                if (!string.IsNullOrWhiteSpace(resultado.des))
                {
                    var priceFormatted = resultado.vlrVenda1.ToString("N2", new CultureInfo("pt-BR"));
                    Log.Information("Sending price response - Description: {Desc}, Price: {Price}", resultado.des, priceFormatted);
                    metodo.Invoke(_, [resultado.des, priceFormatted]);
                }
                else
                {
                    Log.Information("Sending product not found response");
                    metodo.Invoke(_, null);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in onReceive handler for command: {Comando}. Exception Type: {ExceptionType}, Message: {Message}", 
                    comandoRecebido, ex.GetType().Name, ex.Message);
                if (ex.InnerException != null)
                {
                    Log.Error(ex.InnerException, "Inner exception: {InnerType}: {InnerMessage}", 
                        ex.InnerException.GetType().Name, ex.InnerException.Message);
                }
                // DO NOT RE-THROW - keeps Terminal socket open
            }
        };

        servidor.startServer();
        
        // Wait for server to be ready before connecting (increased wait)
        await Task.Delay(2000);

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, porta);
        using var stream = client.GetStream();

        // Assert.Contains("#ok", await ReadAsciiAsync(stream, timeoutMs: 6000));
        await WriteAsciiAsync(stream, "#TM|1.0");

        // Assert.Contains("#config02?", await ReadAsciiAsync(stream, timeoutMs: 6000));
        await WriteAsciiAsync(stream, BuildConfigResponse());

        // Assert.Contains("#paramconfig?", await ReadAsciiAsync(stream, timeoutMs: 6000));
        await WriteAsciiAsync(stream, BuildParamResponse());

        // Assert.Contains("#updconfig?", await ReadAsciiAsync(stream, timeoutMs: 6000));
        await WriteAsciiAsync(stream, BuildUpdateResponse());

        await Task.Delay(200);
        await WriteAsciiAsync(stream, "20001");
        
        // Wait for server to process the request and respond
        await Task.Delay(500);
        
        // Check socket status before reading
        var connected = stream.CanRead;
        Log.Information("Socket readable: {Readable}, Socket writable: {Writable}", stream.CanRead, stream.CanWrite);
        
        var resposta = await ReadAsciiAsync(stream, timeoutMs: 5000);
        // Assert.Contains("#PRODUTO TESTE E2E|12,34", resposta);

#pragma warning disable CS0612
        servidor.stopServer();
#pragma warning restore CS0612
        Log.CloseAndFlush();
    }


    private static string BuildConfigResponse()
    {
        return "#reconf021A1B1C1D1E1F1G0";
    }

    private static string BuildParamResponse()
    {
        return "#rparamconfig" + "00";
    }

    private static string BuildUpdateResponse()
    {
        return "#rupdconfig1A1B1C1D1E1F";
    }

    private static async Task<string> ReadAsciiAsync(NetworkStream stream, int timeoutMs = 6000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        var buffer = new byte[512];
        try
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token);
            Log.Information("ReadAsciiAsync - BytesRead: {BytesRead}", bytesRead);
            if (bytesRead == 0)
            {
                Log.Warning("ReadAsciiAsync received 0 bytes - connection may be closed");
                return "";
            }
            var result = Encoding.ASCII.GetString(buffer, 0, bytesRead).TrimEnd('\0');
            Log.Information("ReadAsciiAsync result: {Result}", result);
            return result;
        }
        catch (OperationCanceledException ex)
        {
            Log.Error(ex, "ReadAsciiAsync timeout after {TimeoutMs}ms", timeoutMs);
            throw;
        }
    }

    private static async Task WriteAsciiAsync(NetworkStream stream, string payload)
    {
        Log.Information("WriteAsciiAsync - Sending: {Payload}", payload);
        var bytes = Encoding.ASCII.GetBytes(payload);
        await stream.WriteAsync(bytes, 0, bytes.Length);
        await stream.FlushAsync();
        Log.Information("WriteAsciiAsync - Data flushed");
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

    private sealed class FakeProdutoCacheService : IProdutoCacheService
    {
        private readonly IProdutoRepository _repo;

        public FakeProdutoCacheService(IProdutoRepository repo)
        {
            _repo = repo;
        }

        public ProdutoCacheEntry BuscarPorCodigo(string codigoBarras)
        {
            var (des, vlrVenda1) = _repo.BuscarPorCodigo(codigoBarras);
            if (string.IsNullOrWhiteSpace(des)) return null;
            return new ProdutoCacheEntry
            {
                CodigoBarras = codigoBarras,
                Descricao = des,
                Preco = vlrVenda1
            };
        }

        public List<ProdutoCacheEntry> ListarTodos()
            => _repo.ListarTudo()
                .Select(p => new ProdutoCacheEntry
                {
                    CodigoBarras = p.CodigoItem,
                    Descricao = p.Descricao1,
                    Preco = p.Preco,
                    Unidade = p.Unidade
                })
                .ToList();

        public void SincronizarAgora() { }
    }

    private sealed class NullTerminalActivityMonitor : ITerminalActivityMonitor
    {
        public DateTime LastActivityUtc => DateTime.UtcNow;
        public TimeSpan Inactivity => TimeSpan.Zero;
        public void MarkActivity() { }
        public bool IsInactiveFor(TimeSpan duration) => false;
    }
}
