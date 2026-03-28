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

        public void SubstituirTodos(IEnumerable<ProdutoCacheEntry> produtos)
        {
            try
            {
                using var conn = _db.AbrirConexao();
                using var tx = conn.BeginTransaction();
                try
                {
                    using var cmdDelete = conn.CreateCommand();
                    cmdDelete.Transaction = tx;
                    cmdDelete.CommandText = "DELETE FROM produtos;";
                    cmdDelete.ExecuteNonQuery();

                    using var cmdInsert = conn.CreateCommand();
                    cmdInsert.Transaction = tx;
                    cmdInsert.CommandText = @"
                        INSERT OR REPLACE INTO produtos
                            (codigo_barras, descricao, preco, unidade, ultima_atualizacao)
                        VALUES
                            (@cod, @desc, @preco, @uni, @ts);
                    ";

                    var pCod = cmdInsert.Parameters.Add("@cod", SqliteType.Text);
                    var pDesc = cmdInsert.Parameters.Add("@desc", SqliteType.Text);
                    var pPreco = cmdInsert.Parameters.Add("@preco", SqliteType.Real);
                    var pUni = cmdInsert.Parameters.Add("@uni", SqliteType.Text);
                    var pTs = cmdInsert.Parameters.Add("@ts", SqliteType.Text);

                    string agora = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    foreach (var p in produtos)
                    {
                        if (string.IsNullOrWhiteSpace(p.CodigoBarras)) continue;

                        pCod.Value = p.CodigoBarras;
                        pDesc.Value = p.Descricao ?? string.Empty;
                        pPreco.Value = (double)p.Preco;
                        pUni.Value = p.Unidade ?? string.Empty;
                        pTs.Value = agora;
                        cmdInsert.ExecuteNonQuery();
                    }

                    tx.Commit();
                    _logger.Info("ProdutoSqliteRepository: tabela produtos sincronizada.");
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("ProdutoSqliteRepository.SubstituirTodos: {Erro}", ex.Message);
                throw;
            }
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
                       OR codigo_barras LIKE @sufixo
                    ORDER BY
                        CASE WHEN codigo_barras = @cod THEN 0 ELSE 1 END
                    LIMIT 1;
                ";
                cmd.Parameters.AddWithValue("@cod", codigoBarras);
                cmd.Parameters.AddWithValue("@sufixo", "%" + codigoBarras);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return null;

                return new ProdutoCacheEntry
                {
                    CodigoBarras = reader.GetString(0),
                    Descricao = reader.GetString(1),
                    Preco = (decimal)reader.GetDouble(2),
                    Unidade = reader.GetString(3),
                    UltimaAtualizacao = DateTime.Parse(reader.GetString(4))
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
                        Preco = (decimal)reader.GetDouble(2),
                        Unidade = reader.GetString(3),
                        UltimaAtualizacao = DateTime.Parse(reader.GetString(4))
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
