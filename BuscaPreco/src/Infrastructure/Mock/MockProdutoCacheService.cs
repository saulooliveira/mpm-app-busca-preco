using System;
using System.Collections.Generic;
using System.Linq;
using BuscaPreco.Application.DTOs;
using BuscaPreco.Application.Interfaces;

namespace BuscaPreco.Infrastructure.Mock
{
    /// <summary>
    /// Implementação mock de IProdutoCacheService para uso em builds Debug.
    /// Gera 1000+ produtos proceduralmente em memória — sem DBF nem SQLite.
    /// </summary>
    public class MockProdutoCacheService : IProdutoCacheService
    {
        private readonly Dictionary<string, ProdutoCacheEntry> _produtos;

        // Categorias de produtos com variações para geração procedural
        private static readonly (string Prefixo, string[] Variacoes, string Unidade, decimal PrecoBase)[] _categorias =
        {
            ("LEITE",          new[] { "INTEGRAL", "DESNATADO", "SEMI DESNATADO", "UHT INTEGRAL", "CONDENSADO", "EM PO", "CACAU", "ZERO LACTOSE" }, "UN", 4.50m),
            ("ARROZ",          new[] { "TIPO 1 1KG", "TIPO 1 2KG", "TIPO 1 5KG", "PARBOILIZADO 1KG", "PARBOILIZADO 5KG", "INTEGRAL 1KG", "JASMINE 1KG" }, "UN", 6.00m),
            ("FEIJAO",         new[] { "CARIOCA 1KG", "PRETO 1KG", "FRADINHO 500G", "BRANCO 500G", "VERMELHO 1KG", "CARIOCA 500G" }, "UN", 7.50m),
            ("OLEO",           new[] { "SOJA 900ML", "SOJA 1.5L", "CANOLA 900ML", "MILHO 900ML", "GIRASSOL 900ML" }, "UN", 7.00m),
            ("ACUCAR",         new[] { "CRISTAL 1KG", "REFINADO 1KG", "DEMERARA 1KG", "MASCAVO 500G", "LIGHT 500G" }, "UN", 3.50m),
            ("FARINHA",        new[] { "TRIGO 1KG", "TRIGO 5KG", "MANDIOCA 500G", "MILHO 500G", "ROSCA 500G" }, "UN", 4.00m),
            ("CAFE",           new[] { "500G TRADICIONAL", "250G SUPERIOR", "500G EXTRAFORTE", "250G ORGANICO", "SOLUVEL 50G" }, "UN", 12.00m),
            ("MACARRAO",       new[] { "ESPAGUETE 500G", "PENNE 500G", "FUSILLI 500G", "PARAFUSO 500G", "TALHARIM 500G", "INTEGRAL 500G" }, "UN", 3.50m),
            ("SAL",            new[] { "REFINADO 1KG", "GROSSO 1KG", "LIGHT 500G", "MARINHO 500G" }, "UN", 2.20m),
            ("BISCOITO",       new[] { "CREAM CRACKER 200G", "MAISENA 200G", "AGUA SAL 200G", "RECHEADO CHOCOLATE 130G", "WAFER BAUNILHA 140G", "ROSQUINHA 200G" }, "UN", 2.80m),
            ("DETERGENTE",     new[] { "NEUTRO 500ML", "LIMAO 500ML", "COCO 500ML", "CONCENTRADO 300ML" }, "UN", 2.50m),
            ("SABAO",          new[] { "PO 1KG", "PO 500G", "BARRA 5X200G", "LIQUIDO 1L", "PO COLORED 800G" }, "UN", 8.00m),
            ("AMACIANTE",      new[] { "LAVANDA 1L", "BEBE 1L", "CONCENTRADO 500ML", "FLORAL 2L" }, "UN", 7.50m),
            ("SHAMPOO",        new[] { "ANTICASPA 400ML", "HIDRATACAO 400ML", "CACHOS 400ML", "RESSECADOS 350ML", "NUTRIÇÃO 350ML" }, "UN", 9.00m),
            ("CONDICIONADOR",  new[] { "HIDRATACAO 400ML", "CACHOS 400ML", "NUTRIÇÃO 350ML" }, "UN", 9.50m),
            ("CREME DENTAL",   new[] { "HORTELÃ 90G", "WHITENING 90G", "KIDS MORANGO 50G", "ORIGINAL 90G" }, "UN", 4.00m),
            ("SABONETE",       new[] { "LAVANDA 90G", "GLICERINA 90G", "ANTIBACTERIANO 90G", "AVEIA 90G", "LIMON 90G" }, "UN", 1.80m),
            ("PAPEL HIGIENICO",new[] { "16 ROLOS FOLHA DUPLA", "4 ROLOS FOLHA TRIPLA", "8 ROLOS NEUTRO", "12 ROLOS ECONOMICO" }, "UN", 14.00m),
            ("IOGURTE",        new[] { "NATURAL 170G", "MORANGO 120G", "COCO 120G", "ABACAXI 120G", "ZERO 120G", "GREGO 100G" }, "UN", 2.20m),
            ("QUEIJO",         new[] { "MUCARELA KG", "PRATO FATIADO 150G", "PARMESAO RALADO 50G", "REQUEIJAO 200G", "COTTAGE 200G" }, "UN", 15.00m),
            ("MANTEIGA",       new[] { "COM SAL 200G", "SEM SAL 200G", "LIGHT 200G" }, "UN", 8.50m),
            ("MARGARINA",      new[] { "COM SAL 500G", "SEM SAL 500G", "LIGHT 500G", "CULINARIA 500G" }, "UN", 5.50m),
            ("PRESUNTO",       new[] { "FATIADO 200G", "DEFUMADO 200G", "PERU 200G" }, "UN", 7.00m),
            ("REFRIGERANTE",   new[] { "COLA 2L", "LARANJA 2L", "GUARANA 2L", "LIMAO 2L", "COLE ZERO 2L", "UVA 1L", "TONICA 1L" }, "UN", 8.00m),
            ("SUCO",           new[] { "LARANJA 1L", "UVA 1L", "MANGA 200ML", "PESSEGO 200ML", "MARACUJA 1L" }, "UN", 5.00m),
            ("AGUA",           new[] { "MINERAL 500ML", "MINERAL 1.5L", "COM GAS 1L", "MINERAL 5L" }, "UN", 2.00m),
            ("ACHOCOLATADO",   new[] { "200ML CAIXINHA", "400G PO", "800G PO" }, "UN", 6.00m),
            ("CARNE BOVINA",   new[] { "ALCATRA KG", "FILE MIGNON KG", "CONTRAFILE KG", "PATINHO KG", "ACEM KG", "FRALDINHA KG" }, "KG", 45.00m),
            ("FRANGO",         new[] { "PEITO KG", "COXA KG", "AZA KG", "INTEIRO KG", "FILE KG" }, "KG", 18.00m),
            ("PEIXE",          new[] { "TILAPIA KG", "SARDINHA LATA 250G", "ATUM LATA 170G", "SALMAO KG" }, "UN", 22.00m),
            ("FRUTA",          new[] { "BANANA PRATA KG", "MACA KG", "LARANJA KG", "MAMAO KG", "MELAO KG", "UVA KG" }, "KG", 5.00m),
            ("VERDURA",        new[] { "ALFACE UN", "TOMATE KG", "CEBOLA KG", "ALHO 100G", "BATATA KG", "CENOURA KG" }, "UN", 4.00m),
            ("MOLHO",          new[] { "TOMATE 340G TRADICIONAL", "TOMATE 340G REFOGADO", "SHOYU 150ML", "INGLES 150ML", "MAIONESE 250G" }, "UN", 3.50m),
            ("TEMPERO",        new[] { "COLORAU 200G", "COMINHO 40G", "OREGANO 10G", "PIMENTA DO REINO 40G", "CURRY 40G" }, "UN", 2.80m),
            ("CONSERVA",       new[] { "AZEITONA 150G", "PALMITO 300G", "MILHO 200G", "ERVILHA 200G", "SELETA 170G" }, "UN", 4.50m),
            ("FARINHA DE ROSCA",new[] {"200G FINA","200G GROSSA" }, "UN", 3.00m),
            ("AMIDO DE MILHO", new[] { "500G", "200G" }, "UN", 3.20m),
            ("FERMENTO",       new[] { "BIOLOGICO 10G", "QUIMICO 100G" }, "UN", 2.50m),
            ("CHOCOLATE",      new[] { "AO LEITE 100G", "MEIO AMARGO 100G", "BRANCO 100G", "BARRA 170G" }, "UN", 6.50m),
            ("PIPOCA",         new[] { "MICRO-ONDAS MANTEIGA 100G", "MICRO-ONDAS LIGHT 100G", "GRÃO 500G" }, "UN", 3.00m),
            ("SALGADINHO",     new[] { "BATATA 50G", "BATATA 100G", "AMENDOIM 200G", "MILHO 50G" }, "UN", 3.00m),
            ("CEREAL",         new[] { "AVEIA 200G", "AVEIA 500G", "GRANOLA 300G", "CORN FLAKES 500G" }, "UN", 5.50m),
            ("BOLACHA",        new[] { "TORRADA 200G", "MARIA 200G", "CHAMPAGNE 200G" }, "UN", 3.00m),
            ("GELATINA",       new[] { "MORANGO 15G", "ABACAXI 15G", "UVA 15G", "LIMAO 15G" }, "UN", 1.50m),
            ("EXTRATO",        new[] { "TOMATE 340G", "TOMATE 130G" }, "UN", 2.50m),
            ("VINAGRE",        new[] { "ALCOOL 750ML", "MACA 500ML", "ARROZ 500ML" }, "UN", 3.00m),
            ("AZEITE",         new[] { "EXTRA VIRGEM 250ML", "EXTRA VIRGEM 500ML", "COMPOSTO 500ML" }, "UN", 18.00m),
            ("ABSORVENTE",     new[] { "COM ABAS 8UN", "SEM ABAS 8UN", "DIARIO 15UN" }, "UN", 4.50m),
            ("FRALDA",         new[] { "RN 20UN", "P 20UN", "M 18UN", "G 16UN", "XG 14UN" }, "UN", 28.00m),
            ("LENCO UMEDECIDO",new[] { "BEBE 50UN", "TOALHA UMEDECIDA 20UN" }, "UN", 6.00m),
            ("CARVAO",         new[] { "2KG", "3KG", "5KG" }, "UN", 12.00m),
        };

