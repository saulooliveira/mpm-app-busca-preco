using System.Globalization;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using BuscaPreco.Infrastructure.Scrapers;
using BuscaPreco.Infrastructure.Services;
using ElectronNET.API;
using ElectronNET.API.Entities;

namespace BuscaPreco.Presentation.Electron;

public class TrayService
{
    private readonly IBuscaPrecosService _buscaPrecosService;
    private readonly Logger _logger;
    private readonly Servidor _servidor;
    private readonly AudioService _audioService;
    private readonly IProdutoCacheService _produtoCacheService;

    public TrayService(
        IBuscaPrecosService buscaPrecosService,
        Logger logger,
        Servidor servidor,
        AudioService audioService,
        IProdutoCacheService produtoCacheService)
    {
        _buscaPrecosService = buscaPrecosService;
        _logger = logger;
        _servidor = servidor;
        _audioService = audioService;
        _produtoCacheService = produtoCacheService;
    }

    public async Task InitializeAsync()
    {
        _servidor.onReceive += OnReceiveData;
        _servidor.onChange += lista =>
            _logger.Info("Terminais conectados: {Total}", lista.Count);
        _servidor.Start();
        _logger.Info("Servidor Gertec inicializado.");

        if (!HybridSupport.IsElectronActive) return;

        // Search for icon: .ico preferred, .png as fallback
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "buscapreco.ico");
        if (!File.Exists(iconPath))
            iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "buscapreco.png");
        if (!File.Exists(iconPath))
            iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "favicon.ico");

        var menu = new[]
        {
            new MenuItem
            {
                Label = "Relatório de Consultas",
                Click = () => _ = AbrirJanelaAsync("/relatorio", "Relatório", 1100, 760)
            },
            new MenuItem
            {
                Label = "Configurações",
                Click = () => _ = AbrirJanelaAsync("/configuracao", "Configurações", 960, 680)
            },
            new MenuItem { Type = MenuType.separator },
            new MenuItem
            {
                Label = "Sincronizar Preços",
                Click = ForcePriceSearch
            },
            new MenuItem { Type = MenuType.separator },
            new MenuItem
            {
                Label = "Sair",
                Click = () => Electron.App.Quit()
            }
        };

        await Electron.Tray.Show(iconPath, menu);
        Electron.Tray.SetToolTip("BuscaPreço — Monitoramento");
    }

    private async Task AbrirJanelaAsync(string path, string titulo, int width, int height)
    {
        var options = new BrowserWindowOptions
        {
            Width = width,
            Height = height,
            Title = $"BuscaPreço — {titulo}",
            AutoHideMenuBar = true,
            Show = false
        };
        var win = await Electron.WindowManager.CreateWindowAsync(options);
        win.OnReadyToShow += () => win.Show();
        await win.LoadURL($"http://localhost:{BridgeSettings.WebPort}{path}");
    }

    private void ForcePriceSearch()
    {
        try
        {
            _produtoCacheService.SincronizarAgora();
            _logger.Info("Sincronização forçada executada.");
        }
        catch (Exception ex)
        {
            _logger.Error("Erro ao sincronizar: {Erro}", ex.Message);
        }
    }

    private void OnReceiveData(object sender, string codigoRecebido)
    {
        try
        {
            var codigo = (codigoRecebido ?? string.Empty)
                .Trim('\0', ' ', '\r', '\n')
                .TrimStart('#');

            _logger.Info("Consulta recebida. CodigoBarras={CodigoBarras}", codigo);

            var (descricao, preco) = _buscaPrecosService.BuscarPorCodigo(codigo);
            var terminal = sender as Terminal;

            if (terminal == null)
            {
                _logger.Warning("Evento sem terminal válido. CodigoBarras={CodigoBarras}", codigo);
                return;
            }

            if (string.IsNullOrWhiteSpace(descricao))
            {
                terminal.SendProdNFound();
                return;
            }

            var precoFormatado = preco.ToString("0.00", CultureInfo.InvariantCulture);
            var wavBytes = _audioService.IsEnabled ? _audioService.GetWavBytes() : null;

            if (wavBytes != null && terminal.IsG2SComAudio)
                terminal.SendPlayAudioWithMessage(
                    wavBytes, _audioService.DuracaoSegundos, _audioService.Volume,
                    descricao, precoFormatado);
            else
                terminal.SendProcPrice(descricao, precoFormatado);
        }
        catch (Exception ex)
        {
            _logger.Error("Erro ao processar consulta do terminal: {Erro}", ex.Message);
        }
    }
}
