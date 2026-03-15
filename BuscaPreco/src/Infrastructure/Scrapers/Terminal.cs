using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Infrastructure.Scrapers
{
    class Terminal
    {
        //Configura os eventos para desconectar o terminal e enviar comandos
        public delegate void onDisconectTerminal(object sender);
        public delegate void onReceiveCommand(object sender, string comando);
        public event onDisconectTerminal Desconectar;
        public event onReceiveCommand onReceive;
        
        private Socket sock; // socket do terminal
        private Thread meuProcesso; // thread para controlar o processo do terminal]
        private IPEndPoint IP; // IP da conexão
        
        private string tipo; // tipo do terminal
        private string versao; // versão do terminal

        public Configuracoes config;// gerencia as configurações dos terminais

        /*
         Método: Terminal
         Função: Construtor da classe, inicia a thread para o terminal
         
         Entrada: socket - socket para a comunicação com o terminal
         */
        public Terminal(Socket socket){
            sock = socket; // Atribui o socket para o atributo sock
            config = new Configuracoes(); // inicia o objeto config
            meuProcesso = new Thread(ProcessaTerminal); // configura a thread para o processo do terminal
            meuProcesso.Start();// inicia a thread
        }

        /*
         Método: ToString
         Função: Método reescrito para altera o modo como o objeto é visto em forma de string
         */
        override public string ToString(){
            string ip = IP.ToString().Substring(0, IP.ToString().IndexOf(':')); // corta a porta de conexão mantendo somente o IP
            return ip.ToString()+":"+tipo + "/" + versao; // retorna IP:tipo/versão
        }

        /*
         Método: sendConfig
         Função: Envia a configuração para o terminal (#reconf02)
         */
        public void sendConfig() {
            EnviaParaTerminal(config.montaConfig()); // envia para o terminal
            Thread.Sleep(100);
            sock.Close();
        }

        /*
         Método: sendParam
         Função: Envia os parametros para o terminal (#paramconfig)
         */
        public void sendParam() {
            EnviaParaTerminal(config.montaParamConfig()); // envia para o terminal
            //Thread.Sleep(100);
            sock.Close();
        }

        /*
         Método: sendUpdate
         Função: Envia o update para o terminal (#updconfig)
         */
        public void sendUpdate() {
            EnviaParaTerminal(config.montaUpdateConfig()); // envia para o terminal
            //Thread.Sleep(100);
            sock.Close();
        }

        /*
         Método: EnviaTexto
         Função: Monta a mensagem para o terminal mostrar um texto na tela
         
         Entrada: linha1 - Conteúdo da 1ª linha
                  linha2 - Conteúdo da 2ª linha
                  tempo - tempo de exibição do texto
         */
        public void EnviaTexto(string linha1, string linha2, int tempo){
            // captura o tamanho do texto (a tela do terminal so permite 20 caracteres por linha)
            // para que a string não tenha caracteres indefinidos os valores numéricos são adicionados de 48 (0 na tabela ASCII)
            int tamanhoLinha1 = linha1.Length>20?20:linha1.Length + 48;
            int tamanhoLinha2 = linha2.Length>20?20:linha2.Length + 48;
            tempo = tempo + 48;
            
            // convertendo os caracteres numéricos em texto
            char valorLinha1 = (char)tamanhoLinha1;
            char valorLinha2 = (char)tamanhoLinha2;
            char tempoExibicao = (char)tempo;

            //montando a mensagem e enviando para o terminal
            string comando = "#mesg"+valorLinha1+linha1.Substring(0, tamanhoLinha1-48)+valorLinha2+linha2.Substring(0, tamanhoLinha2-48)+tempoExibicao+'0';
            EnviaParaTerminal(comando);
        }

        /*
         Método: Reset
         Função: Reinicia o terminal
         */
        public void Reset() { 
            byte[] senhaBytes = { 0xa5, 0xcc, 0x5a, 0x33 };
            string senha = new System.Text.ASCIIEncoding().GetString(senhaBytes);
            string comando = "#restartsoft" + senha;
            EnviaParaTerminal(comando);
            sock.Close();
        }

        /*
         Método: SendProdNFound
         Função: Envia para o terminal "produto não encontrado"
         */
        public void SendProdNFound() {
            EnviaParaTerminal("#nfound");
        }

        /*
         Método: SendProcPrice
         Função: Envia para o terminal a descrição e o preço de um produto
         
         Entrada: desc - descrição do produto
                  price - preço do produto
         */
        public void SendProcPrice(string desc, string price) {
            try
            {
                if (!sock.Connected)
                {
                    System.Diagnostics.Debug.WriteLine($"SendProcPrice: Socket not connected!");
                    return;
                }
                // monta a string e envia para o terminal
                string mensaje = "#" + desc + "|" + price;
                System.Diagnostics.Debug.WriteLine($"SendProcPrice: Sending '{mensaje}'");
                EnviaParaTerminal(mensaje);
                System.Diagnostics.Debug.WriteLine($"SendProcPrice: Data sent successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendProcPrice Error: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        /*
         Método: ConverteStringToBytes
         Função: Converte a string UTF (2 bytes por caractere) para ASC (1 byte por caractere)
         
         Entrada: str - string a ser convertida
         Retorno: Vetor de bytes contendo o código ASC de cada caractere
         */
        private byte[] ConverteStringToBytes(string str){
            return new System.Text.ASCIIEncoding().GetBytes(str);
        }

        /*
         Método: EnviaParaTerminal
         Função: Função que envia os bytes para o terminal
         
         Entrada: comando - string contendo o comando
         */
        private void EnviaParaTerminal(string comando) {
            try
            {
                byte[] dados = ConverteStringToBytes(comando);
                sock.Send(dados);
                System.Diagnostics.Debug.WriteLine($"Data sent successfully: {comando}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending data: {ex.Message}");
                throw;
            }
        }

        /*
         Método: RecebeDoTerminal
         Função: Faz a leitura do socket
         
         Saída: comando - contêm o comando do teminal
         Retorno: 0 - leitura efetuada com sucesso
                  1 - Ocorreu timeout
                 -1 - Erro (desconexão do terminal)
         */
        private int RecebeDoTerminal(ref string comando){
            comando = null; //  zera a string comando
            byte[] dados = new byte[255]; // cria um vetor de 255 bytes
            ArrayList listaSock = new ArrayList(); // cria uma lista para armazenar o socket (Função Select)
            listaSock.Add(sock); // adiciona o socket à lista
            try
            {
                System.Diagnostics.Debug.WriteLine($"RecebeDoTerminal: Starting receive, socket connected: {sock.Connected}");
                Socket.Select(listaSock, null, null, 5 * 1000000); // impõe um timeout para a leitura
                if (listaSock.Count == 1)// se o terminal enviou algo
                {
                    int bytesRecebidos = sock.Receive(dados);// faz a leitura
                    comando = new System.Text.ASCIIEncoding().GetString(dados, 0, bytesRecebidos);// converte apenas os bytes recebidos para texto
                    System.Diagnostics.Debug.WriteLine($"RecebeDoTerminal: Received {bytesRecebidos} bytes: {comando}");
                    return 0; //  retorna OK
                }
                System.Diagnostics.Debug.WriteLine($"RecebeDoTerminal: Timeout - no data available");
                return 1;// caso ocorreu o timeout retorna 1
            }
            catch(Exception ex){
                System.Diagnostics.Debug.WriteLine($"RecebeDoTerminal: Exception: {ex.Message}");
                return -1;// em caso de erro (Try-catch) retorna -1
            }
        }
    
        /*
         Método: ProcessaTerminal
         Função: Função entra em loop para tratar a conexão com o terminal
         */
        private void ProcessaTerminal(){
            try
            {
                // Small delay to ensure socket is fully established
                Thread.Sleep(50);
                
                string paraServidor; // String que recebe os comandos
                int controleConectado;// Recebe o estado da conexão
                int contLive = 0;// controla se haverá desconexão forçada do terminal (terminal não responde)

                paraServidor = "init"; //inicia a string
                EnviaParaTerminal("#ok");// envia a string "#OK" para o terminal
                RecebeDoTerminal(ref paraServidor);// recebe a reposta do terminal
                System.Diagnostics.Debug.WriteLine($"Terminal init response: {paraServidor}");

                IP = (IPEndPoint)sock.RemoteEndPoint;// configura o IP da conexão
                // recolhe o tipo e a versão do terminal
                tipo = paraServidor.Substring(1, paraServidor.LastIndexOf('|') - 1);
                versao = paraServidor.Substring(paraServidor.LastIndexOf('|') + 1);
                System.Diagnostics.Debug.WriteLine($"Terminal type: {tipo}, version: {versao}");

                // pede a configuração do terminal
                EnviaParaTerminal("#config02?");
                Thread.Sleep(500);
                RecebeDoTerminal(ref paraServidor);
                System.Diagnostics.Debug.WriteLine($"Config response: {paraServidor}");
                config.ProcessaConfig(paraServidor);

                // pede os parametros do terminal
                EnviaParaTerminal("#paramconfig?");
                Thread.Sleep(500);
                RecebeDoTerminal(ref paraServidor);
                System.Diagnostics.Debug.WriteLine($"Param response: {paraServidor}");
                config.ProcessaParam(paraServidor);

                // pede as opções de atualização
                EnviaParaTerminal("#updconfig?");
                Thread.Sleep(500);
                RecebeDoTerminal(ref paraServidor);
                System.Diagnostics.Debug.WriteLine($"Update response: {paraServidor}");
                config.ProcessaUpdate(paraServidor);

                System.Diagnostics.Debug.WriteLine("Entering main command loop");
                while(true){
                    // espera o recebimento de dados do terminal
                    controleConectado = RecebeDoTerminal(ref paraServidor);
                    System.Diagnostics.Debug.WriteLine($"Terminal received command: {paraServidor}, Control: {controleConectado}");
                    
                    if (controleConectado == 1)// se houve timeout
                    {
                        if (contLive < 2){// se acontagem de timeouts seguidos for menor que 2
                            EnviaParaTerminal("#live?");// envia "#live?" para o terminal
                            contLive++;// incrementa a contagem
                        }
                        else// se houve estouro de vezes de timeout
                            break;// sai do loop
                    }
                    else if (controleConectado == -1)// se houve erro na leitura
                        break; // sai do loop
                    else {// se a leitura foi efetuada com sucesso
                        if (paraServidor.CompareTo("#live") == 0)// verifica se a mensagem foi a resposta do live
                            contLive = 0; // zera a contagem do live
                        else {// se foi qualquer outro comando
                            try
                            {
                                if (onReceive != null)// verifica se há função para receber o evento
                                    onReceive(this,paraServidor);// envia o evento
                            }
                            catch (Exception handlerEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Exception in onReceive handler: {handlerEx.GetType().Name}: {handlerEx.Message}");
                                // Continue processing, do not close socket
                            }
                        }
                    }
                }
                sock.Close();// fecha o socket
                Desconectar(this);// envia o evento para desconectar o terminal
            }
            catch (Exception ex)
            {
                try
                {
                    sock?.Close();
                }
                catch { }
                try
                {
                    Desconectar?.Invoke(this);
                }
                catch { }
            }
        }
    }
}
