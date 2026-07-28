using System.Diagnostics;
using System.IO;

namespace GuiasMadeira.Desktop.Services;

/// <summary>
/// Gera o PDF de uma guia (4 vias) e abre-o no visualizador padrão do Windows,
/// tal como o botão "Imprimir / Pré-visualizar" fazia no Access.
/// </summary>
public static class GuiaPrintService
{
    public static async Task<bool> PreviewAsync(int idGuia)
    {
        var guiaImpressao = await AppServices.Guias.GetGuiaImpressaoAsync(idGuia);
        if (guiaImpressao is null)
        {
            return false;
        }

        var outputPath = Path.Combine(Path.GetTempPath(), $"guia-{idGuia}.pdf");
        AppServices.ReportGenerator.SaveToFile(guiaImpressao, outputPath);

        Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
        return true;
    }
}
