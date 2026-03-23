namespace BuscaPreco.Infrastructure.Database
{

    using dBASE.NET;
    using BuscaPreco.CrossCutting;
    using BuscaPreco.Infrastructure.Database.Exceptions;
    using BuscaPreco.Domain.Entities;
    using System;
    using System.Collections.Generic;
    using System.IO;
    public class DbfFileManager
    {
        private readonly string _sourceDbfPath;
        private readonly string _destinationDbfPath;
        private readonly Logger _logger; // Instância do Logger

        public DbfFileManager(string sourceDbfPath, Logger logger)
        {
            _sourceDbfPath = sourceDbfPath;
            // Define o caminho de destino na pasta da aplicação
            _destinationDbfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(sourceDbfPath));
            _logger = logger; // Atribui o logger passado
        }

        public String CopyIfModified()
        {
            // Verifica se o arquivo de origem existe
            if (!File.Exists(_sourceDbfPath))
            {
                _logger.Error("O arquivo DBF de origem não existe.");
                return _destinationDbfPath;
            }

            // Obtém a data de modificação do arquivo de origem
            DateTime sourceLastModified = File.GetLastWriteTime(_sourceDbfPath);

            // Se o arquivo de destino não existir, copia o arquivo de origem
            if (!File.Exists(_destinationDbfPath))
            {
                File.Copy(_sourceDbfPath, _destinationDbfPath);
                _logger.Info("Arquivo copiado para a pasta da aplicação.");
                return _destinationDbfPath;
            }

            // Obtém a data de modificação do arquivo de destino
            DateTime destinationLastModified = File.GetLastWriteTime(_destinationDbfPath);

            // Verifica se a data de modificação do arquivo de origem é diferente da do destino
            if (sourceLastModified > destinationLastModified)
            {
                File.Copy(_sourceDbfPath, _destinationDbfPath, true); // Substitui o arquivo de destino
                _logger.Info("Arquivo atualizado na pasta da aplicação.");
            }
            else
            {
                _logger.Info("O arquivo na pasta da aplicação está atualizado.");
            }
            return _destinationDbfPath;
        }
    }


    public class DbfDatabase
    {
        private readonly string _dbfFilePath;
        private readonly DbfFileManager _dbfFileManager;
        private readonly Logger _logger;

        public DbfDatabase(string dbfFilePath, Logger logger)
        {
            _logger = logger;

            if (!File.Exists(dbfFilePath))
            {
                logger.Info($"Arquivo DBF não encontrado: {dbfFilePath}");
                throw new DbfNotFoundException(dbfFilePath);
            }

            _dbfFileManager = new DbfFileManager(dbfFilePath, logger);
            _dbfFilePath = _dbfFileManager.CopyIfModified();
        }

        public (string des, decimal vlrVenda1) BuscarPorCodigo(string cod)
        {
            _dbfFileManager.CopyIfModified();

            try
            {
                var dbf = new Dbf();
                dbf.Read(_dbfFilePath);

                foreach (var record in dbf.Records)
                {
                    var codigo = record["COD"].ToString();
                    if (string.Equals(codigo, cod, StringComparison.OrdinalIgnoreCase))
                    {
                        var des = record["DES"].ToString();
                        var vlrVenda1 = Convert.ToDecimal(record["VLVENDA1"]);
                        return (des, vlrVenda1);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Info($"Erro ao ler o arquivo DBF: {ex.Message}");
            }

            return (string.Empty, 0m);
        }

        public List<Produto> ListarTudo()
        {
            _dbfFileManager.CopyIfModified();
            var produtos = new List<Produto>();

            var dbf = new Dbf();
            dbf.Read(_dbfFilePath);

            foreach (var record in dbf.Records)
            {
                if (record["COD"].ToString().Substring(0, 2) == "20" && record["COD"].ToString().Length == 5)
                {
                    var p = new Produto
                    {
                        CodigoItem = record["COD"].ToString().Substring(2),
                        Descricao1 = record["DES"].ToString(),
                        Preco = Convert.ToDecimal(record["VLVENDA1"]),
                        Unidade = record["UNI"].ToString()
                    };

                    produtos.Add(p);
                }
            }

            return produtos;
        }
    }


}
