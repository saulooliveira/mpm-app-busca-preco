using System;
using System.Collections;
using System.Globalization;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using BuscaPreco.Infrastructure.Terminal;
using BuscaPreco.Infrastructure.Services;

namespace BuscaPreco.Presentation.WindowsForms
{
    public class TrayApplicationContext : ApplicationContext
    {
        // ── Constantes de UI ─────────────────────────────────────────────────

        private const string AppName                    = "BuscaPreço";
        private const string TrayTooltip               = "BuscaPreço - Monitoramento";
        private const string BalloonTitleApp            = "BuscaPreço";

        private const string BalloonTitleIniciado       = "BuscaPreço iniciado";
        private const string BalloonMsgIniciado         = "A aplicação está em execução na bandeja do sistema.";

        private const string MenuStatusTitle            = "Status do Monitoramento";
        private const string BalloonMsgMonitoramentoAtivo = "Monitoramento ativo e pronto para consultas.";

        private const string MenuForcaBusca             = "Forçar Busca de Preços";
        private const string MenuConfiguracoes          = "Configurações";
        private const string MenuRelatorio              = "Relatório de Consultas";
        private const string MenuSair                   = "Sair";

        private const string BalloonTitleBusca          = "Busca de Preços";
        private const string BalloonMsgSincronizado     = "Sincronização concluída. Itens no cache: {0}.";
        private const string BalloonMsgFalhaBusca       = "Falha ao executar a busca. Consulte o log para detalhes.";

        private const string BalloonMsgAppAtiva         = "Aplicação ativa na bandeja.";

        private const string TrayIconFileName           = "buscapreco.ico";
        private const string TrayIconAssetsFolder       = "Assets";

        private const int    BalloonTipDuracaoMs        = 3000;
        private const int    BalloonTipDuracaoCurtaMs   = 2000;

        private const string PrecoFormat               = "0.00";

        // ── Campos ──────────────────────────────────────────────────────────

        private readonly NotifyIcon _notifyIcon;
        private readonly IBuscaPrecosService _buscaPrecosService;
        private readonly Logger _logger;
        private readonly Servidor _servidor;
        private readonly Func<ConfiguracaoForm> _logFormFactory;
        private readonly ArrayList _terminaisConectados;
        private readonly AudioService _audioService;
        private readonly IProdutoCacheService _produtoCacheService;
        private ConfiguracaoForm _logForm;
        private RelatorioForm _relatorioForm;
        private readonly Func<RelatorioForm> _relatorioFormFactory;

        public TrayApplicationContext(
            IBuscaPrecosService buscaPrecosService,
            Logger logger,
            Servidor servidor,
            Func<ConfiguracaoForm> logFormFactory,
            Func<RelatorioForm> relatorioFormFactory,
            AudioService audioService,
            IProdutoCacheService produtoCacheService)
        {
            _buscaPrecosService  = buscaPrecosService;
            _logger              = logger;
            _servidor            = servidor;
            _logFormFactory      = logFormFactory;
            _terminaisConectados = new ArrayList();
            _audioService        = audioService;
            _produtoCacheService = produtoCacheService;
            _relatorioFormFactory = relatorioFormFactory;

            InitializeServer();

            var contextMenu = BuildContextMenu();
            _notifyIcon = new NotifyIcon
            {
                Icon              = LoadTrayIcon(),
                Text              = TrayTooltip,
                ContextMenuStrip  = contextMenu,
                Visible           = true,
                BalloonTipIcon    = ToolTipIcon.Info,
                BalloonTipTitle   = BalloonTitleApp
            };

            _notifyIcon.DoubleClick += OnNotifyIconDoubleClick;

            _notifyIcon.ShowBalloonTip(
                BalloonTipDuracaoMs,
                BalloonTitleIniciado,
                BalloonMsgIniciado,
                ToolTipIcon.Info);
        }

        private void InitializeServer()
        {
            _servidor.onChange   += OnChangeList;
            _servidor.onReceive  += OnReceiveData;
            _servidor.Start();

            _logger.Info("Servidor Gertec inicializado no contexto da bandeja.");
        }

        private void OnChangeList(ArrayList novaLista)
        {
            _terminaisConectados.Clear();
            _terminaisConectados.AddRange(novaLista);

            _logger.Info("Lista de terminais atualizada. Total conectados: {Total}", _terminaisConectados.Count);
        }

