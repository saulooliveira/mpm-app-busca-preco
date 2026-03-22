using System;
using System.Collections;
using System.Globalization;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using BuscaPreco.Infrastructure.Scrapers;
using BuscaPreco.Infrastructure.Services;

namespace BuscaPreco.Presentation.WindowsForms
{
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly IBuscaPrecosService _buscaPrecosService;
        private readonly Logger _logger;
        private readonly Servidor _servidor;
        private readonly Func<ConfiguracaoForm> _logFormFactory;
        private readonly Func<RelatorioForm> _relatorioFormFactory;
        private readonly ArrayList _terminaisConectados;
        private readonly AudioService _audioService;
        private readonly IProdutoCacheService _produtoCacheService;
        private ConfiguracaoForm _logForm;
        private RelatorioForm _relatorioForm;

        public TrayApplicationContext(
            IBuscaPrecosService buscaPrecosService,
            Logger logger,
            Servidor servidor,
            Func<ConfiguracaoForm> logFormFactory,
            Func<RelatorioForm> relatorioFormFactory,
            AudioService audioService,
            IProdutoCacheService produtoCacheService)
        {
            _buscaPrecosService = buscaPrecosService;
            _logger = logger;
            _servidor = servidor;
            _logFormFactory = logFormFactory;
            _terminaisConectados = new ArrayList();
            _audioService = audioService;
            _produtoCacheService = produtoCacheService;
            _relatorioFormFactory = relatorioFormFactory;

            InitializeServer();

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

        private void InitializeServer()
        {
            _servidor.onChange += OnChangeList;
            _servidor.onReceive += OnReceiveData;
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

                var precoFormatado = preco.ToString("0.00", CultureInfo.InvariantCulture);
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
                _logger.Error($"Erro ao processar dado recebido do terminal: {ex.Message}");
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

            var relatorioItem = new ToolStripMenuItem("Relatório de Consultas");
            relatorioItem.Click += (_, __) => ShowRelatorioForm();

            var exitItem = new ToolStripMenuItem("Sair");
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
                    3000,
                    "Busca de Preços",
                    $"Sincronização concluída. Itens no cache: {quantidade}.",
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _logger.Error("Erro ao forçar busca de preços: {Message}", ex.Message);
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

        private void ShowRelatorioForm()
        {
            if (_relatorioForm == null || _relatorioForm.IsDisposed)
            {
                _relatorioForm = _relatorioFormFactory();
                _relatorioForm.FormClosed += (_, __) => _relatorioForm = null;
            }

            if (!_relatorioForm.Visible)
                _relatorioForm.Show();

            _relatorioForm.WindowState = FormWindowState.Normal;
            _relatorioForm.BringToFront();
            _relatorioForm.Activate();
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
