using System;
using System.Collections.Generic;
using System.Text;

namespace BuscaPreco.Domain.Entities
{
    public class Configuracoes
    {
        public string IPServer;
        public string IPCliente;
        public string Mascara;
        public string TLinha1;
        public string TLinha2;
        public string TLinha3;
        public string TLinha4;
        public int Tempo;

        public int IPDinamico;
        public int BuscaServidor;

        public string Gateway;
        public string ServidorNomes;
        public string Nome;
        public string EndUpdate;
        public string User;
        public string Pass;

        /*
         Método: montaParamConfig
         Função: cria a string para enviar para o terminal (#paramconfig)
          
         Retorno: string para ser enviada
         */
        public string montaParamConfig() {
            string str = "#rparamconfig" + 
                (IPDinamico == 0 ? '0' : '1') + 
                (BuscaServidor == 0 ? '0' : '1');
            return str;
        }

        /*
         Método: montaParamConfig
         Função: cria a string para enviar para o terminal (#updateconfig)
          
         Retorno: string para ser enviada
         */
        public string montaUpdateConfig() {
            char tamGateway = (char)(Gateway.Length + 48);
            char tamServNome = (char)(ServidorNomes.Length + 48);
            char tamNome = (char)(Nome.Length + 48);
            char tamEndUpdate = (char)(EndUpdate.Length + 48);
            char tamUser = (char)(User.Length + 48);
            char tamPass = (char)(Pass.Length + 48);

            string str = "#rupdconfig" +
                tamGateway + Gateway +
                tamServNome + ServidorNomes +
                tamNome + Nome +
                tamEndUpdate + EndUpdate +
                tamUser + User +
                tamPass + Pass;

            return str;
        }

        /*
         Método: montaParamConfig
         Função: cria a string para enviar para o terminal (#reconf02)
          
         Retorno: string para ser enviada
         */
        public string montaConfig() {
            char tamIPServer = (char)(IPServer.Length + 48);
            char tamIPCliente = (char)(IPCliente.Length + 48);
            char tamMascara = (char)(Mascara.Length + 48);
            char tamTLinha1 = (char)(TLinha1.Length + 48);
            char tamTLinha2 = (char)(TLinha2.Length + 48);
            char tamTLinha3 = (char)(TLinha3.Length + 48);
            char tamTLinha4 = (char)(TLinha4.Length + 48);
            
            string str = "#reconf02" +
                tamIPServer + IPServer +
                tamIPCliente + IPCliente +
                tamMascara + Mascara +
                tamTLinha1 + TLinha1 +
                tamTLinha2 + TLinha2 +
                tamTLinha3 + TLinha3 +
                tamTLinha4 + TLinha4 +
                (char)(Tempo + 48);
            return str;
        }
        /*
         Método: ProcessaConfig
         Função: Trata o recebimento das configurações vindas do terminal
         
         Entrada: str - comando
         */
        public void ProcessaConfig(string str)
        {
            int tamanho;
            str = str.Substring(9);

            char[] strchar = str.ToCharArray();
            tamanho = (int)(strchar[0] - 48);
            IPServer = str.Substring(1, tamanho);
            str = str.Substring(tamanho + 1);

            strchar = str.ToCharArray();
            tamanho = (int)(strchar[0] - 48);
            IPCliente = str.Substring(1, tamanho);
            str = str.Substring(tamanho + 1);

            strchar = str.ToCharArray();
            tamanho = (int)(strchar[0] - 48);
            Mascara = str.Substring(1, tamanho);
            str = str.Substring(tamanho + 1);

            strchar = str.ToCharArray();
            tamanho = (int)(strchar[0] - 48);
            TLinha1 = str.Substring(1, tamanho);
            str = str.Substring(tamanho + 1);

            strchar = str.ToCharArray();
            tamanho = (int)(strchar[0] - 48);
            TLinha2 = str.Substring(1, tamanho);
            str = str.Substring(tamanho + 1);

            strchar = str.ToCharArray();
            tamanho = (int)(strchar[0] - 48);
            TLinha3 = str.Substring(1, tamanho);
            str = str.Substring(tamanho + 1);

            strchar = str.ToCharArray();
            tamanho = (int)(strchar[0] - 48);
            TLinha4 = str.Substring(1, tamanho);
            str = str.Substring(tamanho + 1);

            strchar = str.ToCharArray();
            Tempo = (int)(strchar[0] - '0');
        }

        /*
         Método: ProcessaConfig
         Função: Trata o recebimento das configurações vindas do terminal
         
         Entrada: str - comando
         */
        public void ProcessaParam(string str)
        {
            str = str.Substring(12);

            char[] strchar = str.ToCharArray();
            IPDinamico = strchar[0] - 48;
            BuscaServidor = strchar[1] - 48;
        }

        /*
         Método: ProcessaConfig
         Função: Trata o recebimento das configurações vindas do terminal
         
         Entrada: str - comando
         */
        public void ProcessaUpdate(string str)
        {
            int tamanho;
            str = str.Substring(10);

            char[] strchar = str.ToCharArray();
            tamanho = (int)(strchar[0] - 48);
            Gateway = str.Substring(1, tamanho);
            str = str.Substring(tamanho + 1);

            strchar = str.ToCharArray();
            tamanho = (int)(strchar[0] - 48);
            ServidorNomes = str.Substring(1, tamanho);
            str = str.Substring(tamanho + 1);

            strchar = str.ToCharArray();
            tamanho = (int)(strchar[0] - 48);
            Nome = str.Substring(1, tamanho);
            str = str.Substring(tamanho + 1);

            strchar = str.ToCharArray();
            tamanho = (int)(strchar[0] - 48);
            EndUpdate = str.Substring(1, tamanho);
            str = str.Substring(tamanho + 1);

            strchar = str.ToCharArray();
            tamanho = (int)(strchar[0] - 48);
            User = str.Substring(1, tamanho);
            str = str.Substring(tamanho + 1);

            strchar = str.ToCharArray();
            tamanho = (int)(strchar[0] - 48);
            Pass = str.Substring(1, tamanho);
            str = str.Substring(tamanho + 1);
        }
    }
}
