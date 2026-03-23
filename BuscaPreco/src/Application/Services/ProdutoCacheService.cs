using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Application.DTOs;
using BuscaPreco.CrossCutting;
using BuscaPreco.Infrastructure.Database;
using BuscaPreco.Infrastructure.Repositories;

namespace BuscaPreco.Application.Services
{
    public class ProdutoCacheService : IProdutoCacheService, IDisposable
    {
        private readonly DbfDatabase _dbfDatabase;
        private readonly ProdutoSqliteRepository _sqliteRepo;
        private readonly Logger _logger;
        private readonly string _dbfFilePath;

        private ConcurrentDictionary<string, ProdutoCacheEntry> _l1 =
            new ConcurrentDictionary<string, ProdutoCacheEntry>(StringComparer.OrdinalIgnoreCase);

        private FileSystemWatcher _watcher;
        private readonly object _syncLock = new object();
        private bool _syncInProgress;
        private bool _disposed;
        private System.Threading.Timer _debounceTimer;
        private const int DebounceMs = 3000;

        public ProdutoCacheService(
            DbfDatabase dbfDatabase,
            ProdutoSqliteRepository sqliteRepo,
            DbfConfig dbfConfig,
            Logger logger)
        {
            _dbfDatabase = dbfDatabase;
            _sqliteRepo = sqliteRepo;
            _logger = logger;
            _dbfFilePath = dbfConfig.DbfFilePath;

            InicializarCacheL1();
            InicializarWatcher();
        }

        private void InicializarCacheL1()
        {
            var produtosSqlite = _sqliteRepo.ListarTodos();

            if (produtosSqlite.Count == 0)
            {
                _logger.Info("ProdutoCacheService: SQLite vazio â€” importando DBF na inicializaÃ§Ã£o.");
                SincronizarDbfParaSqlite();
                produtosSqlite = _sqliteRepo.ListarTodos();
            }
            else
            {
                _logger.Info("ProdutoCacheService: L1 populado do SQLite com {Count} produtos.", produtosSqlite.Count);
            }

            var novoL1 = new ConcurrentDictionary<string, ProdutoCacheEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in produtosSqlite)
                novoL1[p.CodigoBarras] = p;

            _l1 = novoL1;
        }

        private void InicializarWatcher()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_dbfFilePath) || !File.Exists(_dbfFilePath))
                {
                    _logger.Warning("ProdutoCacheService: DBF nÃ£o encontrado para monitoramento: {Path}", _dbfFilePath);
                    return;
                }

                var dir = Path.GetDirectoryName(_dbfFilePath);
                var file = Path.GetFileName(_dbfFilePath);

                _watcher = new FileSystemWatcher(dir, file)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                _watcher.Changed += OnDbfChanged;
                _logger.Info("ProdutoCacheService: monitorando DBF em {Path}", _dbfFilePath);
            }
            catch (Exception ex)
            {
                _logger.Warning("ProdutoCacheService: falha ao inicializar FileSystemWatcher: {Erro}", ex.Message);
            }
        }

        private void OnDbfChanged(object sender, FileSystemEventArgs e)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new System.Threading.Timer(_ =>
            {
                _logger.Info("ProdutoCacheService: DBF modificado â€” iniciando ressincronizaÃ§Ã£o.");
                SincronizarAgora();
            }, null, DebounceMs, System.Threading.Timeout.Infinite);
        }

        public ProdutoCacheEntry BuscarPorCodigo(string codigoBarras)
        {
            if (_l1.TryGetValue(codigoBarras, out var entry))
                return entry;

            var fromDb = _sqliteRepo.BuscarPorCodigo(codigoBarras);
            if (fromDb != null)
            {
                _l1[codigoBarras] = fromDb;
                return fromDb;
            }

            return null;
        }

        public List<ProdutoCacheEntry> ListarTodos() => _l1.Values.ToList();

        public void SincronizarAgora()
        {
            lock (_syncLock)
            {
                if (_syncInProgress)
                {
                    _logger.Info("ProdutoCacheService: sincronizaÃ§Ã£o jÃ¡ em andamento â€” ignorando.");
                    return;
                }
                _syncInProgress = true;
            }

            try
            {
                SincronizarDbfParaSqlite();
                var produtos = _sqliteRepo.ListarTodos();
                var novoL1 = new ConcurrentDictionary<string, ProdutoCacheEntry>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in produtos)
                    novoL1[p.CodigoBarras] = p;

                _l1 = novoL1;
                _logger.Info("ProdutoCacheService: ressincronizaÃ§Ã£o concluÃ­da. {Count} produtos no cache.", novoL1.Count);
            }
            catch (Exception ex)
            {
                _logger.Error("ProdutoCacheService.SincronizarAgora: {Erro}", ex.Message);
            }
            finally
            {
                lock (_syncLock) { _syncInProgress = false; }
            }
        }

        private void SincronizarDbfParaSqlite()
        {
            var produtosDbf = _dbfDatabase.ListarTudo();
            var entradas = produtosDbf.Select(p => new ProdutoCacheEntry
            {
                CodigoBarras = p.CodigoItem,
                Descricao = p.Descricao1 ?? string.Empty,
                Preco = p.Preco,
                Unidade = p.Unidade ?? string.Empty,
                UltimaAtualizacao = DateTime.Now
            });

            _sqliteRepo.SubstituirTodos(entradas);
            _logger.Info("ProdutoCacheService: {Count} produtos sincronizados do DBF para SQLite.", produtosDbf.Count);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _watcher?.Dispose();
            _debounceTimer?.Dispose();
        }
    }
}
