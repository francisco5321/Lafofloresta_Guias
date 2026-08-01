using System.IO;
using GuiasMadeira.Pdf;

namespace GuiasMadeira.UnitTests;

public class Code39BarcodeGeneratorTests
{
    [Fact]
    public void GeneratePng_DevolveImagemPngValida()
    {
        var png = Code39BarcodeGenerator.GeneratePng("ABC12345", width: 300, height: 60);

        Assert.NotEmpty(png);
        // Assinatura PNG: 89 50 4E 47 0D 0A 1A 0A
        Assert.Equal(0x89, png[0]);
        Assert.Equal((byte)'P', png[1]);
        Assert.Equal((byte)'N', png[2]);
        Assert.Equal((byte)'G', png[3]);
    }

    [Fact]
    public void GeneratePng_ProduzImagemComAlturaSemelhanteAPedida()
    {
        var png = Code39BarcodeGenerator.GeneratePng("12345", width: 200, height: 80);

        using var stream = new MemoryStream(png);
        using var imagem = System.Drawing.Image.FromStream(stream);

        Assert.Equal(80, imagem.Height);
    }

    [Fact]
    public void GeneratePng_AceitaDigitosELetrasMaiusculas()
    {
        var png = Code39BarcodeGenerator.GeneratePng("PT00012345", width: 250, height: 50);

        Assert.NotEmpty(png);
    }
}
