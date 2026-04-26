using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BuscaPreco.Infrastructure.Services
{
    internal static class ArgoxLabelPrinter
    {
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

        // Label: 50mm x 40mm (400 x 320 dots at 203 DPI)
        public static void Imprimir(string nomePrinter, string nome, string preco, string codigoBarras)
        {
            if (nome.Length > 35) nome = nome[..35];

            var sb = new StringBuilder();
            sb.Append("Q320,24\r\n");
            sb.Append("q400\r\n");
            sb.Append("N\r\n");
            sb.Append($"A10,15,0,3,1,1,N,\"{EscapePpla(nome)}\"\r\n");
            sb.Append($"A10,90,0,4,2,2,N,\"R$ {EscapePpla(preco)}\"\r\n");
            if (!string.IsNullOrWhiteSpace(codigoBarras))
                sb.Append($"B10,185,0,1,2,4,70,B,\"{EscapePpla(codigoBarras)}\"\r\n");
            sb.Append("P1\r\n");

            RawPrint(nomePrinter, Encoding.ASCII.GetBytes(sb.ToString()));
        }

        private static string EscapePpla(string s) => s.Replace("\"", "'");

        private static void RawPrint(string printerName, byte[] bytes)
        {
            if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
                throw new InvalidOperationException($"Não foi possível abrir a impressora '{printerName}'.");

            try
            {
                var docInfo = new DOCINFO
                {
                    pDocName = "Etiqueta BuscaPreco",
                    pOutputFile = null,
                    pDataType = "RAW"
                };

                if (!StartDocPrinter(hPrinter, 1, ref docInfo))
                    throw new InvalidOperationException("Falha ao iniciar documento na impressora.");

                try
                {
                    StartPagePrinter(hPrinter);

                    var pBytes = Marshal.AllocHGlobal(bytes.Length);
                    try
                    {
                        Marshal.Copy(bytes, 0, pBytes, bytes.Length);
                        WritePrinter(hPrinter, pBytes, bytes.Length, out _);
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
