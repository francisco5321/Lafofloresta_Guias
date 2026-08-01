using System.IO;
using ClosedXML.Excel;
using GuiasMadeira.Desktop.Services;

namespace GuiasMadeira.UnitTests;

public class CodigoBarraExcelImporterTests : IDisposable
{
    private readonly string caminhoFicheiro = Path.Combine(Path.GetTempPath(), $"vinhetas-{Guid.NewGuid()}.xlsx");

    public void Dispose() => File.Delete(caminhoFicheiro);

    [Fact]
    public void Ler_ReconheceColunasPeloNomeDoCabecalho_IndependenteDaOrdem()
    {
        CriarFicheiro(
            cabecalho: ["Número da UGF", "Código de Barras", "Número do Certificado"],
            linhas:
            [
                ["UGFPT00001", "12345", "CERT-001"],
                ["UGFPT00002", "12346", "CERT-002"],
            ]);

        var linhasLidas = CodigoBarraExcelImporter.Ler(caminhoFicheiro);

        Assert.Equal(2, linhasLidas.Count);
        Assert.Equal("12345", linhasLidas[0].Codigo);
        Assert.Equal("CERT-001", linhasLidas[0].NumeroCertificado);
        Assert.Equal("UGFPT00001", linhasLidas[0].NumeroUgf);
    }

    [Fact]
    public void Ler_IgnoraAcentuacaoECapitalizacaoNoCabecalho()
    {
        CriarFicheiro(
            cabecalho: ["CÓDIGO", "Certificado", "UGF"],
            linhas: [["999", "CERT-999", "UGFPT00099"]]);

        var linhasLidas = CodigoBarraExcelImporter.Ler(caminhoFicheiro);

        Assert.Single(linhasLidas);
        Assert.Equal("999", linhasLidas[0].Codigo);
    }

    [Fact]
    public void Ler_IgnoraLinhasComCodigoVazio()
    {
        CriarFicheiro(
            cabecalho: ["Código de Barras", "Número do Certificado", "Número da UGF"],
            linhas:
            [
                ["111", "CERT-A", "UGFPT00001"],
                ["   ", "CERT-B", "UGFPT00002"],
                ["", "CERT-C", "UGFPT00003"],
            ]);

        var linhasLidas = CodigoBarraExcelImporter.Ler(caminhoFicheiro);

        Assert.Single(linhasLidas);
        Assert.Equal("111", linhasLidas[0].Codigo);
    }

    [Fact]
    public void Ler_DevolveNumeroCertificadoENumeroUgfNulos_QuandoCelulasEstaoEmBranco()
    {
        CriarFicheiro(
            cabecalho: ["Código de Barras", "Número do Certificado", "Número da UGF"],
            linhas: [["222", "", "  "]]);

        var linhasLidas = CodigoBarraExcelImporter.Ler(caminhoFicheiro);

        Assert.Null(linhasLidas[0].NumeroCertificado);
        Assert.Null(linhasLidas[0].NumeroUgf);
    }

    [Fact]
    public void Ler_UsaOrdemPosicional_QuandoNaoHaCabecalhosReconhecidos()
    {
        CriarFicheiro(
            cabecalho: ["Coluna A", "Coluna B", "Coluna C"],
            linhas: [["333", "CERT-X", "UGFPT00003"]]);

        var linhasLidas = CodigoBarraExcelImporter.Ler(caminhoFicheiro);

        Assert.Equal("333", linhasLidas[0].Codigo);
        Assert.Equal("CERT-X", linhasLidas[0].NumeroCertificado);
    }

    [Fact]
    public void Ler_DevolveListaVazia_QuandoSoHaCabecalhoSemLinhas()
    {
        CriarFicheiro(cabecalho: ["Código de Barras", "Número do Certificado", "Número da UGF"], linhas: []);

        var linhasLidas = CodigoBarraExcelImporter.Ler(caminhoFicheiro);

        Assert.Empty(linhasLidas);
    }

    private void CriarFicheiro(string[] cabecalho, IReadOnlyList<string[]> linhas)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Vinhetas");

        for (var coluna = 0; coluna < cabecalho.Length; coluna++)
        {
            worksheet.Cell(1, coluna + 1).Value = cabecalho[coluna];
        }

        for (var linha = 0; linha < linhas.Count; linha++)
        {
            for (var coluna = 0; coluna < linhas[linha].Length; coluna++)
            {
                worksheet.Cell(linha + 2, coluna + 1).Value = linhas[linha][coluna];
            }
        }

        workbook.SaveAs(caminhoFicheiro);
    }
}
