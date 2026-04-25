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
            string senha = Encoding.ASCII.GetString(senhaBytes);
            EnviaParaTerminal("#restartsoft" + senha);
            sock.Close();
        }

        public void SendProdNFound()
        {
            try
            {
                if (!sock.Connected)
                {
                    System.Diagnostics.Debug.WriteLine("SendProdNFound: Socket not connected!");
                    return;
                }
                EnviaParaTerminal("#nfound");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendProdNFound Error: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        public void SendProcPrice(string desc, string price)
        {
            try
            {
                if (!sock.Connected)
                {
                    System.Diagnostics.Debug.WriteLine("SendProcPrice: Socket not connected!");
                    return;
                }
                desc = GertecProtocol.Truncate(desc);
                price = GertecProtocol.Truncate(price);
                price = price.Replace("#", string.Empty);
                string mensaje = "#" + desc + "|" + price;
                System.Diagnostics.Debug.WriteLine($"SendProcPrice: Sending '{mensaje}'");
                EnviaParaTerminal(mensaje);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendProcPrice Error: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        public void SendMesg(string linha1, string linha2, int tempo)
        {
            try
            {
                if (!sock.Connected)
                {
                    System.Diagnostics.Debug.WriteLine("SendMesg: Socket not connected!");
                    return;
                }

                linha1 = GertecProtocol.Truncate(linha1 ?? string.Empty);
                linha2 = GertecProtocol.Truncate(linha2 ?? string.Empty);
                if (tempo < 1) tempo = 1;
                if (tempo > 9) tempo = 9;

                string comando = "#mesg" +
                    GertecProtocol.LenChar(linha1) + linha1 +
                    GertecProtocol.LenChar(linha2) + linha2 +
                    (char)(tempo + 48) +
                    (char)(48);

                EnviaParaTerminal(comando);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendMesg Error: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        private void ConsultarMacAddress()
        {
            try
            {
                EnviaParaTerminal("#macaddr?");
                Thread.Sleep(300);

                string resposta = null;
                if (RecebeDoTerminal(ref resposta) != 0 || string.IsNullOrEmpty(resposta))
                    return;

                resposta = resposta.Trim('\0', '\r', '\n', ' ');
                if (!resposta.StartsWith("#macaddr"))
                    return;

                const int prefixLen = 8; // "#macaddr"
                if (resposta.Length < prefixLen + 3)
                    return;

                int tamLen = (int)resposta[prefixLen + 1] - 48;
                if (tamLen <= 0 || prefixLen + 2 + tamLen > resposta.Length)
                    return;

                macAddress = resposta.Substring(prefixLen + 2, tamLen).TrimEnd('\0', ' ');
                System.Diagnostics.Debug.WriteLine($"ConsultarMacAddress: MAC={macAddress}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConsultarMacAddress Error: {ex.Message}");
            }
        }

        public void SendReconf02(string ipServidor, string ipCliente, string mascara,
            string linha1, string linha2, string linha3, string linha4, int tempo)
        {
            try
            {
                if (!sock.Connected) return;

                string str = "#reconf02" +
                    GertecProtocol.LenChar(ipServidor) + ipServidor +
                    GertecProtocol.LenChar(ipCliente) + ipCliente +
                    GertecProtocol.LenChar(mascara) + mascara +
                    GertecProtocol.LenChar(linha1) + linha1 +
                    GertecProtocol.LenChar(linha2) + linha2 +
                    GertecProtocol.LenChar(linha3) + linha3 +
                    GertecProtocol.LenChar(linha4) + linha4 +
                    (char)(tempo + 48);

                EnviaParaTerminal(str);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendReconf02 Error: {ex.Message}");
                throw;
            }
        }

        public void SendRparamconfig(bool ipDinamico)
        {
            try
            {
                if (!sock.Connected) return;
                int tipoIP = ipDinamico ? 1 : 0;
                EnviaParaTerminal("#rparamconfig" + (char)(tipoIP + 48) + (char)(48));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendRparamconfig Error: {ex.Message}");
                throw;
            }
        }

        public void SendRupdconfig(string gateway, string nomeTerminal)
        {
            try
            {
                if (!sock.Connected) return;

                string servidorNomes = string.Empty;
                string ns = "Não suportado";

                string str = "#rupdconfig" +
                    GertecProtocol.LenChar(gateway) + gateway +
                    GertecProtocol.LenChar(servidorNomes) + servidorNomes +
                    GertecProtocol.LenChar(nomeTerminal) + nomeTerminal +
                    (char)(61) + ns +
                    (char)(61) + ns +
                    (char)(61) + ns;

                EnviaParaTerminal(str);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendRupdconfig Error: {ex.Message}");
                throw;
            }
        }

        public void SendPlayAudioWithMessage(byte[] wavBytes, int duracaoSegundos,
            int volume, string desc, string preco)
        {
            try
            {
                if (!sock.Connected) return;
                if (!IsG2SComAudio)
                {
                    SendProcPrice(desc, preco);
                    return;
                }

                if (duracaoSegundos < 2) duracaoSegundos = 2;
                if (duracaoSegundos > 7) duracaoSegundos = 7;
                if (volume < 0) volume = 0;
                if (volume > 3) volume = 3;
                desc = GertecProtocol.Truncate(desc);
                preco = GertecProtocol.Truncate(preco);
                preco = preco.Replace("#", string.Empty);

                string tamHex = wavBytes.Length.ToString("X6");
                string header = "#playaudiowithmessage" +
                    tamHex +
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendPlayAudioWithMessage Error: {ex.Message}");
                try { SendProcPrice(desc, preco); } catch { }
            }
        }

        private void EnviaParaTerminal(string comando)
        {
            try
            {
                sock.Send(Encoding.ASCII.GetBytes(comando));
                System.Diagnostics.Debug.WriteLine($"Data sent: {comando}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending data: {ex.Message}");
                throw;
            }
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
                    if (bytesRecebidos == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("RecebeDoTerminal: 0 bytes — conexão encerrada pelo terminal.");
                        return -1;
                    }
                    comando = Encoding.ASCII.GetString(dados, 0, bytesRecebidos);
                    System.Diagnostics.Debug.WriteLine($"RecebeDoTerminal: {bytesRecebidos} bytes: {comando}");
                    return 0;
                }
                return 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RecebeDoTerminal: Exception: {ex.Message}");
                return -1;
            }
        }

        private void ProcessaTerminal()
        {
            try
            {
                Thread.Sleep(50);

                string paraServidor;
                int controleConectado;
                int contLive = 0;

                paraServidor = "init";
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
                System.Diagnostics.Debug.WriteLine($"Terminal init response: {paraServidor}");

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
                System.Diagnostics.Debug.WriteLine($"Terminal identificado — tipo: {tipo}, versão: {versao}, IP: {IP}");

                EnviaParaTerminal("#alwayslive");
                Thread.Sleep(300);
                string alwaysLiveResp = null;
                RecebeDoTerminal(ref alwaysLiveResp);

                EnviaParaTerminal("#config02?");
                Thread.Sleep(500);
                RecebeDoTerminal(ref paraServidor);
                paraServidor = (paraServidor ?? string.Empty).Trim('\0', '\r', '\n', ' ');
                config.ProcessaConfig(paraServidor);

                EnviaParaTerminal("#paramconfig?");
                Thread.Sleep(500);
                RecebeDoTerminal(ref paraServidor);
                paraServidor = (paraServidor ?? string.Empty).Trim('\0', '\r', '\n', ' ');
                config.ProcessaParam(paraServidor);

                EnviaParaTerminal("#updconfig?");
                Thread.Sleep(500);
                RecebeDoTerminal(ref paraServidor);
                paraServidor = (paraServidor ?? string.Empty).Trim('\0', '\r', '\n', ' ');
                config.ProcessaUpdate(paraServidor);

                ConsultarMacAddress();

                while (true)
                {
                    controleConectado = RecebeDoTerminal(ref paraServidor);

                    if (controleConectado == 1)
                    {
                        if (contLive < 2)
                        {
                            EnviaParaTerminal("#live?");
                            contLive++;
                        }
                        else
                            break;
                    }
                    else if (controleConectado == -1)
                        break;
                    else
                    {
                        contLive = 0;
                        paraServidor = (paraServidor ?? string.Empty).Trim('\0', '\r', '\n', ' ');
                        if (string.IsNullOrEmpty(paraServidor)) { }
                        else if (paraServidor.CompareTo("#live") == 0) { }
                        else if (paraServidor.StartsWith("#queryprocessfailure")) { }
                        else
                        {
                            try
                            {
                                onReceive?.Invoke(this, paraServidor);
                            }
                            catch (Exception handlerEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Exception in onReceive handler: {handlerEx.Message}");
                            }
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
