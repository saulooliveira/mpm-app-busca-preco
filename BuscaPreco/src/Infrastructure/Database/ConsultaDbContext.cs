using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace BuscaPreco.Infrastructure.Database
{
    public class ConsultaDbContext
    {
        private readonly string _dbPath;

        public ConsultaDbContext()
        {
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "buscapreco.db");
            InicializarSchema();
        }

        public SqliteConnection AbrirConexao()
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            return conn;
        }

        private void InicializarSchema()
        {
            using var conn = AbrirConexao();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS consultas (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    data_hora TEXT NOT NULL,
    codigo_barras TEXT NOT NULL,
    nome TEXT NOT NULL DEFAULT '',
    preco TEXT NOT NULL DEFAULT '',
    status TEXT NOT NULL DEFAULT '',
    origem TEXT NOT NULL DEFAULT ''
);

CREATE INDEX IF NOT EXISTS idx_consultas_codigo
    ON consultas (codigo_barras);

CREATE TABLE IF NOT EXISTS produtos (
    codigo_barras     TEXT    PRIMARY KEY,
    descricao         TEXT    NOT NULL DEFAULT '',
    preco             REAL    NOT NULL DEFAULT 0,
    unidade           TEXT    NOT NULL DEFAULT '',
    ultima_atualizacao TEXT   NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_produtos_descricao
    ON produtos (descricao);
";
            cmd.ExecuteNonQuery();
        }
    }
}
