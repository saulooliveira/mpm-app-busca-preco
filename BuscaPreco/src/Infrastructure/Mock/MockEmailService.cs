using System;
using System.Threading;
using System.Threading.Tasks;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;

namespace BuscaPreco.Infrastructure.Mock
{
    public class MockEmailService : IEmailService
    {
        private readonly Logger _logger;
        public MockEmailService(Logger logger) => _logger = logger;

        public Task SendDailyReportAsync(DateTime referenceDate, CancellationToken cancellationToken)
        {
            _logger.Info("[MOCK] EmailService: relatório diário de {Data} não enviado (modo mock).", referenceDate.ToShortDateString());
            return Task.CompletedTask;
        }
    }
}
