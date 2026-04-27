using BuscaPreco.Application.Configurations;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Infrastructure.Data;
using BuscaPreco.Infrastructure.Repositories;
using BuscaPreco.Infrastructure.Scrapers;
using BuscaPreco.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Presentation.Electron;

public static class ApiEndpoints
{
    public static void Map(WebApplication app)
    {
        MapTerminaisApi(app);
        MapRelatorioApi(app);
        MapProdutosApi(app);
        MapEtiquetaApi(app);
    }

    private static void MapTerminaisApi(WebApplication app)
    {
        app.MapGet("/api/terminais", (Servidor servidor) =>
        {
            return servidor.GetTerminaisSnapshot().Select((t, i) => new
            {
                index = i,
                descricao = t.ToString(),
                tipo = t.Tipo,
                versao = t.Versao,
                macAddress = t.MacAddress,
                isG2SComAudio = t.IsG2SComAudio
            });
        });

        app.MapGet("/api/terminais/{index:int}", (int index, Servidor servidor) =>
        {
            var t = servidor.GetTerminal(index);
            if (t == null) return Results.NotFound();

            var c = t.config;
            return Results.Ok(new
            {
                ipCliente = c.IPCliente,
                ipServidor = c.IPServer,
                mascara = c.Mascara,
                linha1 = c.TLinha1,
                linha2 = c.TLinha2,
                linha3 = c.TLinha3,
                linha4 = c.TLinha4,
                tempo = c.Tempo,
                gateway = c.Gateway,
                servidorNomes = c.ServidorNomes,
                nome = c.Nome,
                ftpServidor = c.EndUpdate,
                usuario = c.User,
                senha = c.Pass,
                ipDinamico = c.IPDinamico != 0,
                buscaServidor = c.BuscaServidor != 0
            });
        });

        app.MapPost("/api/terminais/{index:int}/config", (
            int index,
            [FromBody] ConfigPayload payload,
            Servidor servidor) =>
        {
            var t = servidor.GetTerminal(index);
            if (t == null) return Results.NotFound();

            t.SendReconf02(
                payload.IpServidor, payload.IpCliente, payload.Mascara,
                payload.Linha1, payload.Linha2, payload.Linha3, payload.Linha4,
                payload.Tempo);
            return Results.Ok(new { ok = true });
        });

        app.MapPost("/api/terminais/{index:int}/params", (
            int index,
            [FromBody] ParamsPayload payload,
            Servidor servidor) =>
        {
            var t = servidor.GetTerminal(index);
            if (t == null) return Results.NotFound();

            t.SendRparamconfig(payload.IpDinamico);
            return Results.Ok(new { ok = true });
        });

        app.MapPost("/api/terminais/{index:int}/update", (
            int index,
            [FromBody] UpdatePayload payload,
            Servidor servidor) =>
        {
            var t = servidor.GetTerminal(index);
            if (t == null) return Results.NotFound();

            t.SendRupdconfig(payload.Gateway, payload.Nome);
            return Results.Ok(new { ok = true });
        });
    }

    private static void MapRelatorioApi(WebApplication app)
    {
        app.MapGet("/api/relatorio", (
            DateTime? inicio,
            DateTime? fim,
            ConsultaRepository consultaRepository) =>
        {
            var ini = inicio?.Date ?? DateTime.Today.AddDays(-6);
            var fi = fim?.Date ?? DateTime.Today;
            if (ini > fi) fi = ini;

            var (total, encontrados, naoCadastrados) = consultaRepository.ResumoNoPeriodo(ini, fi);
            var topProdutos = consultaRepository.TopProdutos(ini, fi);
            var porHora = consultaRepository.ConsultasPorHora(ini, fi);
            var porDia = consultaRepository.ConsultasPorDiaSemana(ini, fi);

            return Results.Ok(new
            {
                total,
                encontrados,
                naoCadastrados,
                topProdutos = topProdutos.Select(p => new { p.codigo, p.nome, p.qtd }),
                porHora,
                porDia
            });
        });

        app.MapGet("/api/relatorio/preco", (string codigo, ConsultaRepository consultaRepository) =>
        {
            var preco = consultaRepository.BuscarPrecoMaisRecente(codigo);
            return Results.Ok(new { preco = string.IsNullOrWhiteSpace(preco) ? "0,00" : preco });
        });
    }

    private static void MapProdutosApi(WebApplication app)
    {
        app.MapGet("/api/produtos", (string? q, IBuscaPrecosService buscaPrecosService) =>
        {
            var todos = buscaPrecosService.ListarTudo();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var upper = q.ToUpperInvariant();
                todos = todos
                    .Where(p =>
                        (p.Descricao1 ?? string.Empty).ToUpperInvariant().Contains(upper) ||
                        (p.CodigoItem ?? string.Empty).Contains(q))
                    .ToList();
            }
            return todos.Take(300).Select(p => new { p.CodigoItem, p.Descricao1 });
        });

        app.MapGet("/api/fixados", (IOptions<ProdutosFixadosConfig> opts) =>
            Results.Ok(opts.Value.Codigos ?? new List<string>()));

        app.MapPost("/api/fixados", ([FromBody] List<string> codigos, YamlConfigWriter writer) =>
        {
            writer.SaveProdutosFixados(codigos);
            return Results.Ok(new { ok = true });
        });
    }

    private static void MapEtiquetaApi(WebApplication app)
    {
        app.MapPost("/api/etiqueta/imprimir", ([FromBody] EtiquetaPayload payload) =>
        {
            try
            {
                ArgoxLabelPrinter.Imprimir(payload.Impressora, payload.Nome, payload.Preco, payload.Codigo);
                return Results.Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });
    }
}

file record ConfigPayload(
    string IpServidor, string IpCliente, string Mascara,
    string Linha1, string Linha2, string Linha3, string Linha4, int Tempo);

file record ParamsPayload(bool IpDinamico);

file record UpdatePayload(string Gateway, string Nome);

file record EtiquetaPayload(string Impressora, string Nome, string Preco, string Codigo);
