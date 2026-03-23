using System;
using System.Collections.Generic;
using System.Linq;
using BuscaPreco.CrossCutting;
using BuscaPreco.Infrastructure.Database;
using BuscaPreco.Infrastructure.Repositories;

namespace BuscaPreco.Infrastructure.Mock
{
    /// <summary>
    /// Mock de ConsultaRepository para builds Debug.
    /// Armazena consultas em memória e pré-popula ~150 consultas históricas
    /// espalhadas nos últimos 7 dias para visualização nos relatórios.
    /// </summary>
    public class MockConsultaRepository : ConsultaRepository
    {
        private readonly List<ConsultaMock> _consultas = new();
        private readonly Logger _logger;

        private record ConsultaMock(
            DateTime DataHora,
            string CodigoBarras,
            string Nome,
            string Preco,
            string Status,
            string Origem);

        // Amostra de produtos com código + nome para o histórico
        private static readonly (string Cod, string Nome, string Preco)[] _produtosSeed =
        {
            ("7890000100001", "MARCA A LEITE INTEGRAL",      "4.89"),
            ("7890000100002", "MARCA B LEITE DESNATADO",     "4.49"),
            ("7890000100010", "MARCA C ARROZ TIPO 1 5KG",    "22.90"),
            ("7890000100015", "MARCA A FEIJAO CARIOCA 1KG",  "7.45"),
            ("7890000100020", "MARCA D OLEO SOJA 900ML",     "6.89"),
            ("7890000100025", "MARCA B ACUCAR CRISTAL 1KG",  "3.99"),
            ("7890000100030", "MARCA A CAFE 500G TRADICIONAL","14.99"),
            ("7890000100035", "MARCA C MACARRAO ESPAGUETE",  "3.79"),
            ("7890000100040", "MARCA E BISCOITO CREAM CRACKER","2.80"),
            ("7890000100045", "MARCA A DETERGENTE NEUTRO",   "2.50"),
            ("7890000100050", "MARCA B SABAO PO 1KG",        "8.00"),
            ("7890000100055", "MARCA C SHAMPOO ANTICASPA",   "9.00"),
            ("7890000100060", "MARCA A CREME DENTAL HORTELÃ","4.00"),
            ("7890000100065", "MARCA F REFRIGERANTE COLA 2L","8.00"),
            ("7890000100070", "MARCA B IOGURTE MORANGO 120G","2.20"),
            ("7890000100075", "MARCA D QUEIJO MUSSARELA KG", "45.00"),
            ("7890000100080", "MARCA A MANTEIGA COM SAL 200G","8.50"),
            ("7890000100085", "MARCA C FRANGO PEITO KG",     "18.90"),
            ("7890000100090", "MARCA B PAPEL HIG 16 ROLOS",  "14.50"),
            ("7890000100095", "MARCA A AGUA MINERAL 1.5L",   "2.00"),
        };

        private static readonly string[] _codigosNaoEncontrados =
        {
            "1234567890128", "9999999999994", "0000000000000",
            "1111111111110", "5555555555551"
        };

        public MockConsultaRepository(Logger logger)
            : base(null!, logger) // não usa o DbContext (override completo)
        {
            _logger = logger;
            PopularHistorico();
        }

        private void PopularHistorico()
        {
            var rng = new Random(7);
            var agora = DateTime.Now;

            // Distribui ~150 consultas nos últimos 7 dias
            for (int diaAtras = 0; diaAtras < 7; diaAtras++)
            {
                // Mais consultas nos dias mais recentes
                int qtdDia = diaAtras == 0 ? 30 : (diaAtras == 1 ? 25 : rng.Next(10, 22));

                for (int i = 0; i < qtdDia; i++)
                {
                    // Horário aleatório com pico entre 08h e 18h
                    int hora = PicarHora(rng);
                    int min  = rng.Next(0, 60);
                    int seg  = rng.Next(0, 60);

                    var dataHora = agora.Date.AddDays(-diaAtras)
                                         .AddHours(hora).AddMinutes(min).AddSeconds(seg);

                    // 85% produto encontrado, 15% não cadastrado
                    bool encontrado = rng.NextDouble() < 0.85;

                    if (encontrado)
                    {
                        var seed = _produtosSeed[rng.Next(_produtosSeed.Length)];
                        _consultas.Add(new ConsultaMock(
                            dataHora, seed.Cod, seed.Nome, seed.Preco,
                            "Encontrado", "MOCK"));
                    }
                    else
                    {
                        var cod = _codigosNaoEncontrados[rng.Next(_codigosNaoEncontrados.Length)];
                        _consultas.Add(new ConsultaMock(
                            dataHora, cod, string.Empty, "0,00",
                            "Não Cadastrado", "MOCK"));
                    }
                }
            }

            _logger.Info("[MOCK] ConsultaRepository inicializado com {Count} consultas históricas.", _consultas.Count);
        }

        /// <summary>Distribui horários com pico comercial (08h-18h).</summary>
        private static int PicarHora(Random rng)
        {
            double p = rng.NextDouble();
            if (p < 0.10) return rng.Next(7, 9);   // manhã cedo
            if (p < 0.55) return rng.Next(9, 13);  // pico manhã
            if (p < 0.70) return rng.Next(13, 15); // almoço
            if (p < 0.95) return rng.Next(15, 19); // pico tarde
            return rng.Next(19, 22);                // noite
        }

        // ── Override de todos os métodos públicos ────────────────────────────

        public override void Gravar(string codigoBarras, string nome, string preco,
                               bool encontrado, string origem)
        {
            _consultas.Add(new ConsultaMock(
                DateTime.Now, codigoBarras,
                nome ?? string.Empty,
                preco ?? string.Empty,
                encontrado ? "Encontrado" : "Não Cadastrado",
                "MOCK"));
        }

        public override (int total, int encontrados, int naoCadastrados) ResumoNoPeriodo(
            DateTime inicio, DateTime fim)
        {
            var filtradas = FiltrarPeriodo(inicio, fim);
            return (
                filtradas.Count,
                filtradas.Count(c => c.Status == "Encontrado"),
                filtradas.Count(c => c.Status == "Não Cadastrado"));
        }

        public override List<(string codigo, string nome, int qtd)> TopProdutos(
            DateTime inicio, DateTime fim, int top = 200)
        {
            return FiltrarPeriodo(inicio, fim)
                .Where(c => c.Status == "Encontrado")
                .GroupBy(c => c.CodigoBarras)
                .Select(g => (g.Key, g.Max(c => c.Nome), g.Count()))
                .OrderByDescending(t => t.Item3)
                .Take(top)
                .ToList();
        }

        public override int[] ConsultasPorHora(DateTime inicio, DateTime fim)
        {
            var result = new int[24];
            foreach (var c in FiltrarPeriodo(inicio, fim))
                result[c.DataHora.Hour]++;
            return result;
        }

        public override int[] ConsultasPorDiaSemana(DateTime inicio, DateTime fim)
        {
            var result = new int[7];
            foreach (var c in FiltrarPeriodo(inicio, fim))
                result[(int)c.DataHora.DayOfWeek]++;
            return result;
        }

        private List<ConsultaMock> FiltrarPeriodo(DateTime inicio, DateTime fim)
        {
            var de  = inicio.Date;
            var ate = fim.Date.AddDays(1);
            return _consultas
                .Where(c => c.DataHora >= de && c.DataHora < ate)
                .ToList();
        }
    }
}
