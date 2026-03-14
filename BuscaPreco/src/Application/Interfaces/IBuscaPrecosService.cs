using System.Collections.Generic;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Application.Interfaces
{
    public interface IBuscaPrecosService
    {
        (string des, decimal vlrVenda1) BuscarPorCodigo(string codigo);
        List<Produto> ListarTudo();
    }
}
