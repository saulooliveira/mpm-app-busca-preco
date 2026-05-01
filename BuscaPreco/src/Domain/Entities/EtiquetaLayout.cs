using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BuscaPreco.Domain.Entities;

public class EtiquetaLayout
{
    public string Id { get; set; } = "default";
    public string Nome { get; set; } = "Padrão";
    public float LarguraMm { get; set; } = 110f;
    public float AlturaMm { get; set; } = 30f;
    public List<EtiquetaComponente> Componentes { get; set; } = new();

    // Mirrors the current ArgoxLabelPrinter hardcoded layout
    public static EtiquetaLayout CriarPadrao() => new()
    {
        Id = "default",
        Nome = "Padrão (110mm × 30mm)",
        LarguraMm = 110f,
        AlturaMm = 30f,
        Componentes = new List<EtiquetaComponente>
        {
            new()
            {
                Id = "desc1",
                Tipo = TipoComponente.Variavel,
                Conteudo = "{DescricaoProduto}",
                XMm = 2f, YMm = 2f, LarguraMm = 65f, AlturaMm = 8f,
                FonteSize = 10f, Negrito = true, Visivel = true
            },
            new()
            {
                Id = "preco",
                Tipo = TipoComponente.Variavel,
                Conteudo = "R$ {PrecoVenda}",
                XMm = 2f, YMm = 13f, LarguraMm = 65f, AlturaMm = 14f,
                FonteSize = 20f, Negrito = true, Visivel = true
            },
            new()
            {
                Id = "barcode",
                Tipo = TipoComponente.CodigoBarras,
                Conteudo = "{CodigoBarras}",
                XMm = 70f, YMm = 2f, LarguraMm = 38f, AlturaMm = 26f,
                Visivel = true
            }
        }
    };
}

public class EtiquetaComponente
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TipoComponente Tipo { get; set; } = TipoComponente.TextoFixo;

    public string Conteudo { get; set; } = string.Empty;
    public float XMm { get; set; }
    public float YMm { get; set; }
    public float LarguraMm { get; set; } = 30f;
    public float AlturaMm { get; set; } = 8f;
    public float FonteSize { get; set; } = 10f;
    public string FonteFamily { get; set; } = "Arial";
    public bool Negrito { get; set; }
    public bool Italico { get; set; }
    public bool Sublinhado { get; set; }
    public string Cor { get; set; } = "#000000";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AlinhamentoTexto Alinhamento { get; set; } = AlinhamentoTexto.Esquerda;

    public bool Visivel { get; set; } = true;

    public EtiquetaComponente Clone() =>
        JsonSerializer.Deserialize<EtiquetaComponente>(
            JsonSerializer.Serialize(this, _opts), _opts)!;

    private static readonly JsonSerializerOptions _opts = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

public enum TipoComponente { TextoFixo, Variavel, CodigoBarras, QrCode }
public enum AlinhamentoTexto { Esquerda, Centro, Direita }
