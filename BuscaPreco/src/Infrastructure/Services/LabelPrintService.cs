using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BuscaPreco.Application.Interfaces;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Infrastructure.Services;

/// <summary>
/// Converts an EtiquetaLayout to PPLA commands and sends them via Win32 RAW printing.
/// This approach works reliably with Argox OS-214 (and all PPLA-compatible printers)
/// because GDI+/PrintDocument jobs do not control the label-gap sensor or paper feed.
/// </summary>
public class LabelPrintService : ILabelPrintService
{
    private const float DotsPerMm = 203f / 25.4f; // ~7.992 dots/mm at 203 DPI

    private readonly LabelVariableResolver _resolver;

    public LabelPrintService(LabelVariableResolver resolver)
    {
        _resolver = resolver;
    }

    public void Imprimir(string nomePrinter, EtiquetaLayout layout,
        Dictionary<string, string> variaveis, int copias = 1)
    {
        if (copias < 1) copias = 1;

        var ppla = GerarPpla(layout, variaveis, copias);
        RawPrint(nomePrinter, Encoding.ASCII.GetBytes(ppla));
    }

    // ── PPLA generation ───────────────────────────────────────────────────────

    private string GerarPpla(EtiquetaLayout layout, Dictionary<string, string> variaveis, int copias)
    {
        int widthDots  = Math.Max(1, (int)(layout.LarguraMm * DotsPerMm));
        int heightDots = Math.Max(1, (int)(layout.AlturaMm  * DotsPerMm));
        int gapDots    = (int)(3f * DotsPerMm); // 3 mm gap — standard label backing

        var sb = new StringBuilder();
        sb.Append("\r\n");            // flush any partial command in printer buffer
        sb.Append("N\r\n");           // N MUST be first: clears buffer, starts new label
        sb.Append($"q{widthDots}\r\n");
        sb.Append($"Q{heightDots},{gapDots}\r\n");

        foreach (var comp in layout.Componentes.Where(c => c.Visivel))
            AdicionarComponente(sb, comp, variaveis);

        sb.Append($"P{copias}\r\n");
        return sb.ToString();
    }

    private void AdicionarComponente(StringBuilder sb, EtiquetaComponente comp,
        Dictionary<string, string> variaveis)
    {
        int x = Math.Max(0, (int)(comp.XMm * DotsPerMm));
        int y = Math.Max(0, (int)(comp.YMm * DotsPerMm));
        int h = Math.Max(1, (int)(comp.AlturaMm * DotsPerMm));

        switch (comp.Tipo)
        {
            case TipoComponente.TextoFixo:
                AdicionarTexto(sb, comp.Conteudo, comp, x, y);
                break;

            case TipoComponente.Variavel:
                AdicionarTexto(sb, _resolver.Resolver(comp.Conteudo, variaveis), comp, x, y);
                break;

            case TipoComponente.CodigoBarras:
            {
                var cod = _resolver.Resolver(comp.Conteudo, variaveis).Trim();
                if (string.IsNullOrWhiteSpace(cod)) break;

                var tipo = cod.Length switch
                {
                    13 => "7", // EAN-13
                    12 => "6", // UPC-A
                    8  => "4", // EAN-8
                    _  => "9"  // Code 128 B
                };
                sb.Append($"B{x},{y},0,{tipo},2,4,{h},B,\"{Escape(cod)}\"\r\n");
                break;
            }

            case TipoComponente.QrCode:
            {
                // PPLA QR: W<x>,<y>,2,<module>,<ecl>,<len>,<data>
                // ecl: 1=L 2=M 3=Q 4=H; module: 4–8 dots per cell
                var qr = _resolver.Resolver(comp.Conteudo, variaveis).Trim();
                if (string.IsNullOrWhiteSpace(qr)) break;

                int szDots = Math.Max(1, (int)(Math.Min(comp.LarguraMm, comp.AlturaMm) * DotsPerMm));
                int module = Math.Max(4, szDots / 20);          // target ~20 modules wide
                sb.Append($"W{x},{y},2,{module},2,{qr.Length},\"{Escape(qr)}\"\r\n");
                break;
            }
        }
    }

    private static void AdicionarTexto(StringBuilder sb, string texto,
        EtiquetaComponente comp, int x, int y)
    {
        if (string.IsNullOrEmpty(texto)) return;

        var (font, hmult, vmult) = MapearFonte(comp.FonteSize, comp.Negrito);

        // Truncate so text fits within component width
        int maxChars = Math.Max(1, (int)(comp.LarguraMm * DotsPerMm) / (font <= 2 ? 10 : font == 3 ? 14 : font == 4 ? 18 : 32) / hmult);
        if (texto.Length > maxChars) texto = texto[..maxChars];

        sb.Append($"A{x},{y},0,{font},{hmult},{vmult},N,\"{Escape(texto)}\"\r\n");
    }

    /// <summary>
    /// Maps editor font size (pt) to PPLA built-in font + H/V multipliers.
    /// PPLA font dots at 203 DPI: F1=8×12, F2=10×16, F3=14×22, F4=18×28, F5=32×48.
    /// 1 pt ≈ 2.82 dots at 203 DPI.
    /// </summary>
    private static (int font, int hmult, int vmult) MapearFonte(float sizePt, bool bold)
    {
        // Bold adds 1 to hmult to simulate thicker strokes
        int boldAdd = bold ? 1 : 0;

        return sizePt switch
        {
            <= 7f  => (1, 1 + boldAdd, 1),
            <= 9f  => (2, 1 + boldAdd, 1),
            <= 12f => (3, 1 + boldAdd, 1),
            <= 16f => (3, 1 + boldAdd, 2),
            <= 20f => (4, 1 + boldAdd, 1),
            <= 28f => (4, 2 + boldAdd, 2),
            <= 36f => (5, 1 + boldAdd, 1),
            _      => (5, 2 + boldAdd, 2),
        };
    }

    private static string Escape(string s) => s.Replace("\"", "'");

    // ── Win32 RAW printing (same P/Invoke as ArgoxLabelPrinter) ──────────────

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DOCINFO
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
        [MarshalAs(UnmanagedType.LPWStr)] public string pOutputFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
    }

    [DllImport("winspool.Drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);
    [DllImport("winspool.Drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] ref DOCINFO di);
    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);
    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);
    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);
    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);
    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    private static void RawPrint(string printerName, byte[] bytes)
    {
        if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
            throw new InvalidOperationException(
                $"Impressora '{printerName}' não encontrada. (Win32 erro {Marshal.GetLastWin32Error()})");

        try
        {
            var docInfo = new DOCINFO { pDocName = "Etiqueta BuscaPreco", pDataType = "RAW" };
            if (!StartDocPrinter(hPrinter, 1, ref docInfo))
                throw new InvalidOperationException(
                    $"Falha ao iniciar documento. (Win32 erro {Marshal.GetLastWin32Error()})");
            try
            {
                StartPagePrinter(hPrinter);
                var pBytes = Marshal.AllocHGlobal(bytes.Length);
                try
                {
                    Marshal.Copy(bytes, 0, pBytes, bytes.Length);
                    if (!WritePrinter(hPrinter, pBytes, bytes.Length, out int written) || written == 0)
                        throw new InvalidOperationException(
                            $"Falha ao enviar dados. (Win32 erro {Marshal.GetLastWin32Error()})");
                }
                finally { Marshal.FreeHGlobal(pBytes); }
                EndPagePrinter(hPrinter);
            }
            finally { EndDocPrinter(hPrinter); }
        }
        finally { ClosePrinter(hPrinter); }
    }
}
