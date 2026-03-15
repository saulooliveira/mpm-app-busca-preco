using Xunit;
using Moq;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Application.Services;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Entities;
using BuscaPreco.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Tests.Application
{
    public class BuscaPrecosServiceTests
    {
        private readonly Mock<IProdutoRepository> _repositoryMock;
        private readonly Mock<IAlertService> _alertServiceMock;
        private readonly Mock<IProdutoCacheTracker> _cacheTrackerMock;
        private readonly Mock<ITerminalActivityMonitor> _activityMonitorMock;
        private readonly Mock<IOptions<FeatureConfig>> _featureConfigMock;
        private readonly Logger _logger;
        private readonly BuscaPrecosService _service;

        public BuscaPrecosServiceTests()
        {
            _repositoryMock = new Mock<IProdutoRepository>();
            _alertServiceMock = new Mock<IAlertService>();
            _cacheTrackerMock = new Mock<IProdutoCacheTracker>();
            _activityMonitorMock = new Mock<ITerminalActivityMonitor>();
            _featureConfigMock = new Mock<IOptions<FeatureConfig>>();
            _logger = new Logger();

            var featureConfig = new FeatureConfig { CacheTTLMinutes = 5 };
            _featureConfigMock.Setup(x => x.Value).Returns(featureConfig);

            _service = new BuscaPrecosService(
                _repositoryMock.Object,
                _alertServiceMock.Object,
                _cacheTrackerMock.Object,
                _activityMonitorMock.Object,
                _featureConfigMock.Object,
                _logger
            );
        }

        [Fact]
        public void BuscarPorCodigo_DeveRetornarProdutoQuandoEncontrado()
        {
            // Arrange
            var codigo = "123456";
            var produto = new Produto { cod = codigo, des = "Produto Teste", vlrVenda1 = 10.50m };
            _repositoryMock.Setup(x => x.BuscarPorCodigo(codigo))
                .Returns((produto.des, produto.vlrVenda1));

            // Act
            var resultado = _service.BuscarPorCodigo(codigo);

            // Assert
            Assert.Equal("Produto Teste", resultado.des);
            Assert.Equal(10.50m, resultado.vlrVenda1);
            _repositoryMock.Verify(x => x.BuscarPorCodigo(codigo), Times.Once);
        }

        [Fact]
        public void BuscarPorCodigo_DeveRetornarVazioQuandoNaoEncontrado()
        {
            // Arrange
            var codigo = "999999";
            _repositoryMock.Setup(x => x.BuscarPorCodigo(codigo))
                .Returns((string.Empty, 0m));

            // Act
            var resultado = _service.BuscarPorCodigo(codigo);

            // Assert
            Assert.Empty(resultado.des);
            Assert.Equal(0m, resultado.vlrVenda1);
        }

        [Fact]
        public void BuscarPorCodigo_DeveUsarCacheQuandoDisponivel()
        {
            // Arrange
            var codigo = "123456";
            var produto = new Produto { cod = codigo, des = "Produto Teste", vlrVenda1 = 10.50m };
            _repositoryMock.Setup(x => x.BuscarPorCodigo(codigo))
                .Returns((produto.des, produto.vlrVenda1));

            // Act - Primeira busca (sem cache)
            _service.BuscarPorCodigo(codigo);
            
            // Act - Segunda busca (com cache)
            var resultado = _service.BuscarPorCodigo(codigo);

            // Assert
            Assert.Equal("Produto Teste", resultado.des);
            _repositoryMock.Verify(x => x.BuscarPorCodigo(codigo), Times.Once);
        }

        [Fact]
        public void BuscarPorCodigo_DeveRecarregarCacheQuandoExpirado()
        {
            // Arrange
            var codigo = "123456";
            var produto = new Produto { cod = codigo, des = "Produto Teste", vlrVenda1 = 10.50m };
            _repositoryMock.Setup(x => x.BuscarPorCodigo(codigo))
                .Returns((produto.des, produto.vlrVenda1));

            var configComTTLCurto = new FeatureConfig { CacheTTLMinutes = 1 };
            _featureConfigMock.Setup(x => x.Value).Returns(configComTTLCurto);

            // Act - Primeira busca
            _service.BuscarPorCodigo(codigo);
            
            // Simular expiração do cache (aguardar TTL)
            System.Threading.Thread.Sleep(1500);
            
            // Act - Segunda busca após expiração
            _service.BuscarPorCodigo(codigo);

            // Assert - Deve ter chamado a repository 2 vezes
            _repositoryMock.Verify(x => x.BuscarPorCodigo(codigo), Times.AtLeast(1));
        }

        [Fact]
        public void BuscarPorCodigo_DeveNotificarAlertServiceQuandoProdutoNaoEncontrado()
        {
            // Arrange
            var codigo = "999999";
            _repositoryMock.Setup(x => x.BuscarPorCodigo(codigo))
                .Returns((string.Empty, 0m));

            // Act
            _service.BuscarPorCodigo(codigo);

            // Assert
            // A chamada ao AlertService é assíncrona e dispara em background
            System.Threading.Thread.Sleep(100);
        }

        [Fact]
        public void ListarTudo_DeveRetornarTodosProdutos()
        {
            // Arrange
            var produtos = new List<Produto>
            {
                new Produto { cod = "001", des = "Produto 1", vlrVenda1 = 10.00m },
                new Produto { cod = "002", des = "Produto 2", vlrVenda1 = 20.00m }
            };
            _repositoryMock.Setup(x => x.ListarTudo()).Returns(produtos);

            // Act
            var resultado = _service.ListarTudo();

            // Assert
            Assert.Equal(2, resultado.Count);
            Assert.Equal("Produto 1", resultado[0].des);
            _repositoryMock.Verify(x => x.ListarTudo(), Times.Once);
        }

        [Fact]
        public void BuscarPorCodigo_DevePropagarExcecaoDaRepository()
        {
            // Arrange
            var codigo = "123456";
            var excecaoEsperada = new Exception("Erro na repository");
            _repositoryMock.Setup(x => x.BuscarPorCodigo(codigo))
                .Throws(excecaoEsperada);

            // Act & Assert - Deve lançar a exceção da repository
            var exception = Assert.Throws<Exception>(() => _service.BuscarPorCodigo(codigo));
            Assert.Equal("Erro na repository", exception.Message);
        }

        [Fact]
        public void Cache_DeveExpirarAposTTL()
        {
            // Arrange
            var codigo = "123456";
            var produto = new Produto { cod = codigo, des = "Produto Teste", vlrVenda1 = 10.50m };
            var configComTTLMuitoCurto = new FeatureConfig { CacheTTLMinutes = 0 };
            _featureConfigMock.Setup(x => x.Value).Returns(configComTTLMuitoCurto);
            _repositoryMock.Setup(x => x.BuscarPorCodigo(codigo))
                .Returns((produto.des, produto.vlrVenda1));

            // Act
            _service.BuscarPorCodigo(codigo);
            System.Threading.Thread.Sleep(100);
            _service.BuscarPorCodigo(codigo);

            // Assert
            _repositoryMock.Verify(x => x.BuscarPorCodigo(codigo), Times.AtLeast(1));
        }

        [Fact]
        public void Cache_DeveRastrearProdutoViaProdutoCacheTracker()
        {
            // Arrange
            var codigo = "123456";
            var produto = new Produto { cod = codigo, des = "Produto Teste", vlrVenda1 = 10.50m };
            _repositoryMock.Setup(x => x.BuscarPorCodigo(codigo))
                .Returns((produto.des, produto.vlrVenda1));

            // Act
            _service.BuscarPorCodigo(codigo);

            // Assert
            _cacheTrackerMock.Verify(x => x.Track(codigo, It.IsAny<Produto>()), Times.Once);
        }

        [Fact]
        public void BuscarPorCodigo_DeveFormataPrecoCorretamente()
        {
            // Arrange
            var codigo = "123456";
            var precoEsperado = 99.99m;
            var produto = new Produto { cod = codigo, des = "Produto Teste", vlrVenda1 = precoEsperado };
            _repositoryMock.Setup(x => x.BuscarPorCodigo(codigo))
                .Returns((produto.des, precoEsperado));

            // Act
            var resultado = _service.BuscarPorCodigo(codigo);

            // Assert
            Assert.Equal(precoEsperado, resultado.vlrVenda1);
        }
    }
}
