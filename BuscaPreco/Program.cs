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
using Microsoft.Extensions.DependencyInjection;

namespace BuscaPreco
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var serviceProvider = ConfigureServices())
            {
                var trayContext = serviceProvider.GetRequiredService<TrayApplicationContext>();
                Application.Run(trayContext);
            }
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml");

            services.AddSingleton(new ConfigReader(configPath));
            services.AddSingleton(sp => sp.GetRequiredService<ConfigReader>().LoadConfig());
            services.AddSingleton<Logger>();

            services.AddSingleton<DbfDatabase>(sp =>
            {
                var dbfConfig = sp.GetRequiredService<DbfConfig>();
                var logger = sp.GetRequiredService<Logger>();
                return new DbfDatabase(dbfConfig.DbfFilePath, logger);
            });

            services.AddSingleton<IProdutoRepository, ProdutoRepository>();
            services.AddSingleton<IBuscaPrecosService, BuscaPrecosService>();

            services.AddTransient<Form1>();
            services.AddSingleton<Func<Form1>>(sp => () => sp.GetRequiredService<Form1>());
            services.AddSingleton<TrayApplicationContext>();

            return services.BuildServiceProvider();
        }
    }
}
