using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BuscaPreco.Infrastructure.Services
{
    internal static class ArgoxLabelPrinter
    {
        // ── Structs ──────────────────────────────────────────────────────────────

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DOCINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct PRINTER_INFO_4
        {
            public string pPrinterName;
            public string pServerName;
            public uint Attributes;
        }

        // ── P/Invoke ─────────────────────────────────────────────────────────────

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

        [DllImport("winspool.Drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool EnumPrinters(
            uint Flags, string? Name, uint Level,
            IntPtr pPrinterEnum, uint cbBuf,
            out uint pcbNeeded, out uint pcReturned);

        // ── Público ───────────────────────────────────────────────────────────────

        // Label: 50mm x 40mm (400 x 320 dots at 203 DPI)
        public static void Imprimir(string nomePrinter, string nome, string preco, string codigoBarras, int copias = 1)
        {
            if (nome.Length > 35) nome = nome[..35];
            if (copias < 1) copias = 1;

            var sb = new StringBuilder();
            sb.Append("Q320,24\r\n");
            sb.Append("q400\r\n");
            sb.Append("N\r\n");
            sb.Append($"A10,15,0,3,1,1,N,\"{EscapePpla(nome)}\"\r\n");
            sb.Append($"A10,90,0,4,2,2,N,\"R$ {EscapePpla(preco)}\"\r\n");
            if (!string.IsNullOrWhiteSpace(codigoBarras))
                sb.Append($"B10,185,0,1,2,4,70,B,\"{EscapePpla(codigoBarras)}\"\r\n");
            sb.Append($"P{copias}\r\n");

            RawPrint(nomePrinter, Encoding.ASCII.GetBytes(sb.ToString()));
        }

        /// <summary>Retorna os nomes de todas as impressoras instaladas no Windows.</summary>
        public static IReadOnlyList<string> ListarImpressoras()
        {
            const uint PRINTER_ENUM_LOCAL       = 0x00000002;
            const uint PRINTER_ENUM_CONNECTIONS = 0x00000004;
            const uint level = 4;

            var nomes = new List<string>();

            try
            {
                EnumPrinters(PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS,
                    null, level, IntPtr.Zero, 0, out uint cbNeeded, out _);

                if (cbNeeded == 0) return nomes;

                var pMem = Marshal.AllocHGlobal((int)cbNeeded);
                try
                {
                    if (!EnumPrinters(PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS,
                            null, level, pMem, cbNeeded, out _, out uint count))
                        return nomes;

                    int structSize = Marshal.SizeOf<PRINTER_INFO_4>();
                    for (int i = 0; i < count; i++)
                    {
                        var info = Marshal.PtrToStructure<PRINTER_INFO_4>(pMem + i * structSize);
                        if (!string.IsNullOrWhiteSpace(info.pPrinterName))
                            nomes.Add(info.pPrinterName);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pMem);
                }
            }
            catch { /* não disponível fora do Windows */ }

            return nomes;
        }

        // ── Privado ───────────────────────────────────────────────────────────────

        private static string EscapePpla(string s) => s.Replace("\"", "'");

        private static void RawPrint(string printerName, byte[] bytes)
        {
            if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
            {
                var win32Err = Marshal.GetLastWin32Error();
                throw new InvalidOperationException(
                    $"Impressora '{printerName}' não encontrada ou inacessível. " +
                    $"(Win32 erro {win32Err}) — Verifique se o nome está exatamente igual ao mostrado em " +
                    "Painel de Controle → Dispositivos e Impressoras.");
            }

            try
            {
                var docInfo = new DOCINFO
                {
                    pDocName = "Etiqueta BuscaPreco",
                    pOutputFile = null,
                    pDataType = "RAW"
                };

                if (!StartDocPrinter(hPrinter, 1, ref docInfo))
                {
                    var win32Err = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException(
                        $"Falha ao iniciar documento na impressora. (Win32 erro {win32Err})");
                }

                try
                {
                    StartPagePrinter(hPrinter);

                    var pBytes = Marshal.AllocHGlobal(bytes.Length);
                    try
                    {
                        Marshal.Copy(bytes, 0, pBytes, bytes.Length);
                        if (!WritePrinter(hPrinter, pBytes, bytes.Length, out int written) || written == 0)
                        {
                            var win32Err = Marshal.GetLastWin32Error();
                            throw new InvalidOperationException(
                                $"Falha ao enviar dados para a impressora. (Win32 erro {win32Err})");
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pBytes);
                    }

                    EndPagePrinter(hPrinter);
                }
                finally
                {
                    EndDocPrinter(hPrinter);
                }
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}
