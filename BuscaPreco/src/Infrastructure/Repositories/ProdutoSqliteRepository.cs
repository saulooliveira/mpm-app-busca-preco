using System;
using System.Collections.Generic;
using BuscaPreco.Application.Models;
using BuscaPreco.CrossCutting;
using BuscaPreco.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace BuscaPreco.Infrastructure.Repositories
{
    /// <summary>
    /// Persiste e consulta produtos no SQLite (cache L2).
    /// </summary>
    public class ProdutoSqliteRepository
    {
        private readonly ConsultaDbContext _db;
        private readonly Logger _logger;

        public ProdutoSqliteRepository(ConsultaDbContext db, Logger logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza a lista de produtos de forma incremental (UPSERT).
        /// </summary>
        public void SincronizarProdutos(IEnumerable<ProdutoCacheEntry> produtos)
        {
            try
            {
                using var conn = _db.AbrirConexao();
                using var tx = conn.BeginTransaction();
                try
                {
                    // Usamos INSERT OR REPLACE para uma sincronização incremental eficiente (UPSERT)
                    using var cmdUpsert = conn.CreateCommand();
                    cmdUpsert.Transaction = tx;
                    cmdUpsert.CommandText = @"
                        INSERT OR REPLACE INTO produtos
                            (codigo_barras, descricao, preco, unidade, ultima_atualizacao)
                        VALUES
                            (@cod, @desc, @preco, @uni, @ts);
                    ";

                    var pCod = cmdUpsert.Parameters.Add("@cod", SqliteType.Text);
                    var pDesc = cmdUpsert.Parameters.Add("@desc", SqliteType.Text);
                    var pPreco = cmdUpsert.Parameters.Add("@preco", SqliteType.Real);
                    var pUni = cmdUpsert.Parameters.Add("@uni", SqliteType.Text);
                    var pTs = cmdUpsert.Parameters.Add("@ts", SqliteType.Text);

                    string agora = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    foreach (var p in produtos)
                    {
                        pCod.Value = p.CodigoBarras;
                        pDesc.Value = p.Descricao ?? string.Empty;
                        pPreco.Value = p.Preco;
                        pUni.Value = p.Unidade ?? string.Empty;
                        pTs.Value = agora;
                        cmdUpsert.ExecuteNonQuery();
                    }

                    tx.Commit();
                    _logger.Info("ProdutoSqliteRepository: sincronização incremental concluída.");
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("ProdutoSqliteRepository.SincronizarProdutos: {Erro}", ex.Message);
                throw;
            }
        }

        [Obsolete("Use SincronizarProdutos para melhor desempenho e atomicidade.")]
        public void SubstituirTodos(IEnumerable<ProdutoCacheEntry> produtos)
        {
            SincronizarProdutos(produtos);
        }

        public ProdutoCacheEntry BuscarPorCodigo(string codigoBarras)
        {
            try
            {
                using var conn = _db.AbrirConexao();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT codigo_barras, descricao, preco, unidade, ultima_atualizacao
                    FROM produtos
                    WHERE codigo_barras = @cod
                    LIMIT 1;
                ";
                cmd.Parameters.AddWithValue("@cod", codigoBarras);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return null;

                return new ProdutoCacheEntry
                {
                    CodigoBarras = reader.GetString(0),
                    Descricao = reader.GetString(1),
                    Preco = reader.GetDecimal(2),
                    Unidade = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    UltimaAtualizacao = reader.GetDateTime(4)
                };
            }
            catch (Exception ex)
            {
                _logger.Warning("ProdutoSqliteRepository.BuscarPorCodigo: {Erro}", ex.Message);
                return null;
            }
        }

        public List<ProdutoCacheEntry> ListarTodos()
        {
            var result = new List<ProdutoCacheEntry>();
            try
            {
                using var conn = _db.AbrirConexao();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT codigo_barras, descricao, preco, unidade, ultima_atualizacao
                    FROM produtos
                    ORDER BY codigo_barras;
                ";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new ProdutoCacheEntry
                    {
                        CodigoBarras = reader.GetString(0),
                        Descricao = reader.GetString(1),
                        Preco = reader.GetDecimal(2),
                        Unidade = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        UltimaAtualizacao = reader.GetDateTime(4)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("ProdutoSqliteRepository.ListarTodos: {Erro}", ex.Message);
            }
            return result;
        }
    }
}
