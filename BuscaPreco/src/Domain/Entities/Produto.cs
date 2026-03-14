using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuscaPreco.Domain.Entities
{
    public class Produto
    {
        public string Unidade { get; set; } = "";       // "un" ou "kg"
        public string CodigoItem { get; set; } = "";   // Código do Item
        public decimal Preco { get; set; } = 0;      // Preço do Item
        public string DiasValidade { get; set; } = "";   // Dias de validade (VVV)
        public string Descricao1 { get; set; } = "";   // Descrição do Item - 1ª Linha
        public string Descricao2 { get; set; } = "";    // Descrição do Item - 2ª Linha
        public string CodigoFornecedor { get; set; } = "";// Código do Fornecedor
        public string CodigoImagem { get; set; } = "";  // Código da Imagem
        public string EANFornecedor { get; set; } = "";// EAN do Fornecedor
        public string PercentualGlaciamento { get; set; } = ""; // Percentual de Glaciamento
        public string PrecoPromocional { get; set; } = "";// Preço Promocional
        public Produto()
        {

        }
        public Produto(string unidade, string codigoItem, decimal preco, string diasValidade,
                       string descricao1, string descricao2, string codigoFornecedor, string codigoImagem,
                       string eanFornecedor, string percentualGlaciamento, string precoPromocional)
        {
            Unidade = unidade;
            CodigoItem = codigoItem;
            Preco = preco;
            DiasValidade = diasValidade;
            Descricao1 = descricao1;
            Descricao2 = descricao2;
            CodigoFornecedor = codigoFornecedor;
            CodigoImagem = codigoImagem;
            EANFornecedor = eanFornecedor;
            PercentualGlaciamento = percentualGlaciamento;
            PrecoPromocional = precoPromocional;
        }
    }

}
