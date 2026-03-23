using System;
using System.Collections.Generic;
using System.Linq;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Infrastructure.Mock
{
    /// <summary>
    /// Implementação mock de IBuscaPrecosService para uso em builds Debug.
    /// Não usa SQLite nem DBF — busca no MockProdutoCacheService em memória.
    /// </summary>
    public class MockBuscaPrecosService : IBuscaPrecosService
    {
        private readonly IProdutoCacheService _cache;
        private readonly Logger _logger;

        public MockBuscaPrecosService(IProdutoCacheService cache, Logger logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public (string des, decimal vlrVenda1) BuscarPorCodigo(string codigo)
        {
            var produto = _cache.BuscarPorCodigo(codigo);
            bool encontrado = produto != null && !string.IsNullOrWhiteSpace(produto.Descricao);

            _logger.Info("[MOCK] BuscarPorCodigo {Codigo} → {Status}",
                codigo, encontrado ? produto.Descricao : "Não Encontrado");

            return encontrado
                ? (produto.Descricao, produto.Preco)
                : (string.Empty, 0m);
        }

        public List<Produto> ListarTudo()
        {
            return _cache.ListarTodos()
                .Select(p => new Produto
                {
                    CodigoItem  = p.CodigoBarras,
                    Descricao1  = p.Descricao,
                    Preco       = p.Preco,
                    Unidade     = p.Unidade
                })
                .ToList();
        }
    }
}
