using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Application.Services
{
    public class ProdutoCacheTracker : IProdutoCacheTracker
    {
        private readonly ConcurrentDictionary<string, Produto> _produtosPorCodigo = new ConcurrentDictionary<string, Produto>();

        public void Track(string codigo, Produto produto)
        {
            _produtosPorCodigo.AddOrUpdate(codigo, produto, (_, __) => produto);
        }

        public void Remove(string codigo)
        {
            _produtosPorCodigo.TryRemove(codigo, out _);
        }

        public IReadOnlyCollection<Produto> SnapshotProdutos()
        {
            return _produtosPorCodigo.Values.ToList();
        }
    }
}
