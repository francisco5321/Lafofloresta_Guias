using System.IO;
using GuiasMadeira.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GuiasMadeira.Pdf;

/// <summary>
/// Reproduz o relatório "Nova_Guia" do Access ao milímetro: cada elemento é
/// colocado na posição absoluta (convertida de twips para pontos, /20) lida
/// diretamente da definição do relatório original (ver ANALISE_ACCESS.md
/// secção 5 e docs/access-export/reports/Nova_Guia_clean.txt).
/// </summary>
public sealed class GuiaReportGenerator
{
    private const float TwipsPerPoint = 20f;
    private const float CanvasWidth = 381f;
    private const float CanvasHeight = 515.25f;

    private const string TextoAvisoTransporte = "ESTA GUIA NÃO PODE SER UTILIZADA\nCOMO DOCUMENTO DE TRANSPORTE";

    private const string TextoLegalEsquerda = """
        A madeira fornecida por esta Guia para Entrada de Rolaria não provém de:
        A) Corte de madeira ilegal ou comércio ilegal de produtos florestais ou madeireiros;
        B) Violação de direitos tradicionais e humanos em operações florestais;
        C) Destruição de atributos de alto valor de conservação em operações florestais;
        D) Conversão significativa de florestas em plantações ou uso não florestal;
        E) Introdução de organismos geneticamente modificados em operações florestais;
        F) Violação de qualquer uma das Convenções da OIT (ILO Core Conventions),
        conforme definido na Declaração da OIT sobre Princípios e Direitos Fundamentais no Trabalho, 1998.
        """;

    private const string TextoLegalDireita = """
        E ainda,
        G) Os trabalhadores não estão impedidos de se associarem livremente, escolher
        os seus representantes ou de negociar coletivamente com o seu empregador;
        H) Proíbe o uso de trabalho forçado;
        I) Os trabalhadores que estão abaixo de idade mínima legal – 15 anos – ou da idade
        de frequência obrigatória da escola, escolhendo a mais exigente, não são permitidos;
        J) Aos trabalhadores não são negadas iguais oportunidades de emprego
        e igual tratamento;
        K) As condições de trabalho não comprometem a segurança ou a saúde dos trabalhadores.
        """;

    private sealed record ViaVisualSpec(string CorFundo, string CorTexto, bool MostrarCodigoBarras);

    private static readonly Dictionary<ViaImpressao, ViaVisualSpec> ViaSpecs = new()
    {
        [ViaImpressao.Original] = new("#7B7B7B", Colors.White, true),
        [ViaImpressao.Duplicado] = new("#FFDDFF", Colors.Black, false),
        [ViaImpressao.Triplicado] = new("#D1FFFF", Colors.Black, false),
        [ViaImpressao.Quadriplicado] = new("#E6DFEB", Colors.Black, false)
    };

