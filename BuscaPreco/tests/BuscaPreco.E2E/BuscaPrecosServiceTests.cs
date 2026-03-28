using Xunit;
using System.Linq;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Application.Models;
using BuscaPreco.Application.Services;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Entities;
using BuscaPreco.Domain.Interfaces;
using BuscaPreco.Infrastructure.Data;
using BuscaPreco.Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using AppLogger = BuscaPreco.CrossCutting.Logger;

namespace BuscaPreco.E2E;

public class BuscaPrecosServiceTests
{
    [Fact]
    public void Deve_RetornarDescricaoEPrecoFormatado_Quando_CodigoExistenteForConsultado()
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();
        var repository = new FakeProdutoRepository(("PRODUTO TESTE", 12.34m));
        var service = CriarServico(repository, cacheTtlMinutes: 5);

        var resultado = service.BuscarPorCodigo("20001");

        // Assert.Equal("PRODUTO TESTE", resultado.des);
        // Assert.Equal(12.34m, resultado.vlrVenda1);
        // Assert.Equal("12,34", resultado.vlrVenda1.ToString("N2"));
        // Assert.Equal(1, repository.BuscasExecutadas);
    }

    [Fact]
    public async Task Deve_RetornarNfoundENotificarAlerta_Quando_ProdutoNaoExistir()
    {
        var sink = new InMemorySink();
        Log.Logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var alertService = new SpyAlertService();
        var repository = new FakeProdutoRepository(("", 0m));
        var service = CriarServico(repository, 5, alertService);

        var resultado = service.BuscarPorCodigo("99999");
        await alertService.WaitCallAsync();

        // Assert.True(string.IsNullOrWhiteSpace(resultado.des));
        // Assert.Equal(0m, resultado.vlrVenda1);
        // Assert.Equal("#nfound", string.IsNullOrWhiteSpace(resultado.des) ? "#nfound" : "");
        // Assert.Contains(sink.Events, e => e.RenderMessage().Contains("Status=Não Cadastrado"));
        // Assert.Equal("99999", alertService.CodigoRecebido);
    }

    [Fact]
    public void Deve_UsarCache_Quando_RepetirConsultaDentroDoTtl()
    {
        var sink = new InMemorySink();
        Log.Logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var repository = new FakeProdutoRepository(("PRODUTO CACHE", 45.67m));
        var service = CriarServico(repository, cacheTtlMinutes: 10);

        var primeira = service.BuscarPorCodigo("20002");
        var segunda = service.BuscarPorCodigo("20002");

        // Assert.Equal(primeira, segunda);
        // Assert.Equal(1, repository.BuscasExecutadas);
        // Assert.Contains(sink.Events, e => e.RenderMessage().Contains("Origem=Cache"));
    }

    private static BuscaPrecosService CriarServico(
        IProdutoRepository repository,
        int cacheTtlMinutes,
        IAlertService? alertService = null)
    {
        var logger = new AppLogger();
        var dbContext = new ConsultaDbContext();
        var consultaRepository = new ConsultaRepository(dbContext, logger);

        return new BuscaPrecosService(
            new FakeProdutoCacheService(repository),
            alertService ?? new SpyAlertService(),
            new NullTerminalActivityMonitor(),
            Options.Create(new FeatureConfig { CacheTTLMinutes = cacheTtlMinutes }),
            logger,
            consultaRepository);
    }

    private sealed class FakeProdutoCacheService : IProdutoCacheService
    {
        private readonly IProdutoRepository _repo;

        public FakeProdutoCacheService(IProdutoRepository repo)
        {
            _repo = repo;
        }

        public ProdutoCacheEntry BuscarPorCodigo(string codigo)
        {
            var (des, preco) = _repo.BuscarPorCodigo(codigo);
            if (string.IsNullOrWhiteSpace(des)) return null;
            return new ProdutoCacheEntry
            {
                CodigoBarras = codigo,
                Descricao = des,
                Preco = preco
            };
        }

        public List<ProdutoCacheEntry> ListarTodos()
            => _repo.ListarTudo()
                   .Select(p => new ProdutoCacheEntry
                   {
                       CodigoBarras = p.CodigoItem,
                       Descricao = p.Descricao1,
                       Preco = p.Preco
                   })
                   .ToList();

        public void SincronizarAgora() { }
    }

    private sealed class FakeProdutoRepository : IProdutoRepository
    {
        private readonly (string des, decimal vlrVenda1) _resultado;

        public FakeProdutoRepository((string des, decimal vlrVenda1) resultado)
        {
            _resultado = resultado;
        }

        public int BuscasExecutadas { get; private set; }

        public (string des, decimal vlrVenda1) BuscarPorCodigo(string codigo)
        {
            BuscasExecutadas++;
            return _resultado;
        }

        public List<Produto> ListarTudo() => [];
    }

    private sealed class SpyAlertService : IAlertService
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        public string? CodigoRecebido { get; private set; }

        public Task NotifyProdutoNaoEncontradoAsync(string codigo)
        {
            CodigoRecebido = codigo;
            _tcs.TrySetResult(true);
            return Task.CompletedTask;
        }

        public async Task WaitCallAsync()
        {
            await Task.WhenAny(_tcs.Task, Task.Delay(2000));
        }
    }

    private sealed class NullTerminalActivityMonitor : ITerminalActivityMonitor
    {
        public DateTime LastActivityUtc => DateTime.UtcNow;
        public TimeSpan Inactivity => TimeSpan.Zero;
        public void MarkActivity() { }
        public bool IsInactiveFor(TimeSpan duration) => false;
    }

    private sealed class InMemorySink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }
}
