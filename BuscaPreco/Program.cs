using System;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Application.Services;
using BuscaPreco.CrossCutting;
using BuscaPreco.Infrastructure.Config;
using BuscaPreco.Infrastructure.Database;
using BuscaPreco.Infrastructure.Database.Exceptions;
using BuscaPreco.Infrastructure.Repositories;
using BuscaPreco.Infrastructure.Terminal;
using BuscaPreco.Infrastructure.Services;
#if DEBUG
using BuscaPreco.Infrastructure.Mock;
#endif
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

            IHost host;

            try
            {
                host = CreateHost();
                host.Start();
            }
            catch (DbfNotFoundException ex)
            {
                string msg = "N\u00e3o foi poss\u00edvel iniciar o BuscaPreco.\n\n" +
                             "Arquivo de cadastro n\u00e3o encontrado:\n" + ex.FilePath + "\n\n" +
                             "Verifique o caminho em config.yaml e tente novamente.";
                System.Windows.Forms.MessageBox.Show(
                    msg,
                    "BuscaPreco \u2013 Erro de inicializa\u00e7\u00e3o",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }
            catch (Exception ex)
            {
                string msg = "Erro inesperado ao iniciar o BuscaPreco:\n\n" + ex.Message;
                System.Windows.Forms.MessageBox.Show(
                    msg,
                    "BuscaPreco \u2013 Erro",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

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
                    // ── Configurations (shared) ──────────────────────────────
                    services.Configure<DbfConfig>(context.Configuration.GetSection("DbfConfig"));
                    services.Configure<TerminalConfig>(context.Configuration.GetSection("Terminal"));
                    services.Configure<EmailConfig>(context.Configuration.GetSection("Email"));
                    services.Configure<FeatureConfig>(context.Configuration.GetSection("Features"));
                    services.Configure<ProdutosFixadosConfig>(context.Configuration.GetSection("ProdutosFixados"));
                    services.Configure<AudioConfig>(context.Configuration.GetSection("AudioConfig"));

                    services.AddSingleton<Logger>();

#if DEBUG
                    // ── MOCK MODE ────────────────────────────────────────────
                    // Infra fisica (DBF, SQLite, socket, email, webhook)
                    // substituida por implementacoes em memoria.
                    Log.Information("[MOCK MODE] Build Debug — registrando servicos mock.");

                    services.AddSingleton<IProdutoCacheService,      MockProdutoCacheService>();
                    services.AddSingleton<IBuscaPrecosService,       MockBuscaPrecosService>();
                    services.AddSingleton<IAlertService,             MockAlertService>();
                    services.AddSingleton<IEmailService,             MockEmailService>();
                    services.AddSingleton<ITerminalDisplayService,   MockTerminalDisplayService>();
                    services.AddSingleton<MockConsultaRepository>();
                    services.AddSingleton<ConsultaRepository>(sp =>  sp.GetRequiredService<MockConsultaRepository>());
                    services.AddSingleton<MockServidor>();
                    services.AddSingleton<Servidor>(sp =>            sp.GetRequiredService<MockServidor>());
                    services.AddSingleton<ITerminalActivityMonitor,  TerminalActivityMonitor>();
                    services.AddSingleton<AudioService>();
                    services.AddSingleton<YamlConfigWriter>();
#else
                    // ── PRODUCAO ─────────────────────────────────────────────
                    services.AddSingleton(sp => sp.GetRequiredService<IOptions<DbfConfig>>().Value);
                    services.AddSingleton<YamlConfigWriter>();
                    services.AddSingleton<ConsultaDbContext>();
                    services.AddSingleton<ProdutoSqliteRepository>();
                    services.AddSingleton<ConsultaRepository>();
                    services.AddSingleton<ProdutoCacheService>();
                    services.AddSingleton<IProdutoCacheService>(sp => sp.GetRequiredService<ProdutoCacheService>());
                    services.AddSingleton<AudioService>();
                    services.AddSingleton<Servidor>();

                    services.AddSingleton<DbfDatabase>(sp =>
                    {
                        var dbfConfig = sp.GetRequiredService<DbfConfig>();
                        var logger    = sp.GetRequiredService<Logger>();
                        return new DbfDatabase(dbfConfig.DbfFilePath, logger);
                    });

                    services.AddSingleton<ITerminalActivityMonitor,  TerminalActivityMonitor>();
                    services.AddSingleton<IBuscaPrecosService,       BuscaPrecosService>();
                    services.AddSingleton<IAlertService,             WebhookAlertService>();
                    services.AddSingleton<ITerminalDisplayService,   TerminalDisplayService>();
                    services.AddSingleton<IEmailService,             EmailService>();
#endif

                    // ── Hosted services e UI (comuns) ────────────────────────
                    services.AddHostedService<RelatorioDiarioBackgroundService>();
                    services.AddHostedService<ScreensaverPromocionalBackgroundService>();

                    services.AddTransient<ConfiguracaoForm>();
                    services.AddSingleton<Func<ConfiguracaoForm>>(sp => () => sp.GetRequiredService<ConfiguracaoForm>());
                    services.AddTransient<RelatorioForm>();
                    services.AddSingleton<Func<RelatorioForm>>(sp => () => sp.GetRequiredService<RelatorioForm>());
                    services.AddSingleton<TrayApplicationContext>();
                })
                .Build();
        }
    }
}