        private void OnReceiveData(object sender, string codigoRecebido)
        {
            try
            {
                var codigo = NormalizeCodigo(codigoRecebido);
                _logger.Info("Auditoria terminal: consulta recebida. CodigoBarras={CodigoBarras}", codigo);

                var (descricao, preco) = _buscaPrecosService.BuscarPorCodigo(codigo);
                var terminal = sender as Terminal;

                if (terminal == null)
                {
                    _logger.Warning("Evento de recebimento sem terminal válido para resposta. CodigoBarras={CodigoBarras}", codigo);
                    return;
                }

                if (string.IsNullOrWhiteSpace(descricao))
                {
                    terminal.SendProdNFound();
                    _logger.Info("Produto não encontrado para o código {CodigoBarras}.", codigo);
                    return;
                }

                var precoFormatado = preco.ToString(PrecoFormat, CultureInfo.InvariantCulture);
                var wavBytes = _audioService.IsEnabled ? _audioService.GetWavBytes() : null;

                if (wavBytes != null && terminal.IsG2SComAudio)
                {
                    terminal.SendPlayAudioWithMessage(
                        wavBytes,
                        _audioService.DuracaoSegundos,
                        _audioService.Volume,
                        descricao,
                        precoFormatado);
                    _logger.Info("Resposta com áudio enviada. CodigoBarras={CodigoBarras} Descricao={Descricao} Preco={Preco}",
                        codigo, descricao, preco);
                }
                else
                {
                    terminal.SendProcPrice(descricao, precoFormatado);
                    _logger.Info("Resposta enviada ao terminal. CodigoBarras={CodigoBarras} Descricao={Descricao} Preco={Preco}",
                        codigo, descricao, preco);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Erro ao processar dado recebido do terminal: {Erro}", ex.Message);
            }
        }

        private static string NormalizeCodigo(string codigoRecebido)
        {
            return (codigoRecebido ?? string.Empty)
                .Trim('\0', ' ', '\r', '\n')
                .TrimStart('#');
        }

        private ContextMenuStrip BuildContextMenu()
        {
            var contextMenu = new ContextMenuStrip();

            var statusItem = new ToolStripMenuItem(MenuStatusTitle);
            statusItem.Click += (_, __) =>
            {
                _notifyIcon.ShowBalloonTip(
                    BalloonTipDuracaoMs,
                    MenuStatusTitle,
                    BalloonMsgMonitoramentoAtivo,
                    ToolTipIcon.Info);
            };

            var forceSearchItem = new ToolStripMenuItem(MenuForcaBusca);
            forceSearchItem.Click += (_, __) => ForcePriceSearch();

            var configItem = new ToolStripMenuItem(MenuConfiguracoes);
            configItem.Click += (_, __) => ShowLogForm();

            var relatorioItem = new ToolStripMenuItem(MenuRelatorio);
            relatorioItem.Click += (_, __) => ShowRelatorioForm();

            var exitItem = new ToolStripMenuItem(MenuSair);
            exitItem.Click += (_, __) => ExitApplication();

            contextMenu.Items.Add(statusItem);
            contextMenu.Items.Add(forceSearchItem);
            contextMenu.Items.Add(configItem);
            contextMenu.Items.Add(relatorioItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            return contextMenu;
        }

        private void ForcePriceSearch()
        {
            try
            {
                _produtoCacheService.SincronizarAgora();

                var quantidade = _buscaPrecosService.ListarTudo().Count;
                _logger.Info("Forçar Busca de Preços executado. Itens no cache: {Quantidade}.", quantidade);

                _notifyIcon.ShowBalloonTip(
                    BalloonTipDuracaoMs,
                    BalloonTitleBusca,
                    string.Format(BalloonMsgSincronizado, quantidade),
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _logger.Error("Erro ao forçar busca de preços: {Message}", ex.Message);
                _notifyIcon.ShowBalloonTip(
                    BalloonTipDuracaoMs,
                    BalloonTitleBusca,
                    BalloonMsgFalhaBusca,
                    ToolTipIcon.Error);
            }
        }

        private void OnNotifyIconDoubleClick(object sender, EventArgs e)
        {
            _notifyIcon.ShowBalloonTip(
                BalloonTipDuracaoCurtaMs,
                BalloonTitleApp,
                BalloonMsgAppAtiva,
                ToolTipIcon.Info);

            ShowConfiguracaoForm();
        }

        private void ShowLogForm()
        {
            // kept for compatibility
            ShowConfiguracaoForm();
        }

        private void ShowConfiguracaoForm()
        {
            if (_logForm == null || _logForm.IsDisposed)
            {
                _logForm = _logFormFactory();
                _logForm.FormClosed += (_, __) => _logForm = null;
            }

            if (!_logForm.Visible)
            {
                _logForm.Show();
            }

            _logForm.WindowState = FormWindowState.Normal;
            _logForm.BringToFront();
            _logForm.Activate();
        }

        private void ShowRelatorioForm()
        {
            if (_relatorioForm == null || _relatorioForm.IsDisposed)
            {
                _relatorioForm = _relatorioFormFactory();
                _relatorioForm.FormClosed += (_, __) => _relatorioForm = null;
            }

            if (!_relatorioForm.Visible)
            {
                _relatorioForm.Show();
            }

            _relatorioForm.WindowState = FormWindowState.Normal;
            _relatorioForm.BringToFront();
            _relatorioForm.Activate();
        }

        private static Icon LoadTrayIcon()
        {
            var iconPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                TrayIconAssetsFolder,
                TrayIconFileName);

            return File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;
        }

        private void ExitApplication()
        {
            _servidor.Stop();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notifyIcon?.Dispose();
                _logForm?.Dispose();
                _relatorioForm?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
