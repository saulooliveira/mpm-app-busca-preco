using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace BuscaPreco.Infrastructure.Scrapers
{
    class Servidor
    {
        // Cria o evento para receber comandos
        public delegate void onReceiveCommand(object sender, string comando);
        public event onReceiveCommand onReceive;

        // Cria o evento para indicar alterações na lista de terminais
        public delegate void onChangeList(ArrayList lista);
        public event onChangeList onChange;

        private Socket server;// Socket principal abre a porta 6500 para conexão dos terminais
        private Socket cliente;// Socket do cliente
        private ArrayList listaTerminais;// Lista com os terminais conectados
        private Thread threadServidor;//thread que é usada para espera dos terminais
        private IPEndPoint IPServer;// configuração do IP do servidor

        /*
         Método: ReceiveCommand
         Função: Trata os eventos dos terminais
         
         Entrada: sender - Indicando o terminal que enviou o evento
                  str - string com a mensagem do terminal
         */
        private void ReceiveCommand(object sender, string str) {
            if (onReceive != null) // verifica se há alguma função para tratar o evento
                onReceive(sender, str); // dispara o evento
        }

        /*
         Método: RemoveTerminal
         Função: Remove um terminal (desconectado) da lista
         
         Entrada: sender - o terminal que será removido
         */
        private void RemoveTerminal(object sender) {
            listaTerminais.Remove((Terminal)sender); // Remove o terminal
            if (onChange != null) // verifica se há alguma função para o tratamento do evento
                onChange(listaTerminais); // dispara o evento
        }

        /*
         Método: AddTerminal
         Função: Adiciona um terminal na lista
         
         Entrada: term - O terminal a ser adicionado na lista
         */
        private void AddTerminal(Terminal term) {
            listaTerminais.Add(term);//adiciona o terminal
            if (onChange != null)// verifica se existe a função para tratar o evento
                onChange(listaTerminais);// dispara o evento
        }

        /*
         Método: Servidor
         Função: Construtor da classe Servidor
         */
        public Servidor() {
            listaTerminais = new ArrayList(); // cria o objeto da lista
            server = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);//cria/configura o socket servidor
            IPServer = new IPEndPoint(IPAddress.Any, 6500);// configura a porta e o IP do servidor
        }

        /*
        Método: startServer
        Função: Função que inicia o servidor
        */
        public void startServer()
        {
            threadServidor = new Thread(ProcessaServidor);// cria uma thread para esperar conexões informando qual função será executada
            threadServidor.Start();// inicia a thread
        }

        [Obsolete]
        public void stopServer()
        {
            threadServidor.Abort();
 
        }

        /*
         Método: ProcessaServidor
         Função: Função executada em uma thread para esperar por conexões dos terminais
         */
        private void ProcessaServidor(){
            Terminal terminal; // cria uma instância para a classe Terminal
            server.Bind(IPServer); // Configura a porta do servidor
            server.Listen(5); // abre a porta para conexões

            // loop infinito que espera as conexões dos terminais
            while (true){
                cliente = server.Accept();// aceita a conexão do terminal e retorna o socket para comunicação
                if (cliente.Connected) // se houve a correta conexão do terminal
                {
                    terminal = new Terminal(cliente);// cria o objeto terminal e passa o socket para o objeto
                    terminal.onReceive += new Terminal.onReceiveCommand(ReceiveCommand);//configura a função para a qual o terminal irá disparar o evento de comandos
                    terminal.Desconectar += new Terminal.onDisconectTerminal(RemoveTerminal);//configura a função para a qual o terminal irá disparar o evento de desconexão
                    Thread.Sleep(2000); // espera 2 segundos para o terminal começar a comuniocação
                    AddTerminal(terminal);// adiciona o terminal à lista
                }
            }
        }
    }
}
