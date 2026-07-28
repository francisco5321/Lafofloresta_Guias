using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace GuiasMadeira.Desktop.Services;

public sealed record CodigoBarraImportRow(int Linha, string Codigo, string? NumeroCertificado, string? NumeroUgf);

/// <summary>
/// Lê o Excel de "Vinhetas" (Código de Barras / Número do Certificado / Número da UGF)
/// exportado para importação em massa na tabela codigos_barras.
/// </summary>
public static class CodigoBarraExcelImporter
{
    public static IReadOnlyList<CodigoBarraImportRow> Ler(string caminhoFicheiro)
    {
        using var workbook = new XLWorkbook(caminhoFicheiro);
        var worksheet = workbook.Worksheets.First();

        var ultimaLinha = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var ultimaColuna = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (ultimaLinha < 2 || ultimaColuna < 1)
        {
            return Array.Empty<CodigoBarraImportRow>();
        }

        var colunaCodigo = EncontrarColuna(worksheet, ultimaColuna, "código de barras", "codigo de barras", "codigo", "código") ?? 1;
        var colunaCertificado = EncontrarColuna(worksheet, ultimaColuna, "número do certificado", "numero do certificado", "certificado") ?? 2;
        var colunaUgf = EncontrarColuna(worksheet, ultimaColuna, "número da ugf", "numero da ugf", "ugf") ?? 3;

        var linhas = new List<CodigoBarraImportRow>();
        for (var linha = 2; linha <= ultimaLinha; linha++)
        {
            var codigo = worksheet.Cell(linha, colunaCodigo).GetString().Trim();
            if (string.IsNullOrWhiteSpace(codigo))
            {
                continue;
            }

            var certificado = worksheet.Cell(linha, colunaCertificado).GetString().Trim();
            var ugf = worksheet.Cell(linha, colunaUgf).GetString().Trim();

            linhas.Add(new CodigoBarraImportRow(
                linha,
                codigo,
                string.IsNullOrWhiteSpace(certificado) ? null : certificado,
                string.IsNullOrWhiteSpace(ugf) ? null : ugf));
        }

        return linhas;
    }

    private static int? EncontrarColuna(IXLWorksheet worksheet, int ultimaColuna, params string[] nomesPossiveis)
    {
        for (var coluna = 1; coluna <= ultimaColuna; coluna++)
        {
            var texto = Normalizar(worksheet.Cell(1, coluna).GetString());
            if (nomesPossiveis.Any(nome => Normalizar(nome) == texto))
            {
                return coluna;
            }
        }

        return null;
    }

    private static string Normalizar(string texto)
    {
        var semAcentos = string.Concat(texto.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
        return semAcentos.Trim().ToLowerInvariant();
    }
}
