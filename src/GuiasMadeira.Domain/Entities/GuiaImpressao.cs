namespace GuiasMadeira.Domain.Entities;

/// <summary>
/// Vista achatada da Guia com todos os dados relacionados, equivalente à query
/// "Relatorio" do Access (sem o produto cartesiano com Vias — as 4 vias são
/// iteradas em código pelo gerador do relatório).
/// </summary>
public sealed class GuiaImpressao
{
    public int IdGuia { get; set; }

    public string? DestinatarioNome { get; set; }
    public string? DestinatarioNif { get; set; }
    public string? DestinatarioMorada { get; set; }

    public string? ProprietarioNome { get; set; }
    public string? ProprietarioDistrito { get; set; }
    public string? ProprietarioConcelho { get; set; }
    public string? ProprietarioFreguesia { get; set; }

    public string? Fornecedor { get; set; }

    public string? RolariaTipo { get; set; }

    public string? CodigoBarraCodigo { get; set; }
    public string? NumeroCertificado { get; set; }
    public string? NumeroUgf { get; set; }
}
