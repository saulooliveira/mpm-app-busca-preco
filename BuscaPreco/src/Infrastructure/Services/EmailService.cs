using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailConfig _config;
        private readonly Logger _logger;

        public EmailService(IOptions<EmailConfig> options, Logger logger)
        {
            _config = options.Value;
            _logger = logger;
        }

        public async Task SendDailyReportAsync(DateTime referenceDate, CancellationToken cancellationToken)
        {
            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _config.LogDirectory ?? "logs");
            var logFile = Path.Combine(directory, $"consultas-{referenceDate:yyyyMMdd}.txt");

            if (!File.Exists(logFile))
            {
                _logger.Warning("Relatório diário ignorado: arquivo de log não encontrado em {LogFile}", logFile);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var linhas = File.ReadAllLines(logFile);
            var total = linhas.Length;
            var encontrados = linhas.Count(l => l.Contains("Status=Encontrado"));
            var naoCadastrados = linhas.Count(l => l.Contains("Status=Não Cadastrado"));

            var body =
$@"Resumo de consultas do dia {referenceDate:dd/MM/yyyy}

Total de consultas: {total}
Encontrados: {encontrados}
Não cadastrados: {naoCadastrados}

Arquivo de referência: {logFile}";

            using (var smtp = new SmtpClient(_config.SmtpHost, _config.SmtpPort)
            {
                EnableSsl = _config.EnableSsl,
                Credentials = new NetworkCredential(_config.Username, _config.Password)
            })
            using (var message = new MailMessage(_config.Remetente, _config.Destinatario)
            {
                Subject = "Relatório de Consultas Diárias - Mercado Progresso Mineiro",
                Body = body
            })
            {
                await smtp.SendMailAsync(message);
                _logger.Info("Relatório diário enviado para {Destinatario}", _config.Destinatario);
            }
        }
    }
}
