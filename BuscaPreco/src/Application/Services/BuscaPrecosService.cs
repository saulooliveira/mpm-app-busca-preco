using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Entities;
using BuscaPreco.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Application.Services
{
    public class BuscaPrecosService : IBuscaPrecosService
    {
        private readonly IProdutoCacheService _produtoCacheService;
        private readonly IAlertService _alertService;
        private readonly ITerminalActivityMonitor _terminalActivityMonitor;
        private readonly Logger _logger;
        private readonly ConsultaRepository _consultaRepository;

        public BuscaPrecosService(
            IProdutoCacheService produtoCacheService,
            IAlertService alertService,
            ITerminalActivityMonitor terminalActivityMonitor,
            IOptions<FeatureConfig> featureOptions,
            Logger logger,
            ConsultaRepository consultaRepository)
        {
            _ = featureOptions;
            _produtoCacheService = produtoCacheService;
            _alertService = alertService;
            _terminalActivityMonitor = terminalActivityMonitor;
            _logger = logger;
            _consultaRepository = consultaRepository;
        }

        public (string des, decimal vlrVenda1) BuscarPorCodigo(string codigo)
        {
            _terminalActivityMonitor.MarkActivity();

            var produto = _produtoCacheService.BuscarPorCodigo(codigo);
            var encontrado = produto != null && !string.IsNullOrWhiteSpace(produto.Descricao);

            if (!encontrado)
                _ = Task.Run(() => _alertService.NotifyProdutoNaoEncontradoAsync(codigo));

            LogAuditoria(
                codigo,
                encontrado ? produto.Descricao : string.Empty,
                encontrado ? produto.Preco : 0m);

            return encontrado
                ? (produto.Descricao, produto.Preco)
                : (string.Empty, 0m);
        }

        public List<Produto> ListarTudo()
        {
            return _produtoCacheService.ListarTodos()
                .Select(p => new Produto
                {
                    CodigoItem = p.CodigoBarras,
                    Descricao1 = p.Descricao,
                    Preco = p.Preco,
                    Unidade = p.Unidade
                })
                .ToList();
        }

        private void LogAuditoria(string codigo, string nome, decimal preco)
        {
            var encontrado = !string.IsNullOrWhiteSpace(nome);
            var precoStr = encontrado ? preco.ToString("N2") : "0,00";

            _logger.Info(
                "AuditoriaConsulta DataHora={DataHora} CodigoBarras={CodigoBarras} " +
                "Nome={Nome} Preco={Preco} Status={Status} Origem={Origem}",
                DateTime.Now,
                codigo,
                encontrado ? nome : string.Empty,
                precoStr,
                encontrado ? "Encontrado" : "Não Cadastrado",
                "SQLite");

            _consultaRepository.Gravar(
                codigoBarras: codigo,
                nome: encontrado ? nome : string.Empty,
                preco: precoStr,
                encontrado: encontrado,
                origem: "SQLite");
        }
    }
}
