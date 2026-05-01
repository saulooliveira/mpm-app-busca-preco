using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Infrastructure.Repositories;

public class EtiquetaLayoutRepository : IEtiquetaLayoutRepository
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented    = true,
        Converters       = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    private readonly string _layoutsDir;

    public EtiquetaLayoutRepository()
    {
        _layoutsDir = Path.Combine(AppContext.BaseDirectory, "layouts");
        Directory.CreateDirectory(_layoutsDir);
    }

    public EtiquetaLayout? Carregar(string id = "default")
    {
        var path = CaminhoArquivo(id);
        if (!File.Exists(path)) return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<EtiquetaLayout>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public void Salvar(EtiquetaLayout layout)
    {
        var json = JsonSerializer.Serialize(layout, JsonOpts);
        File.WriteAllText(CaminhoArquivo(layout.Id), json);
    }

    public EtiquetaLayout ObterOuCriarPadrao()
    {
        var layout = Carregar("default");
        if (layout is not null) return layout;

        layout = EtiquetaLayout.CriarPadrao();
        Salvar(layout);
        return layout;
    }

    private string CaminhoArquivo(string id) =>
        Path.Combine(_layoutsDir, $"{id}.json");
}
