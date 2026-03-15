using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;

namespace BuscaPreco.Presentation.WindowsForms
{
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly IBuscaPrecosService _buscaPrecosService;
        private readonly Logger _logger;
    private readonly Func<ConfiguracaoForm> _logFormFactory;
    private ConfiguracaoForm _logForm;

        public TrayApplicationContext(
            IBuscaPrecosService buscaPrecosService,
            Logger logger,
            Func<ConfiguracaoForm> logFormFactory)
        {
            _buscaPrecosService = buscaPrecosService;
            _logger = logger;
            _logFormFactory = logFormFactory;

            var contextMenu = BuildContextMenu();
            _notifyIcon = new NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Text = "BuscaPreço - Monitoramento",
                ContextMenuStrip = contextMenu,
                Visible = true,
                BalloonTipIcon = ToolTipIcon.Info,
                BalloonTipTitle = "BuscaPreço"
            };

            _notifyIcon.DoubleClick += OnNotifyIconDoubleClick;

            _notifyIcon.ShowBalloonTip(
                3000,
                "BuscaPreço iniciado",
                "A aplicação está em execução na bandeja do sistema.",
                ToolTipIcon.Info);
        }

        private ContextMenuStrip BuildContextMenu()
        {
            var contextMenu = new ContextMenuStrip();

            var statusItem = new ToolStripMenuItem("Status do Monitoramento");
            statusItem.Click += (_, __) =>
            {
                _notifyIcon.ShowBalloonTip(
                    3000,
                    "Status do Monitoramento",
                    "Monitoramento ativo e pronto para consultas.",
                    ToolTipIcon.Info);
            };

            var forceSearchItem = new ToolStripMenuItem("Forçar Busca de Preços");
            forceSearchItem.Click += (_, __) => ForcePriceSearch();

            var configItem = new ToolStripMenuItem("Configurações");
            configItem.Click += (_, __) => ShowLogForm();

            var exitItem = new ToolStripMenuItem("Sair");
            exitItem.Click += (_, __) => ExitApplication();

            contextMenu.Items.Add(statusItem);
            contextMenu.Items.Add(forceSearchItem);
            contextMenu.Items.Add(configItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            return contextMenu;
        }

        private void ForcePriceSearch()
        {
            try
            {
                var produtos = _buscaPrecosService.ListarTudo();
                var quantidade = produtos?.Count ?? 0;

                _logger.Info($"Forçar Busca de Preços executado. Itens encontrados: {quantidade}.");

                _notifyIcon.ShowBalloonTip(
                    3000,
                    "Busca de Preços",
                    $"Sincronização concluída. Itens carregados: {quantidade}.",
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao forçar busca de preços: {ex.Message}");
                _notifyIcon.ShowBalloonTip(
                    3000,
                    "Busca de Preços",
                    "Falha ao executar a busca. Consulte o log para detalhes.",
                    ToolTipIcon.Error);
            }
        }

        private void OnNotifyIconDoubleClick(object sender, EventArgs e)
        {
            _notifyIcon.ShowBalloonTip(
                2000,
                "BuscaPreço",
                "Aplicação ativa na bandeja.",
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

        private Icon LoadTrayIcon()
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "buscapreco.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }

            return SystemIcons.Application;
        }

        private void ExitApplication()
        {
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
            }

            base.Dispose(disposing);
        }
    }
}
