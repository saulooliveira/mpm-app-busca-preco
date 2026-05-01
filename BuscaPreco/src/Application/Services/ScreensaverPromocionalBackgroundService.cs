using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using BuscaPreco.Infrastructure.Scrapers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Application.Services
{
    public class ScreensaverPromocionalBackgroundService : BackgroundService
    {
        private readonly IProdutoCacheService _produtoCacheService;
        private readonly ITerminalActivityMonitor _terminalActivityMonitor;
        private readonly Servidor _servidor;
        private readonly FeatureConfig _featureConfig;
        private readonly Logger _logger;
        private readonly Random _random;
        private int _pinnedIndex;
        private readonly IOptions<ProdutosFixadosConfig> _produtosFixadosOptions;

        public ScreensaverPromocionalBackgroundService(
            IProdutoCacheService produtoCacheService,
            ITerminalActivityMonitor terminalActivityMonitor,
            Servidor servidor,
            IOptions<FeatureConfig> featureOptions,
            IOptions<ProdutosFixadosConfig> produtosFixadosOptions,
            Logger logger)
        {
            _produtoCacheService = produtoCacheService;
            _terminalActivityMonitor = terminalActivityMonitor;
            _servidor = servidor;
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
                        var produtosCache = _produtoCacheService.ListarTodos();
                        if (produtosCache.Count > 0)
                        {
                            var produtos = produtosCache
                                .Select(p => new BuscaPreco.Domain.Entities.Produto
                                {
                                    CodigoItem = p.CodigoBarras,
                                    Descricao1 = p.Descricao,
                                    Preco = p.Preco,
                                    Unidade = p.Unidade
                                })
                                .ToList();

                            var pinnedCodes = _produtosFixadosOptions.Value.Codigos;
                            BuscaPreco.Domain.Entities.Produto selectedProduto = null;

                            if (pinnedCodes != null && pinnedCodes.Count > 0)
                            {
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

                            if (selectedProduto == null)
                                selectedProduto = produtos.ElementAt(_random.Next(produtos.Count));

                            var nome = GertecProtocol.Truncate(selectedProduto.Descricao1 ?? string.Empty);
                            var preco = GertecProtocol.Truncate(selectedProduto.Preco.ToString("N2", new CultureInfo("pt-BR")));
                            int duracaoMesg = rotation > 9 ? 9 : (rotation < 1 ? 1 : rotation);

                            _servidor.BroadcastMesg(nome, preco, duracaoMesg);
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
