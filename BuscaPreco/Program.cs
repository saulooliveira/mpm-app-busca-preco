using System;
using System.IO;
using System.Windows.Forms;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Application.Services;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Interfaces;
using BuscaPreco.Infrastructure.Data;
using BuscaPreco.Infrastructure.Repositories;
using BuscaPreco.Infrastructure.Scrapers;
using BuscaPreco.Infrastructure.Services;
using BuscaPreco.Presentation.WindowsForms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace BuscaPreco
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var host = CreateHost();
            host.Start();

            var trayContext = host.Services.GetRequiredService<TrayApplicationContext>();
            Application.Run(trayContext);

            host.StopAsync().GetAwaiter().GetResult();
            host.Dispose();
        }

        private static IHost CreateHost()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;

            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.Sources.Clear();
                    config.SetBasePath(basePath);
                    config.AddYamlFile("config.yaml", optional: false, reloadOnChange: true);
                })
                .UseSerilog((context, _, loggerConfig) =>
                {
                    loggerConfig.ReadFrom.Configuration(context.Configuration);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<DbfConfig>(context.Configuration.GetSection("DbfConfig"));
                    services.Configure<TerminalConfig>(context.Configuration.GetSection("Terminal"));
                    services.Configure<EmailConfig>(context.Configuration.GetSection("Email"));

                    services.AddSingleton(sp => sp.GetRequiredService<IOptions<DbfConfig>>().Value);
                    services.AddSingleton<Logger>();
                    services.AddSingleton<Servidor>();

                    services.AddSingleton<DbfDatabase>(sp =>
                    {
                        var dbfConfig = sp.GetRequiredService<DbfConfig>();
                        var logger = sp.GetRequiredService<Logger>();
                        return new DbfDatabase(dbfConfig.DbfFilePath, logger);
                    });

                    services.AddSingleton<IProdutoRepository, ProdutoRepository>();
                    services.AddSingleton<IBuscaPrecosService, BuscaPrecosService>();
                    services.AddSingleton<IEmailService, EmailService>();
                    services.AddHostedService<RelatorioDiarioBackgroundService>();

                    services.AddTransient<Form1>();
                    services.AddSingleton<Func<Form1>>(sp => () => sp.GetRequiredService<Form1>());
                    services.AddSingleton<TrayApplicationContext>();
                })
                .Build();
        }
    }
}
