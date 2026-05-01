using System.Collections.Generic;
using System.IO;
using System.Text;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Infrastructure.Repositories
{
    public class Exportador
    {
        public static void ExportarParaMGV7(List<Produto> produtos, string caminhoArquivo)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(caminhoArquivo, false, Encoding.ASCII))
                {
                    foreach (var produto in produtos)
                    {
                        StringBuilder linha = new StringBuilder();

                        string codigoDepartamento = "01".PadRight(2, ' ');
                        string tipoProduto = (produto.Unidade.ToLower() == "uni" ? "1" : "0").PadRight(1, ' ');
                        string imprimeDataEmbalagem = "1".PadRight(1, ' ');

                        linha.Append(codigoDepartamento);
                        linha.Append(tipoProduto);
                        linha.Append(produto.CodigoItem.PadLeft(6, '0'));
                        linha.Append((produto.Preco * 100).ToString("000000").PadLeft(6, '0'));
                        linha.Append(produto.DiasValidade.PadRight(3, '0'));
                        linha.Append(produto.Descricao1.PadRight(25, ' ').Substring(0, 25));
                        linha.Append(produto.Descricao2.PadRight(25, ' ').Substring(0, 25));
                        linha.Append("000000".PadRight(6, '0'));
                        linha.Append("0000".PadRight(4, '0'));
                        linha.Append("000000".PadRight(6, '0'));
                        linha.Append("0".PadRight(1, '0'));
                        linha.Append(imprimeDataEmbalagem);
                        linha.Append(produto.CodigoFornecedor.PadRight(4, '0'));
                        linha.Append("000000000000".PadRight(12, ' '));
                        linha.Append("00000000000".PadRight(11, ' '));
                        linha.Append("0".PadRight(1, ' '));
                        linha.Append("0000".PadRight(4, ' '));
                        linha.Append(produto.PercentualGlaciamento.PadLeft(4, '0').PadRight(4, ' '));
                        linha.Append(produto.PrecoPromocional.PadLeft(6, '0'));
                        linha.Append("00");
                        linha.Append("0");
                        linha.Append("000000000");
                        linha.Append("0000");
                        linha.Append("000000000000".PadRight(12, ' '));
                        linha.Append("|01|\r\n");

                        writer.Write(linha.ToString());
                    }
                }
            }
            catch { }
        }
    }
}
