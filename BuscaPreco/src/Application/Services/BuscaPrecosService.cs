using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Entities;
using BuscaPreco.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Application.Services
{
    public class BuscaPrecosService : IBuscaPrecosService
    {
        private sealed class CacheItem
        {
            public Produto Produto { get; set; }
            public DateTime ExpiraEmUtc { get; set; }
        }

        private readonly IProdutoRepository _produtoRepository;
        private readonly IAlertService _alertService;
        private readonly IProdutoCacheTracker _cacheTracker;
        private readonly ITerminalActivityMonitor _terminalActivityMonitor;
        private readonly FeatureConfig _featureConfig;
        private readonly Logger _logger;
        private readonly ConcurrentDictionary<string, CacheItem> _cachePorCodigo;

        public BuscaPrecosService(
            IProdutoRepository produtoRepository,
            IAlertService alertService,
            IProdutoCacheTracker cacheTracker,
            ITerminalActivityMonitor terminalActivityMonitor,
            IOptions<FeatureConfig> featureOptions,
            Logger logger)
        {
            _produtoRepository = produtoRepository;
            _alertService = alertService;
            _cacheTracker = cacheTracker;
            _terminalActivityMonitor = terminalActivityMonitor;
            _featureConfig = featureOptions.Value;
            _logger = logger;
            _cachePorCodigo = new ConcurrentDictionary<string, CacheItem>();
        }

        public (string des, decimal vlrVenda1) BuscarPorCodigo(string codigo)
        {
            _terminalActivityMonitor.MarkActivity();

            var produtoCacheado = BuscarDoCache(codigo);
            if (produtoCacheado != null)
            {
                LogAuditoria(codigo, produtoCacheado, true);
                return (produtoCacheado.des, produtoCacheado.vlrVenda1);
            }

            var resultado = _produtoRepository.BuscarPorCodigo(codigo);
            var encontrado = !string.IsNullOrWhiteSpace(resultado.des);

            if (encontrado)
            {
                var produto = new Produto { cod = codigo, des = resultado.des, vlrVenda1 = resultado.vlrVenda1 };
                ArmazenarNoCache(codigo, produto);
            }
            else
            {
                _ = Task.Run(() => _alertService.NotifyProdutoNaoEncontradoAsync(codigo));
            }

            LogAuditoria(codigo, new Produto { cod = codigo, des = resultado.des, vlrVenda1 = resultado.vlrVenda1 }, false);
            return resultado;
        }

        public List<Produto> ListarTudo()
        {
            return _produtoRepository.ListarTudo();
        }

        private Produto BuscarDoCache(string codigo)
        {
            if (!_cachePorCodigo.TryGetValue(codigo, out var cacheItem))
            {
                return null;
            }

            if (cacheItem.ExpiraEmUtc <= DateTime.UtcNow)
            {
                _cachePorCodigo.TryRemove(codigo, out _);
                _cacheTracker.Remove(codigo);
                return null;
            }

            return cacheItem.Produto;
        }

        private void ArmazenarNoCache(string codigo, Produto produto)
        {
            var ttl = Math.Max(_featureConfig.CacheTTLMinutes, 1);
            var cacheItem = new CacheItem
            {
                Produto = produto,
                ExpiraEmUtc = DateTime.UtcNow.AddMinutes(ttl)
            };

            _cachePorCodigo.AddOrUpdate(codigo, cacheItem, (_, __) => cacheItem);
            _cacheTracker.Track(codigo, produto);
        }

        private void LogAuditoria(string codigo, Produto produto, bool veioDoCache)
        {
            var encontrado = !string.IsNullOrWhiteSpace(produto.des);

            _logger.Info(
                "AuditoriaConsulta DataHora={DataHora} CodigoBarras={CodigoBarras} Nome={Nome} Preco={Preco} Status={Status} Origem={Origem}",
                DateTime.Now,
                codigo,
                encontrado ? produto.des : "",
                encontrado ? produto.vlrVenda1.ToString("N2") : "0,00",
                encontrado ? "Encontrado" : "Não Cadastrado",
                veioDoCache ? "Cache" : "Banco");
        }
    }
}
