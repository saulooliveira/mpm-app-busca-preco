using BuscaPreco.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace BuscaPreco.Pages;

public class RelatorioModel : PageModel
{
    private readonly ConsultaRepository _repo;

    [BindProperty(SupportsGet = true)]
    public DateTime Inicio { get; set; } = DateTime.Today.AddDays(-6);

    [BindProperty(SupportsGet = true)]
    public DateTime Fim { get; set; } = DateTime.Today;

    public int Total { get; private set; }
    public int Encontrados { get; private set; }
    public int NaoCadastrados { get; private set; }
    public List<(string codigo, string nome, int qtd)> TopProdutos { get; private set; } = [];
    public int[] PorHora { get; private set; } = new int[24];
    public int[] PorDia { get; private set; } = new int[7];

    public string PorHoraJson => JsonSerializer.Serialize(PorHora);
    public string PorDiaJson => JsonSerializer.Serialize(PorDia);

    public RelatorioModel(ConsultaRepository repo)
    {
        _repo = repo;
    }

    public IActionResult OnGet()
    {
        if (Inicio > Fim) Fim = Inicio;

        (Total, Encontrados, NaoCadastrados) = _repo.ResumoNoPeriodo(Inicio, Fim);
        TopProdutos = _repo.TopProdutos(Inicio, Fim);
        PorHora = _repo.ConsultasPorHora(Inicio, Fim);
        PorDia = _repo.ConsultasPorDiaSemana(Inicio, Fim);

        return Page();
    }
}
