using System.Collections.Generic;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Domain.Interfaces
{
    public interface IProdutoRepository
    {
        (string des, decimal vlrVenda1) BuscarPorCodigo(string codigo);
        List<Produto> ListarTudo();
    }
}
