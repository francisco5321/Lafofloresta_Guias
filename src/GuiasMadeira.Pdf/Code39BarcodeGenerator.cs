using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace GuiasMadeira.Pdf;

/// <summary>
/// Gera a imagem do código de barras Code-39 usada na via Original.
/// O relatório Access original desenhava o mesmo padrão através de uma fonte
/// "Libre Barcode 39"; gerar a imagem diretamente evita ter de instalar
/// essa fonte em cada posto de trabalho.
/// </summary>
public static class Code39BarcodeGenerator
{
    public static byte[] GeneratePng(string codigo, int width, int height)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.CODE_39,
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 0,
                PureBarcode = true
            }
        };

        using var bitmap = writer.Write(codigo);
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
