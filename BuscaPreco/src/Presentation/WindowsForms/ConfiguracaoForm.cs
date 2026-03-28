using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Entities;
using BuscaPreco.Infrastructure.Data;
using BuscaPreco.Infrastructure.Repositories;
using BuscaPreco.Infrastructure.Scrapers;
using BuscaPreco.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Presentation.WindowsForms
{
    public partial class ConfiguracaoForm : Form
    {
        private readonly Logger logger;
        private readonly IBuscaPrecosService buscaPrecosService;
        private readonly IOptions<ProdutosFixadosConfig> _produtosFixadosOptions;
        private readonly YamlConfigWriter _yamlConfigWriter;
        private readonly Servidor _servidor;
        private List<BuscaPreco.Domain.Entities.Produto> _todosProdutos = new List<BuscaPreco.Domain.Entities.Produto>();
        private System.Windows.Forms.Timer _searchDebounceTimer;
        private bool _todosProdutosCarregados;

        public ConfiguracaoForm(Logger logger, IBuscaPrecosService buscaPrecosService,
            IOptions<ProdutosFixadosConfig> produtosFixadosOptions,
            YamlConfigWriter yamlConfigWriter,
            Servidor servidor)
        {
            this.logger = logger;
            this.buscaPrecosService = buscaPrecosService;
            _produtosFixadosOptions = produtosFixadosOptions;
            _yamlConfigWriter = yamlConfigWriter;
            _servidor = servidor;
            InicializarDebounceTimer();

            this.logger.Info("Iniciando App...");
            InitializeComponent();
        }

        private void InicializarDebounceTimer()
        {
            _searchDebounceTimer = new System.Windows.Forms.Timer();
            _searchDebounceTimer.Interval = 400;
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                FiltrarListaBusca();
            };
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            logger.Info($"ExportarParaMGV7 {source} {e.FullPath} {e.ChangeType}");
            var prods = this.buscaPrecosService.ListarTudo();
            Exportador.ExportarParaMGV7(prods, Path.Combine(AppContext.BaseDirectory, "ITENSMGV.TXT"));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Habilita_Configuracoes(true);
            CarregarFixadosAtuais();
            // Products are loaded lazily when the Promoções tab is first selected.
        }

        private void bTexto_Click(object sender, EventArgs e)
        {
            logger.Info("Função de envio de texto aos terminais está desabilitada na tela de configuração.");
        }

        private void Habilita_Configuracoes(bool val)
        {
            ipcliente.Enabled = val;
            ipservidor.Enabled = val;
            mascara.Enabled = val;
            l1.Enabled = val;
            l2.Enabled = val;
            l3.Enabled = val;
            l4.Enabled = val;
            time.Enabled = val;

            gateway.Enabled = val;
            servnomes.Enabled = val;
            nome.Enabled = val;
            ftpserv.Enabled = val;
            usuario.Enabled = val;
            senha.Enabled = val;

            dinamico.Enabled = val;
            busca.Enabled = val;

            bConfig.Enabled = val;
            bParam.Enabled = val;
            bUpdate.Enabled = val;

            if (!val)
            {
                ipcliente.Text = null;
                ipservidor.Text = null;
                mascara.Text = null;
                l1.Text = null;
                l2.Text = null;
                l3.Text = null;
                l4.Text = null;
                time.Text = null;

                gateway.Text = null;
                servnomes.Text = null;
                nome.Text = null;
                ftpserv.Text = null;
                usuario.Text = null;
                senha.Text = null;

                dinamico.Checked = false;
                busca.Checked = false;
            }
        }

        private void lista_SelectedIndexChanged(object sender, EventArgs e)
        {
            var terminal = GetTerminalSelecionado();
            if (terminal == null)
            {
                lblFirmwareInfo.Text = "Selecione um terminal para ver a versão de firmware.";
                lblFirmwareInfo.ForeColor = System.Drawing.Color.Gray;
                Habilita_Configuracoes(false);
                return;
            }

            Habilita_Configuracoes(true);
            montaConfig(terminal.config);

            bool isG2S = terminal.IsG2SComAudio;
            string audioInfo = isG2S ? " — G2 S (áudio suportado)" : " — G2 (sem áudio)";
            string macInfo = string.IsNullOrEmpty(terminal.MacAddress) ? "" : "  |  MAC: " + terminal.MacAddress;
            lblFirmwareInfo.Text = $"Modelo: {terminal.Tipo}  |  Firmware: {terminal.Versao}{audioInfo}{macInfo}";
            lblFirmwareInfo.ForeColor = isG2S
                ? System.Drawing.Color.DarkGreen
                : System.Drawing.Color.DarkBlue;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Salva config.yaml local E envia #reconf02 ao terminal selecionado
            try
            {
                var conf = geraConfig();
                var path = Path.Combine(AppContext.BaseDirectory, "config.yaml");
                SaveConfigToFile(conf, path);
                logger.Info("Configuração salva em: {Path}", path);

                var terminal = GetTerminalSelecionado();
                if (terminal != null)
                {
                    terminal.SendReconf02(
                        conf.IPServer, conf.IPCliente, conf.Mascara,
                        conf.TLinha1, conf.TLinha2, conf.TLinha3, conf.TLinha4,
                        conf.Tempo);
                    MessageBox.Show(
                        "Configuração salva e enviada ao terminal.\nO terminal será reiniciado.",
                        "Configuração", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Configuração salva localmente.\nNenhum terminal selecionado — envio remoto ignorado.",
                        "Configuração", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Erro ao salvar/enviar configuração: {Erro}", ex.Message);
                MessageBox.Show("Erro ao salvar configuração:\n" + ex.Message,
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void bUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                var terminal = GetTerminalSelecionado();
                if (terminal == null)
                {
                    MessageBox.Show("Selecione um terminal na lista antes de enviar.",
                        "BuscaPreço", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                terminal.SendRupdconfig(gateway.Text.Trim(), nome.Text.Trim());
                logger.Info("SendRupdconfig enviado ao terminal {Terminal}.", terminal.ToString());
                MessageBox.Show("Configuração de gateway/nome enviada ao terminal.",
                    "BuscaPreço", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                logger.Error("Erro ao enviar rupdconfig: {Erro}", ex.Message);
                MessageBox.Show("Erro ao enviar:\n" + ex.Message,
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void bParam_Click(object sender, EventArgs e)
        {
            try
            {
                var terminal = GetTerminalSelecionado();
                if (terminal == null)
                {
                    MessageBox.Show("Selecione um terminal na lista antes de enviar.",
                        "BuscaPreço", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                bool ipDinamico = dinamico.Checked;
                terminal.SendRparamconfig(ipDinamico);
                logger.Info("SendRparamconfig enviado ao terminal {Terminal}. IPDinamico={IPDinamico}",
                    terminal.ToString(), ipDinamico);
                MessageBox.Show("Parâmetros de rede enviados ao terminal.",
                    "BuscaPreço", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                logger.Error("Erro ao enviar rparamconfig: {Erro}", ex.Message);
                MessageBox.Show("Erro ao enviar:\n" + ex.Message,
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Terminal GetTerminalSelecionado()
        {
            if (lista.SelectedIndex < 0) return null;
            return _servidor.GetTerminal(lista.SelectedIndex);
        }

        private Configuracoes geraConfig()
        {
            Configuracoes auxconf = new Configuracoes();
            auxconf.IPCliente = ipcliente.Text;
            auxconf.IPServer = ipservidor.Text;
            auxconf.Mascara = mascara.Text;
            auxconf.TLinha1 = l1.Text;
            auxconf.TLinha2 = l2.Text;
            auxconf.TLinha3 = l3.Text;
            auxconf.TLinha4 = l4.Text;
            int t = Int32.Parse(time.Text);
            auxconf.Tempo = (char)t;

            auxconf.Gateway = gateway.Text;
            auxconf.ServidorNomes = servnomes.Text;
            auxconf.Nome = nome.Text;
            auxconf.EndUpdate = ftpserv.Text;
            auxconf.User = usuario.Text;
            auxconf.Pass = senha.Text;

            auxconf.IPDinamico = dinamico.Checked ? 1 : 0;
            auxconf.BuscaServidor = busca.Checked ? 1 : 0;

            return auxconf;
        }

        private void montaConfig(Configuracoes conf)
        {
            ipcliente.Text = conf.IPCliente;
            ipservidor.Text = conf.IPServer;
            mascara.Text = conf.Mascara;
            l1.Text = conf.TLinha1;
            l2.Text = conf.TLinha2;
            l3.Text = conf.TLinha3;
            l4.Text = conf.TLinha4;

            time.Text = conf.Tempo.ToString();

            gateway.Text = conf.Gateway;
            servnomes.Text = conf.ServidorNomes;
            nome.Text = conf.Nome;
            ftpserv.Text = conf.EndUpdate;
            usuario.Text = conf.User;
            senha.Text = conf.Pass;

            dinamico.Checked = conf.IPDinamico != 0;
            busca.Checked = conf.BuscaServidor != 0;
        }

        private void bReset_Click(object sender, EventArgs e)
        {
            logger.Info("bReset acionado, operação desabilitada na tela de configuração.");
        }

        private void SaveConfigToFile(Configuracoes conf, string filePath)
        {
            // Para preservar a estrutura do YAML (seções como Terminal:, DbfConfig:),
            // lemos o arquivo original e apenas atualizamos os valores das chaves conhecidas.
            // Se o arquivo não existir, criamos um novo com a estrutura básica.
            
            var lines = File.Exists(filePath) 
                ? File.ReadAllLines(filePath).ToList() 
                : new List<string>();

            void UpdateOrAdd(string key, string value)
            {
                string formattedValue = value?.Replace("\\", "\\\\").Replace("\"", "\"\"") ?? string.Empty;
                string newLine = $"{key}: \"{formattedValue}\"";
                
                int index = lines.FindIndex(l => l.TrimStart().StartsWith(key + ":"));
                if (index >= 0)
                {
                    // Preserva a indentação original
                    string indentation = lines[index].Substring(0, lines[index].IndexOf(key));
                    lines[index] = $"{indentation}{key}: \"{formattedValue}\"";
                }
                else
                {
                    lines.Add(newLine);
                }
            }

            void UpdateOrAddInt(string key, int value)
            {
                string newLine = $"{key}: {value}";
                int index = lines.FindIndex(l => l.TrimStart().StartsWith(key + ":"));
                if (index >= 0)
                {
                    string indentation = lines[index].Substring(0, lines[index].IndexOf(key));
                    lines[index] = $"{indentation}{key}: {value}";
                }
                else
                {
                    lines.Add(newLine);
                }
            }

            UpdateOrAdd("IPCliente", conf.IPCliente);
            UpdateOrAdd("IPServer", conf.IPServer);
            UpdateOrAdd("Mascara", conf.Mascara);
            UpdateOrAdd("TLinha1", conf.TLinha1);
            UpdateOrAdd("TLinha2", conf.TLinha2);
            UpdateOrAdd("TLinha3", conf.TLinha3);
            UpdateOrAdd("TLinha4", conf.TLinha4);
            UpdateOrAddInt("Tempo", (int)conf.Tempo);
            UpdateOrAdd("Gateway", conf.Gateway);
            UpdateOrAdd("ServidorNomes", conf.ServidorNomes);
            UpdateOrAdd("Nome", conf.Nome);
            UpdateOrAdd("EndUpdate", conf.EndUpdate);
            UpdateOrAdd("User", conf.User);
            UpdateOrAdd("Pass", conf.Pass);
            UpdateOrAddInt("IPDinamico", conf.IPDinamico);
            UpdateOrAddInt("BuscaServidor", conf.BuscaServidor);

            File.WriteAllLines(filePath, lines);
        }


        private void txtBuscaProduto_TextChanged(object sender, EventArgs e)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void txtBuscaProduto_Leave(object sender, EventArgs e)
        {
            var text = txtBuscaProduto.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            var isNumeric = text.All(char.IsDigit);
            if (!isNumeric) return;

            _searchDebounceTimer.Stop();
            FiltrarListaBusca();

            for (int i = 0; i < listBuscaResultados.Items.Count; i++)
            {
                var p = listBuscaResultados.Items[i] as BuscaPreco.Domain.Entities.Produto;
                if (p != null && p.CodigoItem == text)
                {
                    listBuscaResultados.SelectedIndex = i;
                    listBuscaResultados.TopIndex = i;
                    break;
                }
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPagePromocoes)
                CarregarTodosProdutos();
        }

        private void CarregarTodosProdutos()
        {
            if (_todosProdutosCarregados)
            {
                FiltrarListaBusca();
                return;
            }

            _todosProdutos = buscaPrecosService.ListarTudo();
            _todosProdutosCarregados = true;
            FiltrarListaBusca();
        }

        private void FiltrarListaBusca()
        {
            var query = txtBuscaProduto.Text.Trim().ToUpperInvariant();
            listBuscaResultados.DisplayMember = string.Empty;
            listBuscaResultados.BeginUpdate();
            listBuscaResultados.Items.Clear();
            foreach (var p in _todosProdutos)
            {
                if (string.IsNullOrEmpty(query) ||
                    (p.Descricao1 ?? string.Empty).ToUpperInvariant().Contains(query) ||
                    (p.CodigoItem ?? string.Empty).Contains(query))
                {
                    listBuscaResultados.Items.Add(p);
                }
            }
            listBuscaResultados.EndUpdate();
            listBuscaResultados.DisplayMember = "Descricao1";
        }

        private void btnAdicionarFixado_Click(object sender, EventArgs e)
        {
            if (listBuscaResultados.SelectedItem is BuscaPreco.Domain.Entities.Produto produto)
            {
                var jaExiste = listFixados.Items
                    .Cast<BuscaPreco.Domain.Entities.Produto>()
                    .Any(p => p.CodigoItem == produto.CodigoItem);
                if (!jaExiste)
                {
                    listFixados.DisplayMember = string.Empty;
                    listFixados.Items.Add(produto);
                    listFixados.DisplayMember = "Descricao1";
                }
            }
        }

        private void btnRemoverFixado_Click(object sender, EventArgs e)
        {
            if (listFixados.SelectedIndex >= 0)
            {
                listFixados.Items.RemoveAt(listFixados.SelectedIndex);
            }
        }

        private void btnLimparFixados_Click(object sender, EventArgs e)
        {
            listFixados.Items.Clear();
        }

        private void btnSalvarFixados_Click(object sender, EventArgs e)
        {
            try
            {
                var codes = listFixados.Items
                    .Cast<BuscaPreco.Domain.Entities.Produto>()
                    .Select(p => p.CodigoItem)
                    .ToList();

                _yamlConfigWriter.SaveProdutosFixados(codes);
                logger.Info("Promoções salvas: {Count} produto(s) fixado(s).", codes.Count);
                MessageBox.Show(
                    "Promoções salvas com sucesso.",
                    "BuscaPreço",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao salvar promoções:\n" + ex.Message,
                    "BuscaPreço — Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void CarregarFixadosAtuais()
        {
            listFixados.Items.Clear();
            var codes = _produtosFixadosOptions.Value.Codigos;
            if (codes == null || codes.Count == 0) return;

            var produtos = buscaPrecosService.ListarTudo();
            var byCode = produtos.ToDictionary(p => p.CodigoItem);
            foreach (var code in codes)
            {
                if (byCode.TryGetValue(code, out var produto))
                    listFixados.Items.Add(produto);
            }
            listFixados.DisplayMember = "Descricao1";
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Ciclo de vida do servidor é controlado no TrayApplicationContext.
        }
    }
}
