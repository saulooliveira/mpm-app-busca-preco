using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Application.Models;
using BuscaPreco.Application.Services;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Entities;
using BuscaPreco.Domain.Interfaces;
using BuscaPreco.Infrastructure.Data;
using BuscaPreco.Infrastructure.Repositories;
using BuscaPreco.Infrastructure.Scrapers;
using Microsoft.Extensions.Options;
using Serilog;

namespace BuscaPreco.E2E;

/// <summary>
/// Configura um servidor BuscaPreço completo para testes E2E.
/// Gerencia ciclo de vida: inicia o servidor no constructor e o para no DisposeAsync.
/// </summary>
public sealed class ServidorDeTestesContext : IAsyncDisposable
{
    public int Porta { get; private set; }
    public IBuscaPrecosService BuscaPrecosService { get; private set; } = null!;
    private Servidor _servidor = null!;

    private ServidorDeTestesContext() { }

    /// <summary>
    /// Cria e inicia um servidor de teste com banco DBF da fixture.
    /// O servidor está pronto para aceitar conexões ao retornar.
    /// </summary>
    public static async Task<ServidorDeTestesContext> CriarAsync()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var logger = new Logger();
        var dbfPath = MaterializarFixtureDbf();
        var dbfDatabase = new DbfDatabase(dbfPath, logger);
        var repository = new ProdutoRepository(dbfDatabase);

        var consultaDb = new ConsultaDbContext();
        var consultaRepository = new ConsultaRepository(consultaDb, logger);

        var buscaPrecosService = new BuscaPrecosService(
            new FakeProdutoCacheService(repository),
            new NullAlertService(),
            new NullTerminalActivityMonitor(),
            Options.Create(new FeatureConfig { CacheTTLMinutes = 10 }),
            logger,
            consultaRepository);

        var porta = ObterPortaLivre();
        var servidor = new Servidor(
            Options.Create(new TerminalConfig { Porta = porta, ReconnectDelayMs = 100 }),
            logger);

        // Replica a lógica de TrayService.OnReceiveData
        servidor.onReceive += (sender, codigoRecebido) =>
        {
            try
            {
                var codigo = (codigoRecebido ?? string.Empty)
                    .Trim('\0', ' ', '\r', '\n')
                    .TrimStart('#');

                if (string.IsNullOrWhiteSpace(codigo)) return;

                var terminal = sender as BuscaPreco.Infrastructure.Scrapers.Terminal;
                if (terminal is null) return;

                var (descricao, preco) = buscaPrecosService.BuscarPorCodigo(codigo);

                if (string.IsNullOrWhiteSpace(descricao))
                {
                    terminal.SendProdNFound();
                    return;
                }

                terminal.SendProcPrice(
                    descricao,
                    preco.ToString("0.00", CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro no handler onReceive");
            }
        };

        await servidor.StartAsync();

        return new ServidorDeTestesContext
        {
            Porta = porta,
            BuscaPrecosService = buscaPrecosService,
            _servidor = servidor
        };
    }

    // ── Utilitários ────────────────────────────────────────────────────────────

    internal static string MaterializarFixtureDbf()
    {
        var fixturesDir = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        Directory.CreateDirectory(fixturesDir);

        var base64Path = Path.Combine(fixturesDir, "produtos.dbf.base64.txt");
        var dbfPath = Path.Combine(fixturesDir, "produtos.dbf");

        var base64 = File.ReadAllText(base64Path).Trim();
        File.WriteAllBytes(dbfPath, Convert.FromBase64String(base64));
        return dbfPath;
    }

    internal static int ObterPortaLivre()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var porta = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return porta;
    }

    public async ValueTask DisposeAsync()
    {
        _servidor.Stop();
        await Task.Delay(50); // dá tempo ao servidor para liberar a porta
        Log.CloseAndFlush();
    }

    // ── Fakes de suporte ───────────────────────────────────────────────────────

    private sealed class FakeProdutoCacheService : IProdutoCacheService
    {
        private readonly IProdutoRepository _repo;
        public FakeProdutoCacheService(IProdutoRepository repo) => _repo = repo;

        public ProdutoCacheEntry? BuscarPorCodigo(string codigoBarras)
        {
            var (des, preco) = _repo.BuscarPorCodigo(codigoBarras);
            if (string.IsNullOrWhiteSpace(des)) return null;
            return new ProdutoCacheEntry { CodigoBarras = codigoBarras, Descricao = des, Preco = preco };
        }

        public List<ProdutoCacheEntry> ListarTodos()
            => _repo.ListarTudo().Select(p => new ProdutoCacheEntry
            {
                CodigoBarras = p.CodigoItem,
                Descricao = p.Descricao1,
                Preco = p.Preco,
                Unidade = p.Unidade
            }).ToList();

        public void SincronizarAgora() { }
    }

    private sealed class NullAlertService : IAlertService
    {
        public Task NotifyProdutoNaoEncontradoAsync(string codigo) => Task.CompletedTask;
    }

    private sealed class NullTerminalActivityMonitor : ITerminalActivityMonitor
    {
        public DateTime LastActivityUtc => DateTime.UtcNow;
        public TimeSpan Inactivity => TimeSpan.Zero;
        public void MarkActivity() { }
        public bool IsInactiveFor(TimeSpan duration) => false;
    }
}