        public MockProdutoCacheService()
        {
            _produtos = GerarProdutos();
        }

        private static Dictionary<string, ProdutoCacheEntry> GerarProdutos()
        {
            var dict = new Dictionary<string, ProdutoCacheEntry>(StringComparer.OrdinalIgnoreCase);
            var rng = new Random(42); // seed fixo para resultados reproduzíveis
            long codigoBase = 7890000100001L;

            foreach (var (prefixo, variacoes, unidade, precoBase) in _categorias)
            {
                foreach (var variacao in variacoes)
                {
                    // Cada variação gera entre 3 e 8 SKUs (tamanhos/marcas diferentes)
                    int skus = rng.Next(3, 9);
                    string[] marcas = GerarMarcas(prefixo, rng);

                    for (int i = 0; i < skus; i++)
                    {
                        string codigo = GerarEan13(codigoBase++);
                        string marca = marcas[i % marcas.Length];
                        string descricao = $"{marca} {prefixo} {variacao}".ToUpper();
                        // preço com variação de ±30%
                        decimal fator = (decimal)(0.70 + rng.NextDouble() * 0.60);
                        decimal preco = Math.Round(precoBase * fator, 2);

                        dict[codigo] = new ProdutoCacheEntry
                        {
                            CodigoBarras     = codigo,
                            Descricao        = Truncar(descricao, 30),
                            Preco            = preco,
                            Unidade          = unidade,
                            UltimaAtualizacao = DateTime.Now
                        };
                    }
                }
            }

            return dict;
        }

