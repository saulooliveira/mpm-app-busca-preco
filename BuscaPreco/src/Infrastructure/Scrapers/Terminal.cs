using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using BuscaPreco.CrossCutting;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Infrastructure.Scrapers
{
    public class Terminal
    {
        public delegate void onDisconectTerminal(object sender);
        public delegate void onReceiveCommand(object sender, string comando);
        public event onDisconectTerminal Desconectar;
        public event onReceiveCommand onReceive;

        private Socket sock;
        private Thread meuProcesso;
        private IPEndPoint IP;

        private string tipo;
        private string versao;
        private string macAddress;

        public Configuracoes config;

        public string Tipo => tipo ?? string.Empty;
        public string Versao => versao ?? string.Empty;
        public string MacAddress => macAddress ?? string.Empty;

        public bool IsG2SComAudio =>
            !string.IsNullOrEmpty(tipo) &&
            tipo.Equals("tc406", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(versao) &&
            versao.TrimStart().StartsWith("3.");

        public Terminal(Socket socket)
        {
            sock = socket;
            config = new Configuracoes();
            meuProcesso = new Thread(ProcessaTerminal);
            meuProcesso.Start();
        }

        public override string ToString()
        {
            string ip = IP == null ? "?" : IP.ToString();
            int colonIdx = ip.IndexOf(':');
            if (colonIdx >= 0) ip = ip.Substring(0, colonIdx);
            string mac = string.IsNullOrEmpty(macAddress) ? "" : " (" + macAddress + ")";
            return ip + ":" + tipo + "/" + versao + mac;
        }

        public void Reset()
        {
            byte[] senhaBytes = { 0xa5, 0xcc, 0x5a, 0x33 };
            EnviaParaTerminal("#restartsoft" + Encoding.ASCII.GetString(senhaBytes));
            sock.Close();
        }

        public void SendProdNFound()
        {
            if (!sock.Connected) return;
            EnviaParaTerminal("#nfound");
        }

        public void SendProcPrice(string desc, string price)
        {
            if (!sock.Connected) return;
            desc = GertecProtocol.Truncate(desc);
            price = GertecProtocol.Truncate(price).Replace("#", string.Empty);
            EnviaParaTerminal("#" + desc + "|" + price);
        }

        public void SendMesg(string linha1, string linha2, int tempo)
        {
            if (!sock.Connected) return;
            linha1 = GertecProtocol.Truncate(linha1 ?? string.Empty);
            linha2 = GertecProtocol.Truncate(linha2 ?? string.Empty);
            if (tempo < 1) tempo = 1;
            if (tempo > 9) tempo = 9;
            EnviaParaTerminal("#mesg" +
                GertecProtocol.LenChar(linha1) + linha1 +
                GertecProtocol.LenChar(linha2) + linha2 +
                (char)(tempo + 48) +
                (char)(48));
        }

        public void SendReconf02(string ipServidor, string ipCliente, string mascara,
            string linha1, string linha2, string linha3, string linha4, int tempo)
        {
            if (!sock.Connected) return;
            EnviaParaTerminal("#reconf02" +
                GertecProtocol.LenChar(ipServidor) + ipServidor +
                GertecProtocol.LenChar(ipCliente) + ipCliente +
                GertecProtocol.LenChar(mascara) + mascara +
                GertecProtocol.LenChar(linha1) + linha1 +
                GertecProtocol.LenChar(linha2) + linha2 +
                GertecProtocol.LenChar(linha3) + linha3 +
                GertecProtocol.LenChar(linha4) + linha4 +
                (char)(tempo + 48));
        }

        public void SendRparamconfig(bool ipDinamico)
        {
            if (!sock.Connected) return;
            EnviaParaTerminal("#rparamconfig" + (char)((ipDinamico ? 1 : 0) + 48) + (char)(48));
        }

        public void SendRupdconfig(string gateway, string nomeTerminal)
        {
            if (!sock.Connected) return;
            string ns = "Não suportado";
            EnviaParaTerminal("#rupdconfig" +
                GertecProtocol.LenChar(gateway) + gateway +
                GertecProtocol.LenChar(string.Empty) +
                GertecProtocol.LenChar(nomeTerminal) + nomeTerminal +
                (char)(61) + ns +
                (char)(61) + ns +
                (char)(61) + ns);
        }

        public void SendPlayAudioWithMessage(byte[] wavBytes, int duracaoSegundos,
            int volume, string desc, string preco)
        {
            if (!sock.Connected) return;
            if (!IsG2SComAudio) { SendProcPrice(desc, preco); return; }

            try
            {
                if (duracaoSegundos < 2) duracaoSegundos = 2;
                if (duracaoSegundos > 7) duracaoSegundos = 7;
                if (volume < 0) volume = 0;
                if (volume > 3) volume = 3;
                desc = GertecProtocol.Truncate(desc);
                preco = GertecProtocol.Truncate(preco).Replace("#", string.Empty);

                string header = "#playaudiowithmessage" +
                    wavBytes.Length.ToString("X6") +
                    (char)(duracaoSegundos + 48) +
                    (char)(volume + 48) +
                    desc.Length.ToString("D2") + desc +
                    preco.Length.ToString("D2") + preco;

                byte[] headerBytes = Encoding.ASCII.GetBytes(header);
                byte[] payload = new byte[headerBytes.Length + wavBytes.Length];
                Buffer.BlockCopy(headerBytes, 0, payload, 0, headerBytes.Length);
                Buffer.BlockCopy(wavBytes, 0, payload, headerBytes.Length, wavBytes.Length);
                sock.Send(payload);
            }
            catch
            {
                try { SendProcPrice(desc, preco); } catch { }
            }
        }

        private void EnviaParaTerminal(string comando)
        {
            sock.Send(Encoding.ASCII.GetBytes(comando));
        }

        // Returns: 0=success, 1=timeout, -1=error/disconnect
        private int RecebeDoTerminal(ref string comando)
        {
            comando = null;
            byte[] dados = new byte[255];
            var listaSock = new List<Socket> { sock };
            try
            {
                Socket.Select(listaSock, null, null, 5 * 1000000);
                if (listaSock.Count == 1)
                {
                    int bytesRecebidos = sock.Receive(dados);
                    if (bytesRecebidos == 0) return -1;
                    comando = Encoding.ASCII.GetString(dados, 0, bytesRecebidos);
                    return 0;
                }
                return 1;
            }
            catch
            {
                return -1;
            }
        }

        private void ProcessaTerminal()
        {
            try
            {
                Thread.Sleep(50);

                string paraServidor = null;
                int controleConectado;
                int contLive = 0;

                EnviaParaTerminal("#ok");

                int retryCount = 0;
                while (retryCount < 3)
                {
                    int receiveResult = RecebeDoTerminal(ref paraServidor);
                    if (receiveResult == 0 && !string.IsNullOrEmpty(paraServidor)) break;
                    retryCount++;
                    Thread.Sleep(200);
                }
                paraServidor = (paraServidor ?? string.Empty).Trim('\0', '\r', '\n', ' ');

                try { IP = (IPEndPoint)sock.RemoteEndPoint; } catch { IP = null; }

                int separadorIdx = paraServidor.IndexOf('|');
                if (string.IsNullOrEmpty(paraServidor) || separadorIdx < 2)
                {
                    tipo = "desconhecido";
                    versao = "0.0";
                }
                else
                {
                    tipo = paraServidor.Substring(1, separadorIdx - 1);
                    versao = paraServidor.Substring(separadorIdx + 1).TrimEnd('\0', ' ');
                }

                EnviaParaTerminal("#alwayslive");
                Thread.Sleep(300);
                string alwaysLiveResp = null;
                RecebeDoTerminal(ref alwaysLiveResp);

                EnviaParaTerminal("#config02?");
                Thread.Sleep(500);
                RecebeDoTerminal(ref paraServidor);
                config.ProcessaConfig((paraServidor ?? string.Empty).Trim('\0', '\r', '\n', ' '));

                EnviaParaTerminal("#paramconfig?");
                Thread.Sleep(500);
                RecebeDoTerminal(ref paraServidor);
                config.ProcessaParam((paraServidor ?? string.Empty).Trim('\0', '\r', '\n', ' '));

                EnviaParaTerminal("#updconfig?");
                Thread.Sleep(500);
                RecebeDoTerminal(ref paraServidor);
                config.ProcessaUpdate((paraServidor ?? string.Empty).Trim('\0', '\r', '\n', ' '));

                while (true)
                {
                    controleConectado = RecebeDoTerminal(ref paraServidor);

                    if (controleConectado == 1)
                    {
                        if (contLive < 2) { EnviaParaTerminal("#live?"); contLive++; }
                        else break;
                    }
                    else if (controleConectado == -1)
                        break;
                    else
                    {
                        contLive = 0;
                        paraServidor = (paraServidor ?? string.Empty).Trim('\0', '\r', '\n', ' ');
                        if (!string.IsNullOrEmpty(paraServidor) &&
                            paraServidor.CompareTo("#live") != 0 &&
                            !paraServidor.StartsWith("#queryprocessfailure"))
                        {
                            try { onReceive?.Invoke(this, paraServidor); } catch { }
                        }
                    }
                }
                sock.Close();
                Desconectar(this);
            }
            catch (Exception)
            {
                try { sock?.Close(); } catch { }
                try { Desconectar?.Invoke(this); } catch { }
            }
        }
    }
}
