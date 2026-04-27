using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Application.Services;
using BuscaPreco.CrossCutting;
using BuscaPreco.Infrastructure.Data;
using BuscaPreco.Infrastructure.Repositories;
using BuscaPreco.Infrastructure.Scrapers;
using BuscaPreco.Infrastructure.Services;
using BuscaPreco.Presentation.Electron;
using ElectronNET.API;
using Microsoft.Extensions.Options;
using Serilog;

var basePath = AppDomain.CurrentDomain.BaseDirectory;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseElectron(args);

builder.Configuration.Sources.Clear();
builder.Configuration.SetBasePath(basePath);
builder.Configuration.AddYamlFile("config.yaml", optional: false, reloadOnChange: true);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddRazorPages();

builder.Services.Configure<DbfConfig>(builder.Configuration.GetSection("DbfConfig"));
builder.Services.Configure<TerminalConfig>(builder.Configuration.GetSection("Terminal"));
builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<FeatureConfig>(builder.Configuration.GetSection("Features"));
builder.Services.Configure<ProdutosFixadosConfig>(builder.Configuration.GetSection("ProdutosFixados"));
builder.Services.Configure<AudioConfig>(builder.Configuration.GetSection("AudioConfig"));

builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<DbfConfig>>().Value);
builder.Services.AddSingleton<Logger>();
builder.Services.AddSingleton<YamlConfigWriter>();
builder.Services.AddSingleton<ConsultaDbContext>();
builder.Services.AddSingleton<ProdutoSqliteRepository>();
builder.Services.AddSingleton<ConsultaRepository>();
builder.Services.AddSingleton<ProdutoCacheService>();
builder.Services.AddSingleton<IProdutoCacheService>(sp => sp.GetRequiredService<ProdutoCacheService>());
builder.Services.AddSingleton<AudioService>();
builder.Services.AddSingleton<Servidor>();
builder.Services.AddSingleton<DbfDatabase>(sp =>
{
    var dbfConfig = sp.GetRequiredService<DbfConfig>();
    var logger = sp.GetRequiredService<Logger>();
    return new DbfDatabase(dbfConfig.DbfFilePath, logger);
});
builder.Services.AddSingleton<ITerminalActivityMonitor, TerminalActivityMonitor>();
builder.Services.AddSingleton<IBuscaPrecosService, BuscaPrecosService>();
builder.Services.AddSingleton<IAlertService, WebhookAlertService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddHostedService<RelatorioDiarioBackgroundService>();
builder.Services.AddHostedService<ScreensaverPromocionalBackgroundService>();
builder.Services.AddSingleton<TrayService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

ApiEndpoints.Map(app);

await app.StartAsync();

var trayService = app.Services.GetRequiredService<TrayService>();
await trayService.InitializeAsync();

await app.WaitForShutdownAsync();
Log.CloseAndFlush();