        /// <summary>Gera marcas fictícias temáticas por categoria.</summary>
        private static string[] GerarMarcas(string prefixo, Random rng)
        {
            // Marcas genéricas que funcionam para qualquer categoria
            string[] genericas = { "MARCA A", "MARCA B", "MARCA C", "MARCA D", "MARCA E",
                                   "MARCA F", "MARCA G", "MARCA H" };
            // Embaralha com o seed já fixado
            return genericas.OrderBy(_ => rng.Next()).ToArray();
        }

        /// <summary>Calcula dígito verificador EAN-13 e retorna o código como string.</summary>
        private static string GerarEan13(long numero)
        {
            string s = numero.ToString().PadLeft(12, '0')[..12];
            int soma = 0;
            for (int i = 0; i < 12; i++)
                soma += (s[i] - '0') * (i % 2 == 0 ? 1 : 3);
            int dv = (10 - soma % 10) % 10;
            return s + dv;
        }

        private static string Truncar(string s, int max) =>
            s.Length <= max ? s : s[..max];

        // ── IProdutoCacheService ──────────────────────────────────────────────

        public ProdutoCacheEntry BuscarPorCodigo(string codigoBarras)
        {
            _produtos.TryGetValue(codigoBarras, out var entry);
            return entry;
        }

        public List<ProdutoCacheEntry> ListarTodos() =>
            _produtos.Values.ToList();

        public void SincronizarAgora() { /* no-op em modo mock */ }
    }
}
