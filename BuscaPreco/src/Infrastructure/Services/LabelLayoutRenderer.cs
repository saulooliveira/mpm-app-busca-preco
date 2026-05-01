using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Infrastructure.Services;

/// <summary>
/// Renders an EtiquetaLayout to a Bitmap using GDI+.
/// Works for both screen preview (any scale) and print output (203 DPI).
/// </summary>
public class LabelLayoutRenderer
{
    // Argox OS-214 and most thermal printers run at 203 DPI
    public const float PrintDpi   = 203f;
    public const float MmPerInch  = 25.4f;
    public const float DotsPerMm  = PrintDpi / MmPerInch; // ~7.992

    private readonly LabelVariableResolver  _resolver;
    private readonly BarcodeAndQrRenderer   _barcodeRenderer;

    public LabelLayoutRenderer(LabelVariableResolver resolver, BarcodeAndQrRenderer barcodeRenderer)
    {
        _resolver        = resolver;
        _barcodeRenderer = barcodeRenderer;
    }

    /// <summary>Renders at 203 DPI for print quality.</summary>
    public Bitmap RenderizarParaImpressao(EtiquetaLayout layout, Dictionary<string, string> variaveis)
        => Renderizar(layout, variaveis, DotsPerMm);

    /// <summary>
    /// Renders at a custom scale (<paramref name="pxPerMm"/> px per mm).
    /// Use ~4 for editor preview, DotsPerMm (~8) for printing.
    /// </summary>
    public Bitmap Renderizar(EtiquetaLayout layout, Dictionary<string, string> variaveis, float pxPerMm)
    {
        int w = Math.Max(1, (int)(layout.LarguraMm * pxPerMm));
        int h = Math.Max(1, (int)(layout.AlturaMm  * pxPerMm));

        var bmp = new Bitmap(w, h);
        bmp.SetResolution(PrintDpi, PrintDpi);

        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode      = SmoothingMode.AntiAlias;
        g.TextRenderingHint  = TextRenderingHint.AntiAlias;
        g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
        g.Clear(Color.White);

        foreach (var comp in layout.Componentes.Where(c => c.Visivel))
            RenderizarComponente(g, comp, variaveis, pxPerMm);

        return bmp;
    }

    private void RenderizarComponente(Graphics g, EtiquetaComponente comp,
        Dictionary<string, string> variaveis, float pxPerMm)
    {
        var rect = new RectangleF(
            comp.XMm      * pxPerMm,
            comp.YMm      * pxPerMm,
            comp.LarguraMm * pxPerMm,
            comp.AlturaMm  * pxPerMm);

        if (rect.Width <= 0 || rect.Height <= 0) return;

        switch (comp.Tipo)
        {
            case TipoComponente.TextoFixo:
                DesenharTexto(g, comp.Conteudo, comp, rect, pxPerMm);
                break;

            case TipoComponente.Variavel:
                DesenharTexto(g, _resolver.Resolver(comp.Conteudo, variaveis), comp, rect, pxPerMm);
                break;

            case TipoComponente.CodigoBarras:
                var cod = _resolver.Resolver(comp.Conteudo, variaveis);
                using (var bmp = _barcodeRenderer.GerarCodigoBarras(cod, (int)rect.Width, (int)rect.Height))
                    g.DrawImage(bmp, rect);
                break;

            case TipoComponente.QrCode:
                var qr  = _resolver.Resolver(comp.Conteudo, variaveis);
                int sz  = (int)Math.Min(rect.Width, rect.Height);
                using (var bmp = _barcodeRenderer.GerarQrCode(qr, sz, sz))
                    g.DrawImage(bmp, new RectangleF(rect.X, rect.Y, sz, sz));
                break;
        }
    }

    private static void DesenharTexto(Graphics g, string texto, EtiquetaComponente comp,
        RectangleF rect, float pxPerMm)
    {
        if (string.IsNullOrEmpty(texto)) return;

        var style = FontStyle.Regular;
        if (comp.Negrito)    style |= FontStyle.Bold;
        if (comp.Italico)    style |= FontStyle.Italic;
        if (comp.Sublinhado) style |= FontStyle.Underline;

        // comp.FonteSize is in pt; scale to current pxPerMm resolution
        float scaledPt = comp.FonteSize * pxPerMm / DotsPerMm;

        try
        {
            using var font  = new Font(comp.FonteFamily, Math.Max(1f, scaledPt), style, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(ParseCor(comp.Cor));
            using var sf    = new StringFormat
            {
                Alignment     = comp.Alinhamento switch
                {
                    AlinhamentoTexto.Centro  => StringAlignment.Center,
                    AlinhamentoTexto.Direita => StringAlignment.Far,
                    _                        => StringAlignment.Near
                },
                LineAlignment = StringAlignment.Near,
                Trimming      = StringTrimming.EllipsisCharacter,
                FormatFlags   = StringFormatFlags.LineLimit
            };
            g.DrawString(texto, font, brush, rect, sf);
        }
        catch { /* skip unrecognised font family */ }
    }

    private static Color ParseCor(string hex)
    {
        try   { return ColorTranslator.FromHtml(hex); }
        catch { return Color.Black; }
    }
}
