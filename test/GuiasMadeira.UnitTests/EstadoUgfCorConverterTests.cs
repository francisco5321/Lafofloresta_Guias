using System.Windows.Media;
using GuiasMadeira.Desktop.Converters;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.UnitTests;

public class EstadoUgfCorConverterTests
{
    private readonly EstadoUgfCorConverter converter = new();

    [Theory]
    [InlineData(EstadoUgf.Verde)]
    [InlineData(EstadoUgf.Amarelo)]
    [InlineData(EstadoUgf.Vermelho)]
    [InlineData(EstadoUgf.Bloqueado)]
    public void Convert_DevolveUmaCorSolidaDiferenteDeTransparente_ParaCadaEstado(EstadoUgf estado)
    {
        var resultado = converter.Convert(estado, typeof(Brush), null, System.Globalization.CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(resultado);
        Assert.NotEqual(Colors.Transparent, brush.Color);
    }

    [Fact]
    public void Convert_DevolveTransparente_QuandoValorNaoEhEstadoUgf()
    {
        var resultado = converter.Convert("valor-invalido", typeof(Brush), null, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(Brushes.Transparent, resultado);
    }

    [Fact]
    public void Convert_DevolveCoresDiferentes_ParaEstadosDiferentes()
    {
        var verde = (SolidColorBrush)converter.Convert(EstadoUgf.Verde, typeof(Brush), null, System.Globalization.CultureInfo.InvariantCulture)!;
        var vermelho = (SolidColorBrush)converter.Convert(EstadoUgf.Vermelho, typeof(Brush), null, System.Globalization.CultureInfo.InvariantCulture)!;

        Assert.NotEqual(verde.Color, vermelho.Color);
    }

    [Fact]
    public void ConvertBack_NaoEhSuportado()
    {
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(null, typeof(EstadoUgf), null, System.Globalization.CultureInfo.InvariantCulture));
    }
}
