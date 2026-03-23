using System;
using BuscaPreco.Application.Configurations;
using BuscaPreco.CrossCutting;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Infrastructure.Mock
{
    /// <summary>
    /// Substituto do Servidor em modo Debug: não abre socket, apenas loga.
    /// </summary>
    public class MockServidor : BuscaPreco.Infrastructure.Terminal.Servidor
    {
        private readonly Logger _logger;

        public MockServidor(IOptions<TerminalConfig> terminalOptions, Logger logger)
            : base(terminalOptions, logger)
        {
            _logger = logger;
        }

        public new void Start()
        {
            _logger.Info("[MOCK] Servidor: Start() ignorado em modo mock.");
        }

        public new System.Threading.Tasks.Task StartAsync()
        {
            _logger.Info("[MOCK] Servidor: StartAsync() ignorado em modo mock.");
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public new void Stop()
        {
            _logger.Info("[MOCK] Servidor: Stop() ignorado em modo mock.");
        }
    }
}
