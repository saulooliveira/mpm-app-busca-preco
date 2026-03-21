using System;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Entities;
using BuscaPreco.Infrastructure.Data;
using BuscaPreco.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Presentation.WindowsForms
{
    public partial class ConfiguracaoForm : Form
    {
        private readonly Logger logger;
        private readonly IBuscaPrecosService buscaPrecosService;
        private readonly IOptions<ProdutosFixadosConfig> _produtosFixadosOptions;
        private readonly YamlConfigWriter _yamlConfigWriter;

        public ConfiguracaoForm(Logger logger, IBuscaPrecosService buscaPrecosService,
            IOptions<ProdutosFixadosConfig> produtosFixadosOptions, YamlConfigWriter yamlConfigWriter)
        {
            this.logger = logger;
            this.buscaPrecosService = buscaPrecosService;
            _produtosFixadosOptions = produtosFixadosOptions;
            _yamlConfigWriter = yamlConfigWriter;

            this.logger.Info("Iniciando App...");
            InitializeComponent();
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
            // Sem gestão de terminais no formulário.
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var conf = geraConfig();
                var path = Path.Combine(AppContext.BaseDirectory, "config.yaml");
                SaveConfigToFile(conf, path);
                logger.Info($"Configuração salva em: {path}");
                MessageBox.Show($"Configuração salva em:\n{path}", "Configuração", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                logger.Info($"Erro ao salvar configuração: {ex.Message}");
                MessageBox.Show($"Erro ao salvar configuração: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void bUpdate_Click(object sender, EventArgs e)
        {
            logger.Info("bUpdate acionado, operação desabilitada na tela de configuração.");
        }

        private void bParam_Click(object sender, EventArgs e)
        {
            logger.Info("bParam acionado, operação desabilitada na tela de configuração.");
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
            string Escape(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\"\"") ?? string.Empty;

            var lines = new System.Collections.Generic.List<string>();
            lines.Add($"IPCliente: \"{Escape(conf.IPCliente)}\"");
            lines.Add($"IPServer: \"{Escape(conf.IPServer)}\"");
            lines.Add($"Mascara: \"{Escape(conf.Mascara)}\"");
            lines.Add($"TLinha1: \"{Escape(conf.TLinha1)}\"");
            lines.Add($"TLinha2: \"{Escape(conf.TLinha2)}\"");
            lines.Add($"TLinha3: \"{Escape(conf.TLinha3)}\"");
            lines.Add($"TLinha4: \"{Escape(conf.TLinha4)}\"");
            lines.Add($"Tempo: {((int)conf.Tempo)}");
            lines.Add($"Gateway: \"{Escape(conf.Gateway)}\"");
            lines.Add($"ServidorNomes: \"{Escape(conf.ServidorNomes)}\"");
            lines.Add($"Nome: \"{Escape(conf.Nome)}\"");
            lines.Add($"EndUpdate: \"{Escape(conf.EndUpdate)}\"");
            lines.Add($"User: \"{Escape(conf.User)}\"");
            lines.Add($"Pass: \"{Escape(conf.Pass)}\"");
            lines.Add($"IPDinamico: {conf.IPDinamico}");
            lines.Add($"BuscaServidor: {conf.BuscaServidor}");

            File.WriteAllLines(filePath, lines);
        }


        private void txtBuscaProduto_TextChanged(object sender, EventArgs e)
        {
            var query = txtBuscaProduto.Text.Trim().ToUpperInvariant();
            listBuscaResultados.DisplayMember = string.Empty;
            listBuscaResultados.Items.Clear();
            var produtos = buscaPrecosService.ListarTudo();
            foreach (var p in produtos)
            {
                if (string.IsNullOrEmpty(query) ||
                    (p.Descricao1 ?? string.Empty).ToUpperInvariant().Contains(query))
                {
                    listBuscaResultados.Items.Add(p);
                }
            }
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
