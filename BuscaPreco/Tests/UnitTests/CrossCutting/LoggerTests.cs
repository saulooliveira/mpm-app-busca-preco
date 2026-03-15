using Xunit;
using BuscaPreco.CrossCutting;

namespace BuscaPreco.Tests.CrossCutting
{
    public class LoggerTests
    {
        private readonly Logger _logger;

        public LoggerTests()
        {
            _logger = new Logger();
        }

        [Fact]
        public void Info_DeveLancarMensagemInfo()
        {
            // Arrange
            var mensagem = "Mensagem de informação";

            // Act & Assert - Não deve lançar exceção
            var exception = Record.Exception(() => _logger.Info(mensagem));
            Assert.Null(exception);
        }

        [Fact]
        public void Warning_DeveLancarMensagemWarning()
        {
            // Arrange
            var mensagem = "Mensagem de aviso";

            // Act & Assert - Não deve lançar exceção
            var exception = Record.Exception(() => _logger.Warning(mensagem));
            Assert.Null(exception);
        }

        [Fact]
        public void Error_DeveLancarMensagemError()
        {
            // Arrange
            var mensagem = "Mensagem de erro";

            // Act & Assert - Não deve lançar exceção
            var exception = Record.Exception(() => _logger.Error(mensagem));
            Assert.Null(exception);
        }

        [Fact]
        public void Logger_DeveFormatarParametrosCorretamente()
        {
            // Arrange
            var template = "Código: {0}, Preço: {1}";
            var codigo = "123456";
            var preco = 99.99;

            // Act & Assert - Não deve lançar exceção
            var exception = Record.Exception(() => 
                _logger.Info(template, codigo, preco));
            Assert.Null(exception);
        }

        [Fact]
        public void Logger_DeveAceitarMultiplosParametros()
        {
            // Arrange
            var template = "Produto {0} - Descrição: {1} - Preço: {2} - Estoque: {3}";
            var codigo = "001";
            var descricao = "Teste";
            var preco = 10.50;
            var estoque = 100;

            // Act & Assert - Não deve lançar exceção
            var exception = Record.Exception(() => 
                _logger.Info(template, codigo, descricao, preco, estoque));
            Assert.Null(exception);
        }

        [Fact]
        public void Logger_DeveAceitarMensagemSemParametros()
        {
            // Arrange
            var mensagem = "Mensagem simples sem parâmetros";

            // Act & Assert - Não deve lançar exceção
            var exception = Record.Exception(() => _logger.Info(mensagem));
            Assert.Null(exception);
        }
    }
}
