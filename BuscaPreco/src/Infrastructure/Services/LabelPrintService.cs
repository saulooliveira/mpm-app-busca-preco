using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Infrastructure.Services;

public class LabelPrintService : ILabelPrintService
{
    private readonly LabelLayoutRenderer _renderer;

    public LabelPrintService(LabelLayoutRenderer renderer)
    {
        _renderer = renderer;
    }

    public void Imprimir(string nomePrinter, EtiquetaLayout layout,
        Dictionary<string, string> variaveis, int copias = 1)
    {
        using var bmp = _renderer.RenderizarParaImpressao(layout, variaveis);

        using var doc = new PrintDocument();
        doc.PrinterSettings.PrinterName = nomePrinter;
        doc.PrinterSettings.Copies     = (short)Math.Max(1, Math.Min(copias, 999));

        // Label size in hundredths of an inch (PrinterUnit.HundredthsOfAnInch)
        int widthHundredths  = (int)(layout.LarguraMm / LabelLayoutRenderer.MmPerInch * 100);
        int heightHundredths = (int)(layout.AlturaMm  / LabelLayoutRenderer.MmPerInch * 100);

        doc.DefaultPageSettings.PaperSize = new PaperSize("Etiqueta", widthHundredths, heightHundredths);
        doc.DefaultPageSettings.Margins   = new Margins(0, 0, 0, 0);
        doc.DefaultPageSettings.Landscape = layout.LarguraMm > layout.AlturaMm;

        // Capture for closure
        var image = bmp;
        doc.PrintPage += (_, e) =>
        {
            e.Graphics!.DrawImage(image, e.PageBounds);
            e.HasMorePages = false;
        };

        doc.Print();
    }
}
