using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace BuscaPreco.Infrastructure.Services;

/// <summary>
/// Generates barcode and QR code bitmaps using ZXing.Net.
/// </summary>
public class BarcodeAndQrRenderer
{
    private static readonly MultiFormatWriter Writer = new();

    public Bitmap GerarQrCode(string conteudo, int larguraPx, int alturaPx)
    {
        if (string.IsNullOrWhiteSpace(conteudo))
            return BitmapVazio(larguraPx, alturaPx);

        try
        {
            var hints = new Dictionary<EncodeHintType, object>
            {
                [EncodeHintType.MARGIN]           = 1,
                [EncodeHintType.ERROR_CORRECTION] = ZXing.QrCode.Internal.ErrorCorrectionLevel.M
            };
            var matrix = Writer.encode(conteudo, BarcodeFormat.QR_CODE, larguraPx, alturaPx, hints);
            return BitMatrixParaBitmap(matrix);
        }
        catch
        {
            return BitmapVazio(larguraPx, alturaPx);
        }
    }

    public Bitmap GerarCodigoBarras(string conteudo, int larguraPx, int alturaPx)
    {
        if (string.IsNullOrWhiteSpace(conteudo))
            return BitmapVazio(larguraPx, alturaPx);

        try
        {
            var formato = DetectarFormato(conteudo.Trim());
            var hints = new Dictionary<EncodeHintType, object>
            {
                [EncodeHintType.MARGIN] = 0
            };
            var matrix = Writer.encode(conteudo.Trim(), formato, larguraPx, alturaPx, hints);
            return BitMatrixParaBitmap(matrix);
        }
        catch
        {
            return BitmapVazio(larguraPx, alturaPx);
        }
    }

    private static BarcodeFormat DetectarFormato(string data)
    {
        if (data.Length == 13 && data.All(char.IsDigit)) return BarcodeFormat.EAN_13;
        if (data.Length == 12 && data.All(char.IsDigit)) return BarcodeFormat.UPC_A;
        if (data.Length == 8  && data.All(char.IsDigit)) return BarcodeFormat.EAN_8;
        return BarcodeFormat.CODE_128;
    }

    private static Bitmap BitMatrixParaBitmap(BitMatrix matrix)
    {
        int w = matrix.Width;
        int h = matrix.Height;
        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        var bd  = bmp.LockBits(new Rectangle(0, 0, w, h),
                                ImageLockMode.WriteOnly,
                                PixelFormat.Format32bppArgb);
        try
        {
            int stride = Math.Abs(bd.Stride);
            var bytes  = new byte[stride * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * stride + x * 4;
                    byte v  = matrix[x, y] ? (byte)0 : (byte)255;
                    bytes[idx + 0] = v;   // B
                    bytes[idx + 1] = v;   // G
                    bytes[idx + 2] = v;   // R
                    bytes[idx + 3] = 255; // A
                }
            }

            Marshal.Copy(bytes, 0, bd.Scan0, bytes.Length);
        }
        finally
        {
            bmp.UnlockBits(bd);
        }

        return bmp;
    }

    private static Bitmap BitmapVazio(int w, int h)
    {
        var bmp = new Bitmap(Math.Max(1, w), Math.Max(1, h));
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        return bmp;
    }
}
