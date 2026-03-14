using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Infrastructure.Services
{
    public class WebhookAlertService : IAlertService
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly FeatureConfig _featureConfig;
        private readonly Logger _logger;

        public WebhookAlertService(IOptions<FeatureConfig> featureOptions, Logger logger)
        {
            _featureConfig = featureOptions.Value;
            _logger = logger;
        }

        public async Task NotifyProdutoNaoEncontradoAsync(string codigoBarras)
        {
            if (string.IsNullOrWhiteSpace(_featureConfig.WebhookUrl))
            {
                return;
            }

            try
            {
                var payload = "{\"mensagem\":\"Atenção: Produto código " + codigoBarras + " bipado no terminal, mas não encontrado no sistema.\"}";
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using (var response = await HttpClient.PostAsync(_featureConfig.WebhookUrl, content).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.Warning("Webhook retornou status inesperado: {StatusCode}", (int)response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("Erro ao enviar webhook de produto não cadastrado: {Erro}", ex.Message);
            }
        }
    }
}
