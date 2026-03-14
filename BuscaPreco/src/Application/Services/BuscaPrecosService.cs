using System.Collections.Generic;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Domain.Entities;
using BuscaPreco.Domain.Interfaces;

namespace BuscaPreco.Application.Services
{
    public class BuscaPrecosService : IBuscaPrecosService
    {
        private readonly IProdutoRepository _produtoRepository;

        public BuscaPrecosService(IProdutoRepository produtoRepository)
        {
            _produtoRepository = produtoRepository;
        }

        public (string des, decimal vlrVenda1) BuscarPorCodigo(string codigo)
        {
            return _produtoRepository.BuscarPorCodigo(codigo);
        }

        public List<Produto> ListarTudo()
        {
            return _produtoRepository.ListarTudo();
        }
    }
}
