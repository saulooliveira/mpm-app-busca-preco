namespace BuscaPreco.Domain.Entities
{
    public class Produto
    {
        public string Unidade { get; set; } = "";
        public string CodigoItem { get; set; } = "";
        public decimal Preco { get; set; } = 0;
        public string DiasValidade { get; set; } = "";
        public string Descricao1 { get; set; } = "";
        public string Descricao2 { get; set; } = "";
        public string CodigoFornecedor { get; set; } = "";
        public string CodigoImagem { get; set; } = "";
        public string EANFornecedor { get; set; } = "";
        public string PercentualGlaciamento { get; set; } = "";
        public string PrecoPromocional { get; set; } = "";

        public Produto() { }

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
