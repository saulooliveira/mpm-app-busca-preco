using System.Collections.Generic;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Application.Interfaces
{
    public interface IProdutoCacheTracker
    {
        void Track(string codigo, Produto produto);
        void Remove(string codigo);
        IReadOnlyCollection<Produto> SnapshotProdutos();
    }
}
