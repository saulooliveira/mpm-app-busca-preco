using Xunit;
using BuscaPreco.CrossCutting;

namespace BuscaPreco.Tests.CrossCutting
{
    public class ValidatorsTests
    {
        [Fact]
        public void SomenteDigitos_DeveRetornarApenasDigitosQuandoHaMisto()
        {
            // Arrange
            var entrada = "123abc456";

            // Act
            var resultado = Validators.SomenteDigitos(entrada);

            // Assert
            Assert.Equal("123456", resultado);
        }

        [Fact]
        public void SomenteDigitos_DeveRetornarApenasDigitosParaApenasDigitos()
        {
            // Arrange
            var entrada = "123456";

            // Act
            var resultado = Validators.SomenteDigitos(entrada);

            // Assert
            Assert.Equal("123456", resultado);
        }

        [Fact]
        public void SomenteDigitos_DeveRetornarVazioQuandoVazio()
        {
            // Arrange
            var entrada = string.Empty;

            // Act
            var resultado = Validators.SomenteDigitos(entrada);

            // Assert
            Assert.Equal(string.Empty, resultado);
        }

        [Fact]
        public void SomenteDigitos_DeveRetornarVazioQuandoNull()
        {
            // Arrange
            string entrada = null;

            // Act
            var resultado = Validators.SomenteDigitos(entrada);

            // Assert
            Assert.Equal(string.Empty, resultado);
        }

        [Fact]
        public void SomenteDigitos_DeveRetornarVazioQuandoApenasEspacos()
        {
            // Arrange
            var entrada = "   ";

            // Act
            var resultado = Validators.SomenteDigitos(entrada);

            // Assert
            Assert.Equal(string.Empty, resultado);
        }

        [Fact]
        public void SomenteDigitos_DeveRemoverCaracteresEspeciais()
        {
            // Arrange
            var entrada = "123@456#789";

            // Act
            var resultado = Validators.SomenteDigitos(entrada);

            // Assert
            Assert.Equal("123456789", resultado);
        }

        [Fact]
        public void SomenteDigitos_DeveRemoverLetrasAcentuadas()
        {
            // Arrange
            var entrada = "123çáé456";

            // Act
            var resultado = Validators.SomenteDigitos(entrada);

            // Assert
            Assert.Equal("123456", resultado);
        }
    }
}
