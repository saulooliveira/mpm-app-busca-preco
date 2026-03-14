using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Application.Services
{
    public class RelatorioDiarioBackgroundService : BackgroundService
    {
        private readonly IEmailService _emailService;
        private readonly EmailConfig _emailConfig;
        private readonly Logger _logger;

        public RelatorioDiarioBackgroundService(
            IEmailService emailService,
            IOptions<EmailConfig> emailConfig,
            Logger logger)
        {
            _emailService = emailService;
            _emailConfig = emailConfig.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextRun = GetNextRun(DateTime.Now, _emailConfig.DailyReportTime);
                var delay = nextRun - DateTime.Now;

                _logger.Info("Próximo relatório diário agendado para {NextRun}", nextRun);
                await Task.Delay(delay, stoppingToken);

                try
                {
                    await _emailService.SendDailyReportAsync(DateTime.Now.Date, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.Error("Falha ao enviar relatório diário: {Message}", ex.Message);
                }
            }
        }

        private static DateTime GetNextRun(DateTime now, string configuredTime)
        {
            if (!TimeSpan.TryParseExact(configuredTime, @"hh\:mm", CultureInfo.InvariantCulture, out var timeOfDay))
            {
                timeOfDay = new TimeSpan(23, 55, 0);
            }

            var nextRun = now.Date.Add(timeOfDay);
            return nextRun > now ? nextRun : nextRun.AddDays(1);
        }
    }
}
