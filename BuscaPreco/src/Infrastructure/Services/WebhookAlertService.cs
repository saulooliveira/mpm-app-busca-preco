using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
            if (!TryGetValidWebhookUri(_featureConfig.WebhookUrl, out var webhookUri))
            {
                return;
            }

            var codigoNormalizado = Validators.SomenteDigitos(codigoBarras);
            if (string.IsNullOrWhiteSpace(codigoNormalizado))
            {
                return;
            }

            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    mensagem = $"Atenção: Produto código {codigoNormalizado} bipado no terminal, mas não encontrado no sistema."
                });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using (var response = await HttpClient.PostAsync(webhookUri, content).ConfigureAwait(false))
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

        private bool TryGetValidWebhookUri(string url, out Uri webhookUri)
        {
            webhookUri = null;

            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsedUri))
            {
                _logger.Warning("WebhookUrl inválida, envio ignorado.");
                return false;
            }

            var isHttpScheme = parsedUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                               parsedUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            if (!isHttpScheme)
            {
                _logger.Warning("WebhookUrl deve usar HTTP ou HTTPS, envio ignorado.");
                return false;
            }

            webhookUri = parsedUri;
            return true;
        }
    }
}
