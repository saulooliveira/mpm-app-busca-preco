using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;

namespace BuscaPreco.Infrastructure.Mock
{
    public class MockTerminalDisplayService : ITerminalDisplayService
    {
        private readonly Logger _logger;
        public MockTerminalDisplayService(Logger logger) => _logger = logger;

        public void MostrarProdutoPromocional(string linha1, string linha2, int tempoSegundos)
        {
            _logger.Info("[MOCK] TerminalDisplay: '{L1}' / '{L2}' por {Tempo}s", linha1, linha2, tempoSegundos);
        }
    }
}
