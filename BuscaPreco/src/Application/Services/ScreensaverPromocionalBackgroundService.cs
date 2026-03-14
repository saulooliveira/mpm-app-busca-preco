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

        public ScreensaverPromocionalBackgroundService(
            IProdutoCacheTracker cacheTracker,
            ITerminalActivityMonitor terminalActivityMonitor,
            ITerminalDisplayService terminalDisplayService,
            IOptions<FeatureConfig> featureOptions,
            Logger logger)
        {
            _cacheTracker = cacheTracker;
            _terminalActivityMonitor = terminalActivityMonitor;
            _terminalDisplayService = terminalDisplayService;
            _featureConfig = featureOptions.Value;
            _logger = logger;
            _random = new Random();
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
                            var produto = produtos.ElementAt(_random.Next(produtos.Count));
                            var nome = (produto.des ?? string.Empty);
                            if (nome.Length > 20)
                            {
                                nome = nome.Substring(0, 20);
                            }

                            var preco = produto.vlrVenda1.ToString("N2", new CultureInfo("pt-BR"));
                            _terminalDisplayService.MostrarProdutoPromocional(nome, preco);
                            _logger.Info("Screensaver exibiu produto promocional: {Produto} {Preco}", nome, preco);
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
