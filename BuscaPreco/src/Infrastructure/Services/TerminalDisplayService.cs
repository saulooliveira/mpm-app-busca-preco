using BuscaPreco.Application.Interfaces;
using BuscaPreco.Infrastructure.Terminal;

namespace BuscaPreco.Infrastructure.Services
{
    public class TerminalDisplayService : ITerminalDisplayService
    {
        private readonly Servidor _servidor;

        public TerminalDisplayService(Servidor servidor)
        {
            _servidor = servidor;
        }

        public void MostrarProdutoPromocional(string linha1, string linha2, int tempoSegundos)
        {
            _servidor.BroadcastMesg(linha1, linha2, tempoSegundos);
        }
    }
}
