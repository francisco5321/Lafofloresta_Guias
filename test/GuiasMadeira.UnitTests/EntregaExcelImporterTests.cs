using System.IO;
using GuiasMadeira.Desktop.Services;

namespace GuiasMadeira.UnitTests;

public class EntregaExcelImporterTests : IDisposable
{
    private readonly string caminhoFicheiro = Path.Combine(Path.GetTempPath(), $"entregas-{Guid.NewGuid()}.xls");

    public void Dispose() => File.Delete(caminhoFicheiro);

    [Fact]
    public void Ler_SomaColunaQt_ConvertendoFormatoNumericoPortugues()
    {
        Escrever("""
            <html><body>
            <table>
              <tr><td>Data</td><td>Qt</td></tr>
              <tr><td>01/01/2026</td><td>1.234,56</td></tr>
              <tr><td>02/01/2026</td><td>10,44</td></tr>
            </table>
            </body></html>
            """);

        var resumo = EntregaExcelImporter.Ler(caminhoFicheiro);

        Assert.Equal(1245.00m, resumo.TotalToneladas);
        Assert.Equal(2, resumo.LinhasLidas);
    }

    [Fact]
    public void Ler_IgnoraTabelasSemColunaQt()
    {
        Escrever("""
            <html><body>
            <table>
              <tr><td>Data</td><td>Outra coluna</td></tr>
              <tr><td>01/01/2026</td><td>1.234,56</td></tr>
            </table>
            </body></html>
            """);

        var resumo = EntregaExcelImporter.Ler(caminhoFicheiro);

        Assert.Equal(0m, resumo.TotalToneladas);
        Assert.Equal(0, resumo.LinhasLidas);
    }

    [Fact]
    public void Ler_IgnoraCelulasNaoNumericasNaColunaQt()
    {
        Escrever("""
            <html><body>
            <table>
              <tr><td>Qt</td></tr>
              <tr><td>abc</td></tr>
              <tr><td>5,00</td></tr>
            </table>
            </body></html>
            """);

        var resumo = EntregaExcelImporter.Ler(caminhoFicheiro);

        Assert.Equal(5.00m, resumo.TotalToneladas);
        Assert.Equal(1, resumo.LinhasLidas);
    }

    [Fact]
    public void Ler_SomaVariasTabelasNoMesmoFicheiro()
    {
        Escrever("""
            <html><body>
            <table><tr><td>Qt</td></tr><tr><td>1,00</td></tr></table>
            <p>separador entre tabelas</p>
            <table><tr><td>Qt</td></tr><tr><td>2,50</td></tr></table>
            </body></html>
            """);

        var resumo = EntregaExcelImporter.Ler(caminhoFicheiro);

        Assert.Equal(3.50m, resumo.TotalToneladas);
        Assert.Equal(2, resumo.LinhasLidas);
    }

    [Fact]
    public void Ler_DecodificaEntidadesHtmlNasCelulas()
    {
        Escrever("""
            <html><body>
            <table><tr><td>Qt</td></tr><tr><td>1&#44;50</td></tr></table>
            </body></html>
            """);

        var resumo = EntregaExcelImporter.Ler(caminhoFicheiro);

        Assert.Equal(1.50m, resumo.TotalToneladas);
    }

    private void Escrever(string conteudo) => File.WriteAllText(caminhoFicheiro, conteudo);
}
