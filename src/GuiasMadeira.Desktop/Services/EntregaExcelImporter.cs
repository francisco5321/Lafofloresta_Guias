using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace GuiasMadeira.Desktop.Services;

public sealed record EntregaImportResumo(decimal TotalToneladas, int LinhasLidas);

/// <summary>
/// Lê o ficheiro de entregas da fábrica (ex. entregas.xls) para somar as toneladas recebidas.
/// Apesar da extensão .xls, este ficheiro é na realidade uma tabela HTML (exportação do sistema
/// "BMS" do cliente), por isso não pode ser aberto pelo ClosedXML — é lido como texto e as
/// tabelas/colunas são localizadas pelo nome do cabeçalho, tal como o CodigoBarraExcelImporter faz
/// para o Excel de vinhetas.
/// </summary>
public static class EntregaExcelImporter
{
    private static readonly Regex TabelaRegex = new("<table\\b[^>]*>(.*?)</table>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex LinhaRegex = new("<tr\\b[^>]*>(.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex CelulaRegex = new("<td\\b[^>]*>(.*?)</td>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Singleline);

    public static EntregaImportResumo Ler(string caminhoFicheiro)
    {
        var conteudo = File.ReadAllText(caminhoFicheiro);

        var total = 0m;
        var linhasLidas = 0;

        foreach (Match tabela in TabelaRegex.Matches(conteudo))
        {
            var linhas = LinhaRegex.Matches(tabela.Groups[1].Value);
            if (linhas.Count < 2)
            {
                continue;
            }

            var cabecalho = ExtrairCelulas(linhas[0].Groups[1].Value);
            var colunaQt = cabecalho.FindIndex(texto => NormalizarCabecalho(texto) == "qt");
            if (colunaQt < 0)
            {
                continue;
            }

            for (var i = 1; i < linhas.Count; i++)
            {
                var celulas = ExtrairCelulas(linhas[i].Groups[1].Value);
                if (colunaQt >= celulas.Count)
                {
                    continue;
                }

                if (TentarConverterToneladas(celulas[colunaQt], out var toneladas))
                {
                    total += toneladas;
                    linhasLidas++;
                }
            }
        }

        return new EntregaImportResumo(total, linhasLidas);
    }

    private static List<string> ExtrairCelulas(string linhaHtml)
    {
        var celulas = new List<string>();
        foreach (Match celula in CelulaRegex.Matches(linhaHtml))
        {
            var texto = TagRegex.Replace(celula.Groups[1].Value, string.Empty);
            celulas.Add(WebUtility.HtmlDecode(texto).Trim());
        }

        return celulas;
    }

    private static string NormalizarCabecalho(string texto) => texto.Trim().TrimEnd('.').ToLowerInvariant();

    private static bool TentarConverterToneladas(string texto, out decimal toneladas)
    {
        toneladas = 0m;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var normalizado = texto.Replace(".", string.Empty).Replace(",", ".");
        return decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out toneladas);
    }
}
