using BuscaPreco.Application.Interfaces;
using BuscaPreco.Infrastructure.Scrapers;

namespace BuscaPreco.Infrastructure.Services
{
    public class TerminalDisplayService : ITerminalDisplayService
    {
        private readonly Servidor _servidor;

        public TerminalDisplayService(Servidor servidor)
        {
            _servidor = servidor;
        }

        public void MostrarProdutoPromocional(string nome, string preco)
        {
            _servidor.BroadcastProdutoPromocional(nome, preco);
        }
    }
}
