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
    class Servidor
    {
        public delegate void onReceiveCommand(object sender, string comando);
        public event onReceiveCommand onReceive;

        public delegate void onChangeList(ArrayList lista);
        public event onChangeList onChange;

        private Socket server;
        private Socket cliente;
        private readonly ArrayList listaTerminais;
        private Task serverTask;
        private CancellationTokenSource cancellationTokenSource;
        private readonly IPEndPoint ipServer;
        private readonly TerminalConfig terminalConfig;
        private readonly Logger logger;

        private void ReceiveCommand(object sender, string str)
        {
            onReceive?.Invoke(sender, str);
        }

        private void RemoveTerminal(object sender)
        {
            listaTerminais.Remove((Terminal)sender);
            onChange?.Invoke(listaTerminais);
        }

        private void AddTerminal(Terminal term)
        {
            listaTerminais.Add(term);
            onChange?.Invoke(listaTerminais);
        }

        public Servidor(IOptions<TerminalConfig> terminalOptions, Logger logger)
        {
            this.logger = logger;
            terminalConfig = terminalOptions.Value;
            listaTerminais = new ArrayList();
            ipServer = new IPEndPoint(IPAddress.Any, terminalConfig.Porta);
        }

        public void startServer()
        {
            cancellationTokenSource = new CancellationTokenSource();
            serverTask = Task.Run(() => ProcessaServidorAsync(cancellationTokenSource.Token));
        }

        [Obsolete]
        public void stopServer()
        {
            cancellationTokenSource?.Cancel();
            server?.Close();
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

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        cliente = await Task<Socket>.Factory.FromAsync(server.BeginAccept, server.EndAccept, null);
                        if (cliente.Connected)
                        {
                            var terminal = new Terminal(cliente);
                            terminal.onReceive += new Terminal.onReceiveCommand(ReceiveCommand);
                            terminal.Desconectar += new Terminal.onDisconectTerminal(RemoveTerminal);
                            await Task.Delay(2000, cancellationToken);
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
