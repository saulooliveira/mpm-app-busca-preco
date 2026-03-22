using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BuscaPreco.Application.Configurations;
using BuscaPreco.CrossCutting;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Infrastructure.Scrapers
{
    public class Servidor
    {
        public delegate void onReceiveCommand(object sender, string comando);
        public event onReceiveCommand onReceive;

        public delegate void onChangeList(ArrayList lista);
        public event onChangeList onChange;

        private Socket server;
        private Socket cliente;
        private readonly ArrayList listaTerminais;
        private readonly object listaTerminaisLock = new object();
        private Task serverTask;
        private CancellationTokenSource cancellationTokenSource;
        private readonly IPEndPoint ipServer;
        private readonly TerminalConfig terminalConfig;
        private readonly Logger logger;
        private TaskCompletionSource<bool> serverReadyTcs;

        private void ReceiveCommand(object sender, string str)
        {
            onReceive?.Invoke(sender, str);
        }

        private void RemoveTerminal(object sender)
        {
            lock (listaTerminaisLock)
            {
                listaTerminais.Remove((Terminal)sender);
            }

            onChange?.Invoke(listaTerminais);
        }

        private void AddTerminal(Terminal term)
        {
            lock (listaTerminaisLock)
            {
                listaTerminais.Add(term);
            }

            onChange?.Invoke(listaTerminais);
        }

        public Servidor(IOptions<TerminalConfig> terminalOptions, Logger logger)
        {
            this.logger = logger;
            terminalConfig = terminalOptions.Value;
            listaTerminais = new ArrayList();
            ipServer = new IPEndPoint(IPAddress.Any, terminalConfig.Porta);
        }


        public void BroadcastProdutoPromocional(string nome, string preco)
        {
            Terminal[] terminaisSnapshot;
            lock (listaTerminaisLock)
            {
                terminaisSnapshot = new Terminal[listaTerminais.Count];
                for (var i = 0; i < listaTerminais.Count; i++)
                {
                    terminaisSnapshot[i] = (Terminal)listaTerminais[i];
                }
            }

            foreach (var terminal in terminaisSnapshot)
            {
                terminal.SendProcPrice(nome, preco);
            }
        }

        public void BroadcastMesg(string linha1, string linha2, int tempoSegundos)
        {
            Terminal[] terminaisSnapshot;
            lock (listaTerminaisLock)
            {
                terminaisSnapshot = new Terminal[listaTerminais.Count];
                for (var i = 0; i < listaTerminais.Count; i++)
                {
                    terminaisSnapshot[i] = (Terminal)listaTerminais[i];
                }
            }

            foreach (var terminal in terminaisSnapshot)
            {
                terminal.SendMesg(linha1, linha2, tempoSegundos);
            }
        }

        public void startServer()
        {
            Start();
        }

        public void Start()
        {
            if (serverTask != null && !serverTask.IsCompleted)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            serverReadyTcs = new TaskCompletionSource<bool>();
            serverTask = Task.Run(() => ProcessaServidorAsync(cancellationTokenSource.Token));
        }

        public async Task StartAsync()
        {
            if (serverTask != null && !serverTask.IsCompleted)
            {
                if (serverReadyTcs != null)
                {
                    await serverReadyTcs.Task;
                }
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            serverReadyTcs = new TaskCompletionSource<bool>();
            serverTask = Task.Run(() => ProcessaServidorAsync(cancellationTokenSource.Token));
            await serverReadyTcs.Task;
        }

        [Obsolete]
        public void stopServer()
        {
            cancellationTokenSource?.Cancel();
            server?.Close();
        }

        // Non-obsolete wrapper method to stop the server
        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            try
            {
                server?.Close();
            }
            catch { }
        }

        private async Task ProcessaServidorAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    server.Bind(ipServer);
                    server.Listen(5);
                    logger.Info("Servidor do terminal iniciado na porta {Porta}", terminalConfig.Porta);
                    
                    // Signal that the server is ready
                    serverReadyTcs?.TrySetResult(true);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        cliente = await Task<Socket>.Factory.FromAsync(server.BeginAccept, server.EndAccept, null);
                        if (cliente.Connected)
                        {
                            var terminal = new Terminal(cliente);
                            terminal.onReceive += new Terminal.onReceiveCommand(ReceiveCommand);
                            terminal.Desconectar += new Terminal.onDisconectTerminal(RemoveTerminal);
                            await Task.Delay(100, cancellationToken);
                            AddTerminal(terminal);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Warning("Conexão com terminal indisponível. Tentando reconectar em {Delay}ms. Erro: {Erro}", terminalConfig.ReconnectDelayMs, ex.Message);
                    try
                    {
                        server?.Close();
                    }
                    catch
                    {
                    }

                    await Task.Delay(terminalConfig.ReconnectDelayMs, cancellationToken);
                }
            }
        }
    }
}
