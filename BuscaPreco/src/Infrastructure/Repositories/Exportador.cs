using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Infrastructure.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class Exportador
    {
        /*
                   // Criar lista de produtos
            var produtos = new List<Produto>
        {
            new Produto("un", "123456", 123.45m, "010", "Descrição 1", "Descrição 2", "0001", "0001", "1234567890123", "12", "123456"),
            new Produto("kg", "654321", 67.89m, "999", "Outro Item", "Mais detalhes", "0002", "0002", "9876543210987", "15", "234567")
        };

            // Caminho para o arquivo de saída
            string caminhoArquivo = "Itensmgv.txt";

            // Exportar dados para o arquivo
            Exportador.ExportarParaMGV7(produtos, caminhoArquivo);*/
        public static void ExportarParaMGV7(List<Produto> produtos, string caminhoArquivo)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(caminhoArquivo, false, Encoding.ASCII))
                {
                    foreach (var produto in produtos)
                    {
                        StringBuilder linha = new StringBuilder();

                        // Fixando o código do departamento
                        string codigoDepartamento = "01".PadRight(2, ' '); // DD

                        // Tipo de Produto: se for "un" => 1, se for "kg" => 0
                        string tipoProduto = (produto.Unidade.ToLower() == "uni" ? "1" : "0").PadRight(1, ' '); // T

                        // Impressão da Data de Embalagem: fixado como 1
                        string imprimeDataEmbalagem = "1".PadRight(1, ' '); // DE

                        // Montar a linha de acordo com a estrutura fornecida
                        linha.Append(codigoDepartamento);  // DD
                        linha.Append(tipoProduto);         // T
                        linha.Append(produto.CodigoItem.PadLeft(6, '0'));  // CCCCC (6 caracteres)
                        linha.Append((produto.Preco * 100).ToString("000000").PadLeft(6, '0')); // Preço em centavos (PPPPPP)
                        linha.Append(produto.DiasValidade.PadRight(3, '0')); // VVV
                        linha.Append(produto.Descricao1.PadRight(25, ' ').Substring(0,25));  // D1 (25 caracteres)
                        linha.Append(produto.Descricao2.PadRight(25, ' ').Substring(0, 25));  // D2 (25 caracteres)
                        linha.Append("000000".PadRight(6, '0'));             // RRRRRR (Campo extra, assumido vazio)
                        linha.Append("0000".PadRight(4, '0'));               // FFFF (Código de imagem, assumido vazio)
                        linha.Append("000000".PadRight(6, '0'));             // IIIIII (Código de informação nutricional)
                        linha.Append("0".PadRight(1, '0'));                  // DV (Impressão da validade, assumido "0")
                        linha.Append(imprimeDataEmbalagem);                  // DE (Impressão da embalagem, fixado "1")
                        linha.Append(produto.CodigoFornecedor.PadRight(4, '0')); // CF (Código de fornecedor)
                        linha.Append("000000000000".PadRight(12, ' '));        // L (Lote, assumido vazio)
                        linha.Append("00000000000".PadRight(11, ' '));         // G (EAN-13 especial, assumido vazio)
                        linha.Append("0".PadRight(1, ' '));                    // Z (Versão do preço)
                        linha.Append("0000".PadRight(4, ' '));                 // G1 (Código EAN-13 especial, assumido vazio)
                        linha.Append(produto.PercentualGlaciamento.PadLeft(4, '0').PadRight(4, ' ')); // PG (Percentual de glaciamento)
                        linha.Append(produto.PrecoPromocional.PadLeft(6, '0')); // Preço promocional (PPPPPP)
                        linha.Append("00");        // SF (Unidade, assumido)
                        linha.Append("0");                                  // Separador de campo
                        linha.Append("000000000");                           // Campo para BNA, assumido vazio
                        linha.Append("0000");                                // ST (Assumido como "0000")
                        linha.Append("000000000000".PadRight(12, ' '));        // G1 (Código de EAN-13 especial, assumido vazio)
                        linha.Append("|01|\r\n");                                // Final da linha (CR+LF)

                        writer.Write(linha.ToString());
                    }
                }

                Console.WriteLine("Arquivo exportado com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao exportar os dados: {ex.Message}");
            }
        }
    }
}
