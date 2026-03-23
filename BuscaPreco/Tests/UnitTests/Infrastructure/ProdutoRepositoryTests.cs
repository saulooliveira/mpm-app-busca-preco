using System.Collections.Generic;
using Xunit;
using Moq;
using BuscaPreco.Infrastructure.Repositories;
using BuscaPreco.Infrastructure.Database;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Tests.Infrastructure
{
    public class ProdutoRepositoryTests
    {
        private readonly Mock<DbfDatabase> _mockDbfDatabase;

        public ProdutoRepositoryTests()
        {
            _mockDbfDatabase = new Mock<DbfDatabase>();
        }

        /// <summary>
        /// Testa se BuscarPorCodigo delega para DbfDatabase
        /// </summary>
        [Fact]
        public void BuscarPorCodigo_DeveDelegarParaDbfDatabase()
        {
            // Arrange
            var codigo = "12345";
            var esperado = ("Produto Teste", 99.99m);
            
            _mockDbfDatabase
                .Setup(x => x.BuscarPorCodigo(codigo))
                .Returns(esperado);
            
            var repository = new ProdutoRepository(_mockDbfDatabase.Object);
            
            // Act
            var resultado = repository.BuscarPorCodigo(codigo);
            
            // Assert
            Assert.Equal(esperado.Item1, resultado.des);
            Assert.Equal(esperado.Item2, resultado.vlrVenda1);
            _mockDbfDatabase.Verify(x => x.BuscarPorCodigo(codigo), Times.Once,
                "Deve delegar chamada para DbfDatabase");
        }

        /// <summary>
        /// Testa se ListarTudo delega para DbfDatabase
        /// </summary>
        [Fact]
        public void ListarTudo_DeveDelegarParaDbfDatabase()
        {
            // Arrange
            var produtosEsperados = new List<Produto>
            {
                new Produto 
                { 
                    CodigoItem = "001", 
                    Descricao1 = "Produto 1", 
                    Preco = 10.00m,
                    Unidade = "UN"
                },
                new Produto 
                { 
                    CodigoItem = "002", 
                    Descricao1 = "Produto 2", 
                    Preco = 20.00m,
                    Unidade = "KG"
                }
            };
            
            _mockDbfDatabase
                .Setup(x => x.ListarTudo())
                .Returns(produtosEsperados);
            
            var repository = new ProdutoRepository(_mockDbfDatabase.Object);
            
            // Act
            var resultado = repository.ListarTudo();
            
            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(2, resultado.Count);
            Assert.Equal("Produto 1", resultado[0].Descricao1);
            Assert.Equal("Produto 2", resultado[1].Descricao1);
            _mockDbfDatabase.Verify(x => x.ListarTudo(), Times.Once,
                "Deve delegar chamada para DbfDatabase");
        }

        /// <summary>
        /// Testa se o constructor armazena referÃªncia do DbfDatabase
        /// </summary>
        [Fact]
        public void Constructor_DeveArmazenarRefÃªnciaDbfDatabase()
        {
            // Arrange & Act
            var repository = new ProdutoRepository(_mockDbfDatabase.Object);
            
            // Assert - Verifica se a referÃªncia foi armazenada tentando usar outro mÃ©todo
            var codigo = "TEST";
            _mockDbfDatabase
                .Setup(x => x.BuscarPorCodigo(codigo))
                .Returns(("Teste", 50m));
            
            repository.BuscarPorCodigo(codigo);
            
            // Se chegou aqui, a referÃªncia foi armazenada corretamente
            _mockDbfDatabase.Verify(x => x.BuscarPorCodigo(codigo), Times.Once);
        }
    }
}
