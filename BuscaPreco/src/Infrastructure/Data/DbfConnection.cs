namespace BuscaPreco.Infrastructure.Data
{

    using dBASE.NET;
    using BuscaPreco.CrossCutting;
    using BuscaPreco.Domain.Entities;
    using System;
    using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<string, (string des, decimal vlrVenda1)> _cache
            = new ConcurrentDictionary<string, (string des, decimal vlrVenda1)>();
        private DateTime _ultimaDataModificacao;
        private DbfFileManager dbfFileManager;
        private Logger logger;
        public DbfDatabase(string dbfFilePath, Logger logger)
        {
            this.logger = logger;

            if (!File.Exists(dbfFilePath))
            {
                logger.Info($"Arquivo DBF não encontrado: {dbfFilePath}");
                throw new DbfNotFoundException(dbfFilePath);
            }

            // Crie uma instância do DbfFileManager passando o logger
            dbfFileManager = new DbfFileManager(dbfFilePath, logger);

            // Chame o método para copiar se modificado
            _dbfFilePath = dbfFileManager.CopyIfModified(); 
            _ultimaDataModificacao = File.GetLastWriteTime(_dbfFilePath);
            logger.Info($"ultimaDataModificacao {_ultimaDataModificacao}");
        }

        public (string des, decimal vlrVenda1) BuscarPorCodigo(string cod)
        {
            try
            {
                dbfFileManager.CopyIfModified();


            logger.Info($"BuscarPorCodigo {cod}");
            // Verifica se o arquivo foi modificado
            DateTime dataModificacaoAtual = File.GetLastWriteTime(_dbfFilePath);
            if (dataModificacaoAtual != _ultimaDataModificacao)
            {
                logger.Info($"Limpa o cache se a data de modificação tiver mudado");
                // Limpa o cache se a data de modificação tiver mudado
                _cache.Clear();
                _ultimaDataModificacao = dataModificacaoAtual;
            }

            logger.Info($"Verifica se o valor já está em cache");

            // Verifica se o valor já está em cache
            if (_cache.TryGetValue(cod, out var cachedResult))
            {
                logger.Info($" cachedResult {cachedResult}");

                return cachedResult;  // Retorna o valor do cache
            }



      
                logger.Info($" MakeCache");
                this.MakeCache();
            }
            catch (Exception ex)
            {
                logger.Info($" Erro ao ler o arquivo DBF: {ex.Message}");
                Console.WriteLine("Erro ao ler o arquivo DBF: " + ex.Message);
            }

            _cache.TryGetValue(cod, out var cachedResult2);

            logger.Info($" cachedResult2: {cachedResult2}");

            return cachedResult2;

        }

        public List<Produto> ListarTudo()
        {
            dbfFileManager.CopyIfModified();
            List<Produto> produtos = new List<Produto>();

            logger.Info($" Abrindo o arquivo DB");

            // Abrindo o arquivo DB
            var dbf = new Dbf();
            logger.Info($"Carregar o arquivo DBF {_dbfFilePath}");


            // Carregar o arquivo DBF
            dbf.Read(_dbfFilePath);

            // Percorre os registros para encontrar o COD correspondente
            foreach (var record in dbf.Records)
            {
                if (record["COD"].ToString().Substring(0,2) == "20" && record["COD"].ToString().Length == 5)
                {
                    Produto p = new Produto();
                    p.CodigoItem = record["COD"].ToString().Substring(2);
                    p.Descricao1 = record["DES"].ToString();
                    p.Preco = Convert.ToDecimal(record["VLVENDA1"]);
                    p.Unidade = record["UNI"].ToString();

                    produtos.Add(p); 
                }
            }
            return produtos;
        }

        private void MakeCache()
        {
            string des = null;
            decimal vlrVenda1 = 0;
            logger.Info($" Abrindo o arquivo DB");

            // Abrindo o arquivo DB
            var dbf = new Dbf();
            logger.Info($"Carregar o arquivo DBF {_dbfFilePath}");

             
            // Carregar o arquivo DBF
            dbf.Read(_dbfFilePath);

            // Percorre os registros para encontrar o COD correspondente
            foreach (var record in dbf.Records)
            {
                des = record["DES"].ToString();
                vlrVenda1 = Convert.ToDecimal(record["VLVENDA1"]);

                // Armazena o resultado no cache
                _cache[record["COD"].ToString()] = (des, vlrVenda1);
            }

            logger.Info($"Carregar o arquivo DBF finalizado  COunt {_cache.Count}");
        }

        // Método opcional para limpar o cache manualmente
        public void LimparCache()
        {
            logger.Info($"Limpar Cache");
            _cache.Clear();
        }
    }


}
