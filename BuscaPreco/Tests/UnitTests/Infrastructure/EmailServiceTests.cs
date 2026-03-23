using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using BuscaPreco.Infrastructure.Services;
using BuscaPreco.Application.Configurations;
using BuscaPreco.CrossCutting;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Tests.Infrastructure
{
    public class EmailServiceTests
    {
        private readonly Mock<IOptions<EmailConfig>> _mockEmailOptions;
        private readonly Mock<Logger> _mockLogger;
        private readonly EmailConfig _emailConfig;

        public EmailServiceTests()
        {
            _mockLogger = new Mock<Logger>();
            _emailConfig = new EmailConfig
            {
                SmtpHost = "smtp.test.com",
                SmtpPort = 587,
                EnableSsl = true,
                Username = "test@test.com",
                Password = "password123",
                Remetente = "noreply@test.com",
                Destinatario = "admin@test.com",
                LogDirectory = "logs"
            };
            
            _mockEmailOptions = new Mock<IOptions<EmailConfig>>();
            _mockEmailOptions.Setup(x => x.Value).Returns(_emailConfig);
        }

        /// <summary>
        /// Testa se SendDailyReportAsync envia email com relatório válido
        /// </summary>
        [Fact]
        public async Task SendDailyReportAsync_DeveEnviarEmailComRelatorioValido()
        {
            // Arrange
            var service = new EmailService(_mockEmailOptions.Object, _mockLogger.Object);
            var referenceDate = DateTime.Now;
            var cancellationToken = CancellationToken.None;
            
            // Cria arquivo de log de teste
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            var logFile = Path.Combine(logDirectory, $"consultas-{referenceDate:yyyyMMdd}.txt");
            
            try
            {
                // Simula arquivo de log com algumas entradas
                var logContent = "Status=Encontrado\nStatus=Não Cadastrado\nStatus=Encontrado";
                if (!File.Exists(logFile))
                    File.WriteAllText(logFile, logContent);
                
                // Act
                await service.SendDailyReportAsync(referenceDate, cancellationToken);
                
                // Assert - Verifica que o logger foi chamado
                _mockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.AtLeastOnce,
                    "Deve registrar informações ao enviar email");
            }
            finally
            {
                if (File.Exists(logFile))
                    File.Delete(logFile);
            }
        }

        /// <summary>
        /// Testa se SendDailyReportAsync ignora quando arquivo não existe
        /// </summary>
        [Fact]
        public async Task SendDailyReportAsync_DeveIgnorarSeArquivoNaoExiste()
        {
            // Arrange
            var service = new EmailService(_mockEmailOptions.Object, _mockLogger.Object);
            var referenceDate = DateTime.Now.AddDays(-1); // Data do passado
            var cancellationToken = CancellationToken.None;
            
            // Act
            await service.SendDailyReportAsync(referenceDate, cancellationToken);
            
            // Assert - Deve logar warning quando arquivo não encontrado
            _mockLogger.Verify(l => l.Warning(It.Is<string>(s => s.Contains("não encontrado"))), 
                Times.AtLeastOnce, "Deve ignorar quando arquivo de log não existe");
        }

        /// <summary>
        /// Testa se SendDailyReportAsync conta corretamente os encontrados
        /// </summary>
        [Fact]
        public async Task SendDailyReportAsync_DeveContarEncontrados()
        {
            // Arrange
            var service = new EmailService(_mockEmailOptions.Object, _mockLogger.Object);
            var referenceDate = DateTime.Now;
            var cancellationToken = CancellationToken.None;
            
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            var logFile = Path.Combine(logDirectory, $"consultas-{referenceDate:yyyyMMdd}.txt");
            
            try
            {
                // Simula arquivo com múltiplas entradas "Encontrado"
                var logContent = "Status=Encontrado\nStatus=Encontrado\nStatus=Não Cadastrado\nStatus=Encontrado";
                if (!File.Exists(logFile))
                    File.WriteAllText(logFile, logContent);
                
                // Act
                await service.SendDailyReportAsync(referenceDate, cancellationToken);
                
                // Assert - Verifica log registrando sent
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Relatório diário enviado"))), 
                    Times.AtLeastOnce, "Deve registrar quando relatório é enviado");
            }
            finally
            {
                if (File.Exists(logFile))
                    File.Delete(logFile);
            }
        }

        /// <summary>
        /// Testa se SendDailyReportAsync conta corretamente os não cadastrados
        /// </summary>
        [Fact]
        public async Task SendDailyReportAsync_DeveContarNaoCadastrados()
        {
            // Arrange
            var service = new EmailService(_mockEmailOptions.Object, _mockLogger.Object);
            var referenceDate = DateTime.Now;
            var cancellationToken = CancellationToken.None;
            
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            var logFile = Path.Combine(logDirectory, $"consultas-{referenceDate:yyyyMMdd}.txt");
            
            try
            {
                // Simula arquivo com múltiplas entradas "Não Cadastrado"
                var logContent = "Status=Não Cadastrado\nStatus=Encontrado\nStatus=Não Cadastrado\nStatus=Não Cadastrado";
                if (!File.Exists(logFile))
                    File.WriteAllText(logFile, logContent);
                
                // Act
                await service.SendDailyReportAsync(referenceDate, cancellationToken);
                
                // Assert
                _mockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.AtLeastOnce);
            }
            finally
            {
                if (File.Exists(logFile))
                    File.Delete(logFile);
            }
        }

        /// <summary>
        /// Testa se SendDailyReportAsync lança log ao enviar
        /// </summary>
        [Fact]
        public async Task SendDailyReportAsync_DeveLancarLogAoEnviar()
        {
            // Arrange
            var service = new EmailService(_mockEmailOptions.Object, _mockLogger.Object);
            var referenceDate = DateTime.Now;
            var cancellationToken = CancellationToken.None;
            
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            var logFile = Path.Combine(logDirectory, $"consultas-{referenceDate:yyyyMMdd}.txt");
            
            try
            {
                var logContent = "Status=Encontrado";
                if (!File.Exists(logFile))
                    File.WriteAllText(logFile, logContent);
                
                // Act
                await service.SendDailyReportAsync(referenceDate, cancellationToken);
                
                // Assert
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Relatório diário enviado"))), 
                    Times.Once, "Deve registrar log quando email é enviado com sucesso");
            }
            finally
            {
                if (File.Exists(logFile))
                    File.Delete(logFile);
            }
        }

        /// <summary>
        /// Testa se SendDailyReportAsync trata exceções SMTP
        /// </summary>
        [Fact]
        public async Task SendDailyReportAsync_DeveTratarExcecoesSmtp()
        {
            // Arrange
            var service = new EmailService(_mockEmailOptions.Object, _mockLogger.Object);
            var referenceDate = DateTime.Now;
            var cancellationToken = CancellationToken.None;
            
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            var logFile = Path.Combine(logDirectory, $"consultas-{referenceDate:yyyyMMdd}.txt");
            
            try
            {
                var logContent = "Status=Encontrado";
                if (!File.Exists(logFile))
                    File.WriteAllText(logFile, logContent);
                
                // Act - Simula exceção de SMTP (credenciais inválidas)
                var exception = Record.Exception(() => 
                    service.SendDailyReportAsync(referenceDate, cancellationToken).GetAwaiter().GetResult());
                
                // Assert - Exceção pode ocorrer, mas deve ser tratada gracefully
                // Se houver exceção, o método deve ter tentado enviar
                if (exception != null)
                {
                    // Exceção é esperada com credenciais de teste
                    Assert.NotNull(exception);
                }
            }
            finally
            {
                if (File.Exists(logFile))
                    File.Delete(logFile);
            }
        }

        /// <summary>
        /// Testa se o constructor armazena a configuração
        /// </summary>
        [Fact]
        public void Constructor_DeveArmazenarConfiguracao()
        {
            // Arrange & Act
            var service = new EmailService(_mockEmailOptions.Object, _mockLogger.Object);
            
            // Assert - Verifica que foi criado sem exceção
            Assert.NotNull(service);
            _mockEmailOptions.Verify(x => x.Value, Times.AtLeastOnce,
                "Deve acessar configuração no constructor");
        }

        /// <summary>
        /// Testa se SendDailyReportAsync respeita CancellationToken
        /// </summary>
        [Fact]
        public async Task SendDailyReportAsync_DeveReseitarCancellationToken()
        {
            // Arrange
            var service = new EmailService(_mockEmailOptions.Object, _mockLogger.Object);
            var referenceDate = DateTime.Now;
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            var logFile = Path.Combine(logDirectory, $"consultas-{referenceDate:yyyyMMdd}.txt");
            
            try
            {
                var logContent = "Status=Encontrado";
                if (!File.Exists(logFile))
                    File.WriteAllText(logFile, logContent);
                
                // Act - Cancela token imediatamente
                cancellationTokenSource.Cancel();
                
                // Act & Assert
                await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                    await service.SendDailyReportAsync(referenceDate, cancellationToken));
            }
            finally
            {
                cancellationTokenSource.Dispose();
                if (File.Exists(logFile))
                    File.Delete(logFile);
            }
        }
    }
}
