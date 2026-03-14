using System;
using System.IO;
using System.Windows.Forms;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Application.Services;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Interfaces;
using BuscaPreco.Infrastructure.Data;
using BuscaPreco.Infrastructure.Repositories;
using BuscaPreco.Presentation.WindowsForms;

namespace BuscaPreco
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var logger = new Logger();
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml");
            var configReader = new ConfigReader(configPath);
            var config = configReader.LoadConfig();

            var dbfDatabase = new DbfDatabase(config.DbfFilePath, logger);
            IProdutoRepository produtoRepository = new ProdutoRepository(dbfDatabase);
            IBuscaPrecosService buscaPrecosService = new BuscaPrecosService(produtoRepository);

            Application.Run(new Form1(logger, buscaPrecosService));
        }
    }
}
