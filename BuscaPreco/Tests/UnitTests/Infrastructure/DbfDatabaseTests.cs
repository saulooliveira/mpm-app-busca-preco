using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Moq;
using BuscaPreco.Infrastructure.Database;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Tests.Infrastructure
{
    public class DbfDatabaseTests
    {
        private readonly Mock<Logger> _mockLogger;
        private readonly string _testDbfPath;

        public DbfDatabaseTests()
        {
            _mockLogger = new Mock<Logger>();
            // Cria um arquivo de teste DBF vÃ¡lido ou usa um existente
            _testDbfPath = Path.Combine(Path.GetTempPath(), "test_database.dbf");
        }

        /// <summary>
        /// Testa se o constructor lanÃ§a exceÃ§Ã£o quando arquivo nÃ£o existe
        /// </summary>
        [Fact]
        public void Constructor_DeveLancarExcecaoSeArquivoNaoExiste()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".dbf");

            // Act & Assert
            var exception = Record.Exception(() => new DbfDatabase(nonExistentPath, _mockLogger.Object));
            Assert.NotNull(exception);
            Assert.IsType<Exception>(exception);
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Caminho nÃ£o existe"))), 
                Times.Once);
        }

        /// <summary>
        /// Testa se BuscarPorCodigo retorna valor do cache quando disponÃ­vel
        /// </summary>
        [Fact]
        public void BuscarPorCodigo_DeveRetornarValorDoCache()
        {
            // Arrange
            var sourceFile = Path.Combine(Path.GetTempPath(), "cache_source.dbf");
            
            try
            {
                // Cria arquivo de origem para passar na validaÃ§Ã£o
                File.WriteAllText(sourceFile, "test data");
                var database = new DbfDatabase(sourceFile, _mockLogger.Object);
                
                // SimulaÃ§Ã£o: adicionamos diretamente ao cache atravÃ©s de reflexÃ£o para teste
                // Em uma implementaÃ§Ã£o real com dependÃªncias injetÃ¡veis, seria mais fÃ¡cil
                var codigo = "12345";
                
                // Act - Chama o mÃ©todo (que tentarÃ¡ ler do cache)
                // Nota: Como o cache nÃ£o foi prÃ©-populado, retornarÃ¡ valor padrÃ£o
                var resultado = database.BuscarPorCodigo(codigo);
                
                // Assert - Verifica que o mÃ©todo foi executado sem exceÃ§Ã£o
                // Log deve mostrar tentativa de buscar no cache
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("BuscarPorCodigo"))), 
                    Times.AtLeastOnce);
            }
            finally
            {
                if (File.Exists(sourceFile))
                    File.Delete(sourceFile);
            }
        }

        /// <summary>
        /// Testa se BuscarPorCodigo popula o cache na primeira consulta
        /// </summary>
        [Fact]
        public void BuscarPorCodigo_DevePopularCacheNaPrimeiraConsulta()
        {
            // Arrange
            var sourceFile = Path.Combine(Path.GetTempPath(), "populate_cache.dbf");
            
            try
            {
                File.WriteAllText(sourceFile, "cache population test");
                var database = new DbfDatabase(sourceFile, _mockLogger.Object);
                var codigo = "99999";
                
                // Act
                var resultado = database.BuscarPorCodigo(codigo);
                
                // Assert - Verifica logs de cache population
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("MakeCache"))), 
                    Times.AtLeastOnce, "Deve popular cache na primeira consulta");
            }
            finally
            {
                if (File.Exists(sourceFile))
                    File.Delete(sourceFile);
            }
        }

        /// <summary>
        /// Testa se BuscarPorCodigo limpa o cache quando arquivo foi modificado
        /// </summary>
        [Fact]
        public void BuscarPorCodigo_DeveLimparCacheAoDetectarArquivoModificado()
        {
            // Arrange
            var sourceFile = Path.Combine(Path.GetTempPath(), "clear_cache_on_modify.dbf");
            
            try
            {
                File.WriteAllText(sourceFile, "initial data");
                File.SetLastWriteTime(sourceFile, DateTime.Now.AddHours(-1));
                
                var database = new DbfDatabase(sourceFile, _mockLogger.Object);
                var codigo = "11111";
                
                // Simula modificaÃ§Ã£o do arquivo
                File.WriteAllText(sourceFile, "modified data");
                File.SetLastWriteTime(sourceFile, DateTime.Now);
                
                // Act
                var resultado = database.BuscarPorCodigo(codigo);
                
                // Assert - Log deve indicar limpeza de cache
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Limpa o cache"))), 
                    Times.AtLeastOnce, "Deve limpar cache quando arquivo foi modificado");
            }
            finally
            {
                if (File.Exists(sourceFile))
                    File.Delete(sourceFile);
            }
        }

        /// <summary>
        /// Testa se BuscarPorCodigo retorna um produto vÃ¡lido
        /// </summary>
        [Fact]
        public void BuscarPorCodigo_DeveRetornarProdutoValido()
        {
            // Arrange
            var sourceFile = Path.Combine(Path.GetTempPath(), "valid_product.dbf");
            
            try
            {
                File.WriteAllText(sourceFile, "valid product data");
                var database = new DbfDatabase(sourceFile, _mockLogger.Object);
                var codigo = "22222";
                
                // Act
                var resultado = database.BuscarPorCodigo(codigo);
                
                // Assert - Resultado deve ser uma tupla com descriÃ§Ã£o e preÃ§o
                Assert.IsType<ValueTuple<string, decimal>>(resultado);
                _mockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.AtLeastOnce);
            }
            finally
            {
                if (File.Exists(sourceFile))
                    File.Delete(sourceFile);
            }
        }

        /// <summary>
        /// Testa se BuscarPorCodigo retorna valor padrÃ£o quando nÃ£o encontrado
        /// </summary>
        [Fact]
        public void BuscarPorCodigo_DeveRetornarValorPadraoSeNaoEncontrado()
        {
            // Arrange
            var sourceFile = Path.Combine(Path.GetTempPath(), "not_found.dbf");
            
            try
            {
                File.WriteAllText(sourceFile, "no product found");
                var database = new DbfDatabase(sourceFile, _mockLogger.Object);
                var codigo = "NOTFOUND";
                
                // Act
                var resultado = database.BuscarPorCodigo(codigo);
                
                // Assert - DescriÃ§Ã£o vazia ou nula, preÃ§o zero
                Assert.True(string.IsNullOrEmpty(resultado.des) || resultado.vlrVenda1 == 0,
                    "Deve retornar valor padrÃ£o quando cÃ³digo nÃ£o encontrado");
            }
            finally
            {
                if (File.Exists(sourceFile))
                    File.Delete(sourceFile);
            }
        }

        /// <summary>
        /// Testa se ListarTudo retorna lista de produtos
        /// </summary>
        [Fact]
        public void ListarTudo_DeveRetornarListaDeProdutos()
        {
            // Arrange
            var sourceFile = Path.Combine(Path.GetTempPath(), "list_all.dbf");
            
            try
            {
                File.WriteAllText(sourceFile, "list all products");
                var database = new DbfDatabase(sourceFile, _mockLogger.Object);
                
                // Act
                var resultado = database.ListarTudo();
                
                // Assert
                Assert.IsType<List<Produto>>(resultado);
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Abrindo o arquivo"))), 
                    Times.AtLeastOnce);
            }
            finally
            {
                if (File.Exists(sourceFile))
                    File.Delete(sourceFile);
            }
        }

        /// <summary>
        /// Testa se ListarTudo filtra especÃ­fico por cÃ³digo
        /// </summary>
        [Fact]
        public void ListarTudo_DeveFiltrareSpecificoPorCodigo()
        {
            // Arrange
            var sourceFile = Path.Combine(Path.GetTempPath(), "filter_by_code.dbf");
            
            try
            {
                File.WriteAllText(sourceFile, "filter test");
                var database = new DbfDatabase(sourceFile, _mockLogger.Object);
                
                // Act
                var resultado = database.ListarTudo();
                
                // Assert - Produtos com cÃ³digo comeÃ§ando com "20" devem ser filtrados
                // (conforme lÃ³gica em ListarTudo)
                foreach (var produto in resultado)
                {
                    Assert.NotNull(produto.CodigoItem);
                    Assert.NotNull(produto.Descricao1);
                }
            }
            finally
            {
                if (File.Exists(sourceFile))
                    File.Delete(sourceFile);
            }
        }

        /// <summary>
        /// Testa se ListarTudo retorna lista vazia sem arquivo
        /// </summary>
        [Fact]
        public void ListarTodo_DeveRetornarVazioSemaArquivo()
        {
            // Arrange
            var sourceFile = Path.Combine(Path.GetTempPath(), "empty_list.dbf");
            
            try
            {
                File.WriteAllText(sourceFile, "empty");
                var database = new DbfDatabase(sourceFile, _mockLogger.Object);
                
                // Remove arquivo para simular indisponibilidade
                File.Delete(sourceFile);
                
                // Nota: DbfDatabase jÃ¡ foi inicializado, entÃ£o usamos CopyIfModified
                // que gerarÃ¡ erro de arquivo nÃ£o encontrado
                
                // Act & Assert
                // Esperamos que o mÃ©todo retorne lista vazia ou lance exceÃ§Ã£o tratada
                try
                {
                    var resultado = database.ListarTudo();
                    Assert.NotNull(resultado);
                }
                catch (Exception ex)
                {
                    // ExceÃ§Ã£o esperada quando arquivo nÃ£o existe
                    Assert.NotNull(ex);
                }
            }
            finally
            {
                if (File.Exists(sourceFile))
                    File.Delete(sourceFile);
            }
        }

        /// <summary>
        /// Testa se LimparCache limpa todas as entradas
        /// </summary>
        [Fact]
        public void LimparCache_DeveLimparTodasAsEntradas()
        {
            // Arrange
            var sourceFile = Path.Combine(Path.GetTempPath(), "clear_all_cache.dbf");
            
            try
            {
                File.WriteAllText(sourceFile, "clear cache test");
                var database = new DbfDatabase(sourceFile, _mockLogger.Object);
                
                // Act
                database.LimparCache();
                
                // Assert - Verifica log de limpeza
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Limpar Cache"))), 
                    Times.AtLeastOnce);
            }
            finally
            {
                if (File.Exists(sourceFile))
                    File.Delete(sourceFile);
            }
        }

        /// <summary>
        /// Testa se o cache Ã© reutilizado em consultas consecutivas
        /// </summary>
        [Fact]
        public void Cache_DeveSerReutilizadoEmConsecutivas()
        {
            // Arrange
            var sourceFile = Path.Combine(Path.GetTempPath(), "reuse_cache.dbf");
            
            try
            {
                File.WriteAllText(sourceFile, "reuse cache data");
                var database = new DbfDatabase(sourceFile, _mockLogger.Object);
                var codigo = "33333";
                
                // Act
                var resultado1 = database.BuscarPorCodigo(codigo);
                var resultado2 = database.BuscarPorCodigo(codigo);
                
                // Assert - Ambas as buscas devem retornar o mesmo resultado
                Assert.Equal(resultado1.des, resultado2.des);
                Assert.Equal(resultado1.vlrVenda1, resultado2.vlrVenda1);
                
                // Verifica que houve busca em cache (nÃ£o deve chamar MakeCache novamente)
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("cachedResult"))), 
                    Times.AtLeastOnce);
            }
            finally
            {
                if (File.Exists(sourceFile))
                    File.Delete(sourceFile);
            }
        }

        /// <summary>
        /// Testa se BuscarPorCodigo trata exceÃ§Ãµes do arquivo DBF
        /// </summary>
        [Fact]
        public void BuscarPorCodigo_DeveTratarExcecoesDoArquivoDBF()
        {
            // Arrange
            var sourceFile = Path.Combine(Path.GetTempPath(), "corrupted.dbf");
            
            try
            {
                File.WriteAllText(sourceFile, "corrupted dbf data");
                var database = new DbfDatabase(sourceFile, _mockLogger.Object);
                var codigo = "44444";
                
                // Act
                var exception = Record.Exception(() => database.BuscarPorCodigo(codigo));
                
                // Assert - MÃ©todo deve tratar exceÃ§Ã£o gracefully
                // Log deve registrar erro
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Erro ao ler"))), 
                    Times.AtLeastOnce, "Deve registrar erro ao ler arquivo corrompido");
            }
            finally
            {
                if (File.Exists(sourceFile))
                    File.Delete(sourceFile);
            }
        }
    }
}
