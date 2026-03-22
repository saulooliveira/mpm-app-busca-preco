using System.Collections.Generic;
using BuscaPreco.Application.Models;

namespace BuscaPreco.Application.Interfaces
{
    public interface IProdutoCacheService
    {
        ProdutoCacheEntry BuscarPorCodigo(string codigoBarras);
        List<ProdutoCacheEntry> ListarTodos();
        void SincronizarAgora();
    }
}
