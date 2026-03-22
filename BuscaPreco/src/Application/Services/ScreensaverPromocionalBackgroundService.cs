using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Application.Services
{
    public class ScreensaverPromocionalBackgroundService : BackgroundService
    {
        private readonly IProdutoCacheTracker _cacheTracker;
        private readonly ITerminalActivityMonitor _terminalActivityMonitor;
        private readonly ITerminalDisplayService _terminalDisplayService;
        private readonly FeatureConfig _featureConfig;
        private readonly Logger _logger;
        private readonly Random _random;
        private int _pinnedIndex;
        private readonly IOptions<ProdutosFixadosConfig> _produtosFixadosOptions;

        public ScreensaverPromocionalBackgroundService(
            IProdutoCacheTracker cacheTracker,
            ITerminalActivityMonitor terminalActivityMonitor,
            ITerminalDisplayService terminalDisplayService,
            IOptions<FeatureConfig> featureOptions,
            IOptions<ProdutosFixadosConfig> produtosFixadosOptions,
            Logger logger)
        {
            _cacheTracker = cacheTracker;
            _terminalActivityMonitor = terminalActivityMonitor;
            _terminalDisplayService = terminalDisplayService;
            _featureConfig = featureOptions.Value;
            _logger = logger;
            _random = new Random();
            _produtosFixadosOptions = produtosFixadosOptions;
            _pinnedIndex = 0;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var idleTimeout = Math.Max(_featureConfig.IdleTimeoutSeconds, 1);
                    var rotation = Math.Max(_featureConfig.ScreensaverRotationSeconds, 1);

                    var idleFor = DateTime.UtcNow - _terminalActivityMonitor.LastActivityUtc;
                    if (idleFor.TotalSeconds >= idleTimeout)
                    {
                        var produtos = _cacheTracker.SnapshotProdutos();
                        if (produtos.Count > 0)
                        {
                            var pinnedCodes = _produtosFixadosOptions.Value.Codigos;
                            Domain.Entities.Produto selectedProduto = null;

                            if (pinnedCodes != null && pinnedCodes.Count > 0)
                            {
                                // Priority: try to find the next pinned product in cache
                                var produtosByCode = produtos.ToDictionary(p => p.CodigoItem);
                                for (int attempt = 0; attempt < pinnedCodes.Count; attempt++)
                                {
                                    var idx = _pinnedIndex % pinnedCodes.Count;
                                    _pinnedIndex = (_pinnedIndex + 1) % pinnedCodes.Count;
                                    if (produtosByCode.TryGetValue(pinnedCodes[idx], out var candidate))
                                    {
                                        selectedProduto = candidate;
                                        break;
                                    }
                                }
                            }

                            // Fallback to random cache product if no pinned product was resolved
                            if (selectedProduto == null)
                                selectedProduto = produtos.ElementAt(_random.Next(produtos.Count));

                            var nome = (selectedProduto.des ?? string.Empty);
                            if (nome.Length > 20) nome = nome.Substring(0, 20);

                            var preco = selectedProduto.vlrVenda1.ToString("N2", new CultureInfo("pt-BR"));
                            if (preco.Length > 20) preco = preco.Substring(0, 20);

                            // Clamp duration to 1-9 for #mesg single-digit ASCII encoding
                            int duracaoMesg = rotation > 9 ? 9 : (rotation < 1 ? 1 : rotation);

                            _terminalDisplayService.MostrarProdutoPromocional(nome, preco, duracaoMesg);
                            _logger.Info("Screensaver exibiu produto via #mesg: {Produto} {Preco} {Duração}s", nome, preco, duracaoMesg);
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(rotation), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Warning("Falha no screensaver promocional: {Erro}", ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }
    }
}
