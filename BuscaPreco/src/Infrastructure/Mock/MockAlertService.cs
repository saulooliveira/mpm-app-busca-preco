using System.Threading.Tasks;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;

namespace BuscaPreco.Infrastructure.Mock
{
    public class MockAlertService : IAlertService
    {
        private readonly Logger _logger;
        public MockAlertService(Logger logger) => _logger = logger;

        public Task NotifyProdutoNaoEncontradoAsync(string codigoBarras)
        {
            _logger.Info("[MOCK] AlertService: produto não encontrado {Codigo}", codigoBarras);
            return Task.CompletedTask;
        }
    }
}
