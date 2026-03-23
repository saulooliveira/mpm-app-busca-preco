using System;

namespace BuscaPreco.Application.DTOs
{
    /// <summary>
    /// Entrada do cache L1 (memÃ³ria) para um produto.
    /// </summary>
    public class ProdutoCacheEntry
    {
        public string CodigoBarras { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public string Unidade { get; set; } = string.Empty;
        public DateTime UltimaAtualizacao { get; set; }
    }
}
