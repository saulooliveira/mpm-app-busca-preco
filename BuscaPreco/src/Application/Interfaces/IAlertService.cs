using System.Threading.Tasks;

namespace BuscaPreco.Application.Interfaces
{
    public interface IAlertService
    {
        Task NotifyProdutoNaoEncontradoAsync(string codigoBarras);
    }
}