    static GuiaReportGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(GuiaImpressao guia)
    {
        var document = Document.Create(container =>
        {
            foreach (var via in Enum.GetValues<ViaImpressao>())
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Calibri"));
                    page.Content().AlignCenter().AlignMiddle().Element(content => ComposeVia(content, guia, via));
                });
            }
        });

        return document.GeneratePdf();
    }

    public void SaveToFile(GuiaImpressao guia, string filePath) => File.WriteAllBytes(filePath, Generate(guia));

    private static void ComposeVia(IContainer container, GuiaImpressao guia, ViaImpressao via)
    {
        var spec = ViaSpecs[via];

        container.Width(CanvasWidth).Height(CanvasHeight).Layers(layers =>
        {
            layers.PrimaryLayer().Background(Colors.White);

            // Marca d'água centrada
            layers.Layer().AlignCenter().AlignMiddle().Width(230).Image(GetEmbeddedResourceBytes("fundo.png"));

            // Crachá da via (canto superior direito)
            Positioned(layers, 4980, 0, 2586, 393, e => e.Background(spec.CorFundo));
            Positioned(layers, 5100, 60, 2331, 270, e => e
                .AlignCenter().AlignMiddle()
                .Text(via.NomeVia()).Bold().FontSize(10).FontColor(spec.CorTexto));

            // Logótipo e certificação
            Positioned(layers, 0, 0, 3056, 1596, e => e.Image(GetEmbeddedResourceBytes("logo.png")).FitArea());
            Positioned(layers, 0, 1709, 2580, 375, e => e.Text(" MADEIRA CERTIFICADA FSC 100%").FontSize(7));
            Positioned(layers, 0, 1870, 2865, 270, e => e.Text(" CERTIFICADO: APCER-COC-150294-AP").FontSize(7));
            Positioned(layers, 0, 1650, 2865, 435, e => e.Border(1).BorderColor(Colors.Grey.Lighten2));

            // Cabeçalho: "GUIA PARA ENTRADA DE ROLARIA -" + número
            Positioned(layers, 3285, 510, 3300, 285, e => e
                .Background("#AFABAB").PaddingLeft(6).AlignMiddle()
                .Text("GUIA PARA ENTRADA DE ROLARIA -").Bold().FontSize(9));
            Positioned(layers, 6585, 510, 1005, 285, e => e
                .Border(1).BorderColor(Colors.Grey.Lighten1).AlignCenter().AlignMiddle()
                .Text($"{guia.IdGuia}").Bold().FontSize(9));

            // Aviso "não pode ser utilizada como documento de transporte"
            Positioned(layers, 3279, 960, 4305, 615, e => e
                .Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().AlignMiddle()
                .Text(TextoAvisoTransporte).Bold().FontSize(9).FontColor(Colors.Grey.Darken2));

            // Linha de data
            Positioned(layers, 3964, 1640, 3630, 315, e => e
                .AlignCenter().Text("______ de __________________ de 20_______").FontSize(8));

            // Caixa "Contrato Nº" (tracejada, tal como no original)
            DashedBox(layers, 4692, 2141, 2848, 1147);
            Positioned(layers, 4980, 2550, 2385, 315, e => e.Text("Contrato Nº: 59573.1").Bold().FontSize(11));

            // Caixa do Destinatário
            Positioned(layers, 0, 2085, 7594, 1260, e => e.Border(1).BorderColor(Colors.Grey.Lighten2));
            Positioned(layers, 170, 2154, 1185, 285, e => e.Text("Destinatário:").FontSize(8));
            Positioned(layers, 170, 2381, 4206, 285, e => e
                .Text(guia.DestinatarioNome ?? string.Empty).Bold().Underline().FontSize(10));
            Positioned(layers, 285, 2655, 1470, 285, e => e.Text("Contribuinte n. º:").FontSize(8));
            Positioned(layers, 1587, 2664, 1191, 295, e => e
                .Text(guia.DestinatarioNif ?? string.Empty).Bold().Underline().FontSize(9));
            Positioned(layers, 120, 3000, 735, 225, e => e.Text("Morada:").FontSize(8));
            Positioned(layers, 914, 3000, 3456, 285, e => e
                .Text(guia.DestinatarioMorada ?? string.Empty).Bold().Underline().FontSize(9));

            // Caixa da Rolaria — estende-se até à linha "Entregue pelo Fornecedor" (uma só caixa contínua)
            Positioned(layers, 0, 3405, 7594, 2995, e => e.Border(1).BorderColor(Colors.Grey.Lighten2));
            Positioned(layers, 120, 3472, 720, 270, e => e.Text("Rolaria:").FontSize(8));
            Positioned(layers, 737, 3465, 6786, 390, e => e
                .Text(guia.RolariaTipo ?? string.Empty).Bold().Underline().FontSize(9));

            Positioned(layers, 2154, 3853, 5374, 720, e => e.Border(1).BorderColor(Colors.Grey.Lighten2));
            Positioned(layers, 2265, 4123, 3540, 315, e => e.Text("Quantidade: __________________________").FontSize(9));
            Checkbox(layers, 5788, 4152);
            Positioned(layers, 6014, 4095, 915, 225, e => e.Text("Toneladas").FontSize(8));
            Checkbox(layers, 6921, 4152);
            Positioned(layers, 7148, 4095, 285, 225, e => e.Text("M3").FontSize(8));

            Checkbox(layers, 510, 3972);
            Positioned(layers, 736, 3915, 750, 225, e => e.Text("C/ Casca").FontSize(8));
            Checkbox(layers, 510, 4329);
            Positioned(layers, 736, 4272, 750, 225, e => e.Text("S/ Casca").FontSize(8));

            // N.º Guia Remessa / Matrícula (dentro da mesma caixa da Rolaria)
            Positioned(layers, 113, 4741, 7410, 270, e => e
                .Text("N.º Guia Remessa/Cod AT: _______________________________________________________").FontSize(8));
            Positioned(layers, 113, 5082, 7470, 270, e => e
                .Text("Matrícula do Carro: _______ - _______ - _______     Reboque: ___________________").FontSize(8));

            // Carregamento / Morada (2ª ocorrência, tal como no original)
            Positioned(layers, 113, 5478, 1245, 240, e => e.Text("Carregamento:").FontSize(8));
            Positioned(layers, 1303, 5478, 1251, 285, e => e
                .Text(guia.ProprietarioFreguesia ?? string.Empty).Bold().Underline().FontSize(9));
            Positioned(layers, 2777, 5507, 735, 225, e => e.Text("Morada:").FontSize(8));
            Positioned(layers, 3459, 5499, 3081, 285, e => e
                .Text(guia.DestinatarioMorada ?? string.Empty).Bold().Underline().FontSize(9));

            // Entregue pelo Fornecedor
            Positioned(layers, 113, 6045, 2220, 270, e => e.Text("Entregue pelo Fornecedor:").FontSize(8));
            Positioned(layers, 2154, 6045, 2661, 285, e => e
                .Text(guia.Fornecedor ?? string.Empty).Bold().Underline().FontSize(9));

            // Caixa Origem da Madeira / dados do proprietário
            Positioned(layers, 0, 6470, 7594, 2085, e => e.Border(1).BorderColor(Colors.Grey.Lighten2));
            Positioned(layers, 60, 6648, 2115, 315, e => e.Text("ORIGEM DA MADEIRA:").Bold().FontSize(8));
            Positioned(layers, 2430, 6630, 1575, 315, e => e.Text("Cerna Portugal  /").Bold().FontSize(9));
            Positioned(layers, 3741, 6630, 1701, 315, e => e
                .Text(guia.NumeroUgf ?? string.Empty).Bold().Underline().FontSize(9));

            Positioned(layers, 120, 6930, 2115, 315, e => e.Text("Nome da Propriedade:").FontSize(8));
            Positioned(layers, 1870, 6916, 1701, 315, e => e
                .Text(guia.NumeroCertificado ?? string.Empty).Bold().Underline().FontSize(9));

            Positioned(layers, 120, 7485, 825, 225, e => e.Text("Distrito:").FontSize(8));
            Positioned(layers, 800, 7485, 1071, 225, e => e
                .Text(guia.ProprietarioDistrito ?? string.Empty).Bold().Underline().FontSize(9));
            Positioned(layers, 2218, 7485, 855, 225, e => e.Text("Concelho:").FontSize(8));
            Positioned(layers, 3012, 7485, 1071, 225, e => e
                .Text(guia.ProprietarioConcelho ?? string.Empty).Bold().Underline().FontSize(9));

            Positioned(layers, 120, 7815, 930, 270, e => e.Text("Freguesia:").FontSize(8));
            Positioned(layers, 914, 7815, 951, 270, e => e
                .Text(guia.ProprietarioFreguesia ?? string.Empty).Bold().Underline().FontSize(9));

            Positioned(layers, 120, 8225, 675, 240, e => e.Text("Nome:").FontSize(8));
            Positioned(layers, 793, 8236, 2676, 225, e => e
                .Text(guia.ProprietarioNome ?? string.Empty).Bold().Underline().FontSize(9));

            // Código de barras (só na via Original) ou "N.º Compra" nas cópias
            if (spec.MostrarCodigoBarras && !string.IsNullOrWhiteSpace(guia.CodigoBarraCodigo))
            {
                Positioned(layers, 4478, 7880, 2946, 330, e => e
                    .AlignCenter().Image(Code39BarcodeGenerator.GeneratePng(guia.CodigoBarraCodigo!, 600, 150)).FitArea());
                Positioned(layers, 4478, 8260, 2946, 270, e => e
                    .AlignCenter().Text(guia.CodigoBarraCodigo).Bold().FontSize(9));
            }
            else
            {
                Positioned(layers, 5385, 8160, 1785, 315, e => e.Text("N.º Compra: ________").FontSize(9));
            }

            // Texto legal (duas colunas)
            Positioned(layers, 0, 8670, 3405, 1635, e => e.Text(TextoLegalEsquerda).FontSize(5).FontColor(Colors.Grey.Darken1));
            Positioned(layers, 3495, 8675, 4125, 1245, e => e.Text(TextoLegalDireita).FontSize(5).FontColor(Colors.Grey.Darken1));
        });
    }

    private static void Positioned(LayersDescriptor layers, float left, float top, float width, float height, Action<IContainer> content)
    {
        layers.Layer()
            .OffsetX(left / TwipsPerPoint)
            .OffsetY(top / TwipsPerPoint)
            .Width(width / TwipsPerPoint)
            .Height(height / TwipsPerPoint)
            .Element(content);
    }

    private static void Checkbox(LayersDescriptor layers, float left, float top) =>
        Positioned(layers, left, top, 169, 180, e => e.Border(1).BorderColor(Colors.Grey.Darken1));

    /// <summary>
    /// Caixa com contorno tracejado (QuestPDF não tem um estilo de borda tracejada
    /// direto, por isso desenha-se com 4 linhas usando LineDashPattern).
    /// </summary>
    private static void DashedBox(LayersDescriptor layers, float left, float top, float width, float height)
    {
        var dash = new[] { 3f, 2f };

        Positioned(layers, left, top, width, 20, e => e.LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten1).LineDashPattern(dash));
        Positioned(layers, left, top + height, width, 20, e => e.LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten1).LineDashPattern(dash));
        Positioned(layers, left, top, 20, height, e => e.LineVertical(0.75f).LineColor(Colors.Grey.Lighten1).LineDashPattern(dash));
        Positioned(layers, left + width, top, 20, height, e => e.LineVertical(0.75f).LineColor(Colors.Grey.Lighten1).LineDashPattern(dash));
    }

    private static byte[] GetEmbeddedResourceBytes(string fileName)
    {
        var assembly = typeof(GuiaReportGenerator).Assembly;
        var resourceName = $"GuiasMadeira.Pdf.Resources.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Recurso incorporado não encontrado: {resourceName}");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
