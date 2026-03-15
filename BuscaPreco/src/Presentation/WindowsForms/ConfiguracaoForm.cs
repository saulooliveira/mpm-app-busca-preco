using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Entities;
using BuscaPreco.Infrastructure.Repositories;
using BuscaPreco.Infrastructure.Scrapers;

namespace BuscaPreco.Presentation.WindowsForms
{
    public partial class ConfiguracaoForm : Form
    {
        private readonly Logger logger;
        private readonly IBuscaPrecosService buscaPrecosService;
        private readonly Servidor servidor; //cria uma instancia do servidor

        // listas para o cadastro dos terminais conectados e selecionado
        ArrayList terminaisConectados;
        ArrayList ItensSelecionados;

        delegate void SafeListChange();// evento para alterar a lista
        delegate void Escreve(string str); // evento para escrever no histrico
 

        /*
        Método: Form1
        Função: Construtor da classe
        */
    public ConfiguracaoForm(Logger logger, IBuscaPrecosService buscaPrecosService, Servidor servidor)
        {
            this.logger = logger;
            this.buscaPrecosService = buscaPrecosService;
            this.servidor = servidor;

            this.logger.Info("Iniciando App...");
            InitializeComponent();// inicializa o formulario
        }

      
      

      

        // Função que será chamada quando um arquivo for alterado
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            logger.Info($"ExportarParaMGV7 {source} {e.FullPath} {e.ChangeType}");
            var prods = this.buscaPrecosService.ListarTudo();
            Exportador.ExportarParaMGV7(prods, Path.Combine(AppContext.BaseDirectory, "ITENSMGV.TXT"));
        }

   
        /*
        Método: Form1_Load
        Função: Funçao para tratar o evento formload
        */
    private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                ItensSelecionados = new ArrayList(); // cria a lista de terminais conectados
                // Nesta tela usamos o formulário apenas como tela de configuração.
                // Não inicializamos o servidor aqui para evitar comportamento de produção.
                Habilita_Configuracoes(true); // habilita a edição das configurações
            }
            catch (Exception ex)
            {
                logger.Info($"Form1_Load {ex.Message} {ex.StackTrace}");
               
            }
        }

        /*
        Método: bTexto_Click
        Função: Evento do Botao. Percorre a lista e envia o texto para os terminais conectados
        */
        // Função de broadcast desabilitada na tela de configuração
        private void bTexto_Click(object sender, EventArgs e){
            logger.Info("Função de envio de texto aos terminais está desabilitada na tela de configuração.");
        }

        /*
        Método: alteraLista
        Função: Altera a lista de terminais conectados e remarca os selecionados
                    Como a chamada desta funçao é feita através de outra thread, é necessário usar a técnica safethread
        */
        private void alteraLista() {
            if (this.lista.InvokeRequired)// verifica o componente lista pertence a esta thread se nao pertencer...
            {
                SafeListChange d = new SafeListChange(alteraLista);//cria um vinculo com a thread correta
                this.Invoke(d);//re-chama a funçao
            }
            else// se o compinente pertence a thread
            {
                lista.Items.Clear();// limpa a lista de conectados
                foreach (Terminal term in terminaisConectados)// percorre a lista de conctados
                {
                    this.lista.Items.Add(term);//adiciona os itens a lista
                }
                if (lista.Items.Count >= ItensSelecionados.Count){//sehouver itens selecionados
                    for (int index = 0; index < ItensSelecionados.Count; index++)//percorre os itens da lista
                    {
                        lista.SetSelected(index, true);//re-seleciona os itens
                    }
                }
            }
        }

        /*
        Método: escreveHistorico
        Função: escreve uma string no histrico
                    Como este método é chamado fora da thread que possui o componente, é necessário retornar ao processo correto
         
        Entrada: str - string
        */
        private void escreveHistorico(string str) {
            // mantém funcionalidade mas protege chamadas de outras threads
            if (this.textBox1.InvokeRequired)
            {
                Escreve d = new Escreve(escreveHistorico);
                string[] array = new String[1];
                array[0] = str;
                this.Invoke(d, array);
            }
            else
            {
                this.textBox1.AppendText(str + '\n');
            }
        }

        /*
        Método: onChangeList
        Função: evento para alterar a lista
        
        Entrada: novalista - lista gerada pelo servidor
        */
        private void onChangeList(ArrayList novalista)
        {
            terminaisConectados = novalista; // copia a lista

            this.alteraLista();// chama o método para alterar a lista
        }

        /*
        Método: onReceiveData
        Função: Trata o evento de recebimento de dados dos terminais
        
        Entrada: sender - terminal que enviou o comando
                 str - comando recebido
        */
        // Ao usar o formulário como tela de configuração, não tratamos eventos recebidos do terminal aqui.
        public void onReceiveData(object sender, string str){
            logger.Info("onReceiveData recebido, mas ignorei na tela de configuração.");
        }

        /*
        Método: Habilita_Configuracoes
        Função: habilita/desabilita a ediçao e visualizacao das configuraçoes
         
        Entrada: true - habilitar
                 false - desabilitar
        */
        private void Habilita_Configuracoes(bool val){
            //habilita/desabilita os editbox/checkbox/botoes
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

            // se for para desabilitar anula os valores mostrados
            if (val == false) {
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

        /*
        Método: lista_SelectedIndexChanged
        Função: evento disparado toda a vez que um item é selecionado
        */
        private void lista_SelectedIndexChanged(object sender, EventArgs e)
        {
            ItensSelecionados.Clear();//limpa a lista de selecionados
            //percorre a lista de terminais conectados
            for (int index = 0; index < lista.Items.Count; index++)
            {
                if (lista.GetSelected(index))// se o item estiver selecionado
                {
                    ItensSelecionados.Add(index);//adiciona na lista
                }
            }
            if (ItensSelecionados.Count == 1)// se existir itens selecionados
            {
                Habilita_Configuracoes(true);//habilita as configuraçoes
                Terminal term = (Terminal)terminaisConectados[(int)ItensSelecionados[0]];//seleciona o terminal selecionado
                montaConfig(term.config);//altera as configuracoes
            }
            else {//se mais de um item estiver selecionado
                Habilita_Configuracoes(false);// desabilita as configuraçoes
            }
        }

        /*
        Método: button1_Click
        Função: evento para enviar as configuraçoes para o terminal
        */
        // Salvamento de configuração local em formato YAML simples
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

        /*
        Método: bUpdate_Click
        Função: evento para enviar as configuraçoes para o terminal
        */
        private void bUpdate_Click(object sender, EventArgs e)
        {
            // Operação não aplicável na tela de configuração local
            logger.Info("bUpdate acionado, operação desabilitada na tela de configuração.");
        }

        /*
        Método: bParam_Click
        Função: evento para enviar configuraçoes para o terminal
        */
        private void bParam_Click(object sender, EventArgs e)
        {
            logger.Info("bParam acionado, operação desabilitada na tela de configuração.");
        }

        /*
        Método: geraConfig
        Função: gera as mensagens de configuraçoes
         
        Retorno: um objeto com as configuraçoes
        */
        private Configuracoes geraConfig() {
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

            auxconf.IPDinamico = dinamico.Checked == true ? 1 : 0;
            auxconf.BuscaServidor = busca.Checked == true ? 1 : 0;

            return auxconf;
        }

        /*
        Método: montaConfig
        Função: Escreve nos edits as configuraçoes do terminal selecionado
         
        Entrada: conf - configuraçoes a ser escrita
        */
        private void montaConfig(Configuracoes conf) {
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

            dinamico.Checked = conf.IPDinamico == 0 ? false : true;
            busca.Checked = conf.BuscaServidor == 0 ? false : true;
        }

        /*
        Método: bReset_Click
        Função: envia o comando reset para os termminais selecionados
        */
        private void bReset_Click(object sender, EventArgs e)
        {
            logger.Info("bReset acionado, operação desabilitada na tela de configuração.");
        }

        // Salva uma configuração simples em YAML (formato mínimo)
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

    private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                servidor.Stop();
            }
            catch (Exception ex)
            {
                logger.Warning($"Erro ao parar servidor: {ex.Message}");
            }
        }
    }
}
