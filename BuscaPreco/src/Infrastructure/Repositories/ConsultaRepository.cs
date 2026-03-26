using System;
using System.Collections.Generic;
using BuscaPreco.CrossCutting;
using BuscaPreco.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace BuscaPreco.Infrastructure.Repositories
{
    /// <summary>
    /// Persiste e consulta registros de auditoria de consultas no SQLite.
    /// Todos os métodos são fire-and-forget safe: exceções são capturadas e logadas,
    /// nunca propagadas para o fluxo principal de negócio.
    /// </summary>
    public class ConsultaRepository
    {
        private readonly ConsultaDbContext _db;
        private readonly Logger _logger;

        public ConsultaRepository(ConsultaDbContext db, Logger logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Grava uma consulta na tabela consultas. Nunca lança exceção.
        /// </summary>
        public void Gravar(string codigoBarras, string nome, string preco, bool encontrado, string origem)
        {
            try
            {
                using var conn = _db.AbrirConexao();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO consultas (data_hora, codigo_barras, nome, preco, status, origem)
                    VALUES (@ts, @cod, @nome, @preco, @status, @origem);
                ";
                
                // Tenta converter o preço para decimal para armazenamento correto
                decimal precoDecimal = 0;
                decimal.TryParse(preco, out precoDecimal);

                cmd.Parameters.AddWithValue("@ts", DateTime.Now);
                cmd.Parameters.AddWithValue("@cod", codigoBarras);
                cmd.Parameters.AddWithValue("@nome", nome ?? string.Empty);
                cmd.Parameters.AddWithValue("@preco", precoDecimal);
                cmd.Parameters.AddWithValue("@status", encontrado ? "Encontrado" : "Não Cadastrado");
                cmd.Parameters.AddWithValue("@origem", origem ?? string.Empty);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.Warning("ConsultaRepository.Gravar: {Erro}", ex.Message);
            }
        }

        /// <summary>
        /// Retorna total, encontrados e não cadastrados no período [inicio, fim] inclusive.
        /// </summary>
        public (int total, int encontrados, int naoCadastrados) ResumoNoPeriodo(DateTime inicio, DateTime fim)
        {
            try
            {
                using var conn = _db.AbrirConexao();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        COUNT(*) AS total,
                        SUM(CASE WHEN status = 'Encontrado'     THEN 1 ELSE 0 END) AS encontrados,
                        SUM(CASE WHEN status = 'Não Cadastrado' THEN 1 ELSE 0 END) AS nao_cadastrados
                    FROM consultas
                    WHERE data_hora >= @inicio
                      AND data_hora <  @fim_exclusivo;
                ";
                cmd.Parameters.AddWithValue("@inicio", inicio.Date);
                cmd.Parameters.AddWithValue("@fim_exclusivo", fim.Date.AddDays(1));
                using var r = cmd.ExecuteReader();
                if (!r.Read())
                {
                    return (0, 0, 0);
                }

                return (
                    r.IsDBNull(0) ? 0 : r.GetInt32(0),
                    r.IsDBNull(1) ? 0 : r.GetInt32(1),
                    r.IsDBNull(2) ? 0 : r.GetInt32(2));
            }
            catch (Exception ex)
            {
                _logger.Warning("ConsultaRepository.ResumoNoPeriodo: {Erro}", ex.Message);
                return (0, 0, 0);
            }
        }

        /// <summary>
        /// Top produtos encontrados por quantidade de consultas no período.
        /// Retorna lista de (codigo, nome, quantidade) ordenada DESC.
        /// </summary>
        public List<(string codigo, string nome, int qtd)> TopProdutos(DateTime inicio, DateTime fim, int top = 200)
        {
            var result = new List<(string, string, int)>();
            try
            {
                using var conn = _db.AbrirConexao();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT   codigo_barras,
                             MAX(nome)  AS nome,
                             COUNT(*)   AS qtd
                    FROM     consultas
                    WHERE    data_hora >= @inicio
                      AND    data_hora <  @fim_exclusivo
                      AND    status    =  'Encontrado'
                    GROUP BY codigo_barras
                    ORDER BY qtd DESC
                    LIMIT    @top;
                ";
                cmd.Parameters.AddWithValue("@inicio", inicio.Date);
                cmd.Parameters.AddWithValue("@fim_exclusivo", fim.Date.AddDays(1));
                cmd.Parameters.AddWithValue("@top", top);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    result.Add((r.GetString(0), r.GetString(1), r.GetInt32(2)));
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("ConsultaRepository.TopProdutos: {Erro}", ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Retorna array[24] com contagem de consultas por hora do dia (0=00h … 23=23h).
        /// </summary>
        public int[] ConsultasPorHora(DateTime inicio, DateTime fim)
        {
            var result = new int[24];
            try
            {
                using var conn = _db.AbrirConexao();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT CAST(strftime('%H', data_hora) AS INTEGER) AS hora,
                           COUNT(*) AS qtd
                    FROM   consultas
                    WHERE  data_hora >= @inicio
                      AND  data_hora <  @fim_exclusivo
                    GROUP  BY hora;
                ";
                cmd.Parameters.AddWithValue("@inicio", inicio.Date);
                cmd.Parameters.AddWithValue("@fim_exclusivo", fim.Date.AddDays(1));
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    int h = r.GetInt32(0);
                    if (h >= 0 && h < 24)
                    {
                        result[h] = r.GetInt32(1);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("ConsultaRepository.ConsultasPorHora: {Erro}", ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Retorna array[7] com contagem por dia da semana.
        /// SQLite strftime('%w'): 0=Domingo, 1=Segunda … 6=Sábado.
        /// </summary>
        public int[] ConsultasPorDiaSemana(DateTime inicio, DateTime fim)
        {
            var result = new int[7];
            try
            {
                using var conn = _db.AbrirConexao();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT CAST(strftime('%w', data_hora) AS INTEGER) AS dia,
                           COUNT(*) AS qtd
                    FROM   consultas
                    WHERE  data_hora >= @inicio
                      AND  data_hora <  @fim_exclusivo
                    GROUP  BY dia;
                ";
                cmd.Parameters.AddWithValue("@inicio", inicio.Date);
                cmd.Parameters.AddWithValue("@fim_exclusivo", fim.Date.AddDays(1));
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    int d = r.GetInt32(0);
                    if (d >= 0 && d < 7)
                    {
                        result[d] = r.GetInt32(1);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("ConsultaRepository.ConsultasPorDiaSemana: {Erro}", ex.Message);
            }

            return result;
        }
    }
}
