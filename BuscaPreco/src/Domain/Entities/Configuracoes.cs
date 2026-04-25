using BuscaPreco.CrossCutting;

namespace BuscaPreco.Domain.Entities
{
    public class Configuracoes
    {
        public string IPServer { get; set; } = string.Empty;
        public string IPCliente { get; set; } = string.Empty;
        public string Mascara { get; set; } = string.Empty;
        public string TLinha1 { get; set; } = string.Empty;
        public string TLinha2 { get; set; } = string.Empty;
        public string TLinha3 { get; set; } = string.Empty;
        public string TLinha4 { get; set; } = string.Empty;
        public int Tempo { get; set; }

        public int IPDinamico { get; set; }
        public int BuscaServidor { get; set; }

        public string Gateway { get; set; } = string.Empty;
        public string ServidorNomes { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string EndUpdate { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Pass { get; set; } = string.Empty;

        public string montaParamConfig() =>
            "#rparamconfig" +
            (IPDinamico == 0 ? '0' : '1') +
            (BuscaServidor == 0 ? '0' : '1');

        public string montaUpdateConfig() =>
            "#rupdconfig" +
            GertecProtocol.LenChar(Gateway) + Gateway +
            GertecProtocol.LenChar(ServidorNomes) + ServidorNomes +
            GertecProtocol.LenChar(Nome) + Nome +
            GertecProtocol.LenChar(EndUpdate) + EndUpdate +
            GertecProtocol.LenChar(User) + User +
            GertecProtocol.LenChar(Pass) + Pass;

        public string montaConfig() =>
            "#reconf02" +
            GertecProtocol.LenChar(IPServer) + IPServer +
            GertecProtocol.LenChar(IPCliente) + IPCliente +
            GertecProtocol.LenChar(Mascara) + Mascara +
            GertecProtocol.LenChar(TLinha1) + TLinha1 +
            GertecProtocol.LenChar(TLinha2) + TLinha2 +
            GertecProtocol.LenChar(TLinha3) + TLinha3 +
            GertecProtocol.LenChar(TLinha4) + TLinha4 +
            (char)(Tempo + 48);

        public void ProcessaConfig(string str)
        {
            str = (str ?? string.Empty).Trim('\0', '\r', '\n', ' ');
            if (string.IsNullOrEmpty(str) || !str.StartsWith("#config02")) return;
            str = str.Substring("#config02".Length);

            IPServer = GertecProtocol.ParseField(ref str);
            IPCliente = GertecProtocol.ParseField(ref str);
            Mascara = GertecProtocol.ParseField(ref str);
            TLinha1 = GertecProtocol.ParseField(ref str);
            TLinha2 = GertecProtocol.ParseField(ref str);
            TLinha3 = GertecProtocol.ParseField(ref str);
            TLinha4 = GertecProtocol.ParseField(ref str);
            Tempo = string.IsNullOrEmpty(str) ? 0 : str[0] - '0';
        }

        public void ProcessaParam(string str)
        {
            str = (str ?? string.Empty).Trim('\0', '\r', '\n', ' ');
            if (string.IsNullOrEmpty(str) || !str.StartsWith("#paramconfig")) return;
            str = str.Substring("#paramconfig".Length);
            if (str.Length < 2) return;
            IPDinamico = str[0] - 48;
            BuscaServidor = str[1] - 48;
        }

        public void ProcessaUpdate(string str)
        {
            str = (str ?? string.Empty).Trim('\0', '\r', '\n', ' ');
            if (string.IsNullOrEmpty(str) || !str.StartsWith("#updconfig")) return;
            str = str.Substring("#updconfig".Length);

            Gateway = GertecProtocol.ParseField(ref str);
            ServidorNomes = GertecProtocol.ParseField(ref str);
            Nome = GertecProtocol.ParseField(ref str);
            EndUpdate = GertecProtocol.ParseField(ref str);
            User = GertecProtocol.ParseField(ref str);
            Pass = GertecProtocol.ParseField(ref str);
        }
    }
}
