using System;
using System.Collections.Generic;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Entities;
using BuscaPreco.Domain.Interfaces;

namespace BuscaPreco.Application.Services
{
    public class BuscaPrecosService : IBuscaPrecosService
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly Logger _logger;

        public BuscaPrecosService(IProdutoRepository produtoRepository, Logger logger)
        {
            _produtoRepository = produtoRepository;
            _logger = logger;
        }

        public (string des, decimal vlrVenda1) BuscarPorCodigo(string codigo)
        {
            var resultado = _produtoRepository.BuscarPorCodigo(codigo);
            var encontrado = !string.IsNullOrWhiteSpace(resultado.des);

            _logger.Info(
                "AuditoriaConsulta DataHora={DataHora} CodigoBarras={CodigoBarras} Nome={Nome} Preco={Preco} Status={Status}",
                DateTime.Now,
                codigo,
                encontrado ? resultado.des : "",
                encontrado ? resultado.vlrVenda1.ToString("N2") : "0,00",
                encontrado ? "Encontrado" : "Não Cadastrado");

            return resultado;
        }

        public List<Produto> ListarTudo()
        {
            return _produtoRepository.ListarTudo();
        }
    }
}
