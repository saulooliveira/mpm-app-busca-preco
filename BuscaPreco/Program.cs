using System;
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
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            var host = CreateHost();
            host.Start();

            var trayContext = host.Services.GetRequiredService<TrayApplicationContext>();
            System.Windows.Forms.Application.Run(trayContext);

            host.StopAsync().GetAwaiter().GetResult();
            host.Dispose();
            Log.CloseAndFlush();
        }

        private static IHost CreateHost()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddYamlFile("config.yaml", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.Sources.Clear();
                    config.SetBasePath(basePath);
                    config.AddConfiguration(configuration);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<DbfConfig>(options =>
                        context.Configuration.GetSection("DbfConfig").Bind(options));
                    services.Configure<TerminalConfig>(options =>
                        context.Configuration.GetSection("Terminal").Bind(options));
                    services.Configure<EmailConfig>(options =>
                        context.Configuration.GetSection("Email").Bind(options));

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
                    services.AddSingleton<IProdutoCacheTracker, ProdutoCacheTracker>();
                    services.AddSingleton<ITerminalActivityMonitor, TerminalActivityMonitor>();
                    services.AddSingleton<IBuscaPrecosService, BuscaPrecosService>();
                    services.AddSingleton<IAlertService, WebhookAlertService>();
                    services.AddSingleton<ITerminalDisplayService, TerminalDisplayService>();
                    services.AddSingleton<IEmailService, EmailService>();
                    services.AddHostedService<RelatorioDiarioBackgroundService>();
                    services.AddHostedService<ScreensaverPromocionalBackgroundService>();

                    services.AddTransient<Form1>();
                    services.AddSingleton<Func<Form1>>(sp => () => sp.GetRequiredService<Form1>());
                    services.AddSingleton<TrayApplicationContext>();
                })
                .Build();
        }
    }
}
