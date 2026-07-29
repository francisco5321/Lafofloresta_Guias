namespace GuiasMadeira.Domain.Entities;

/// <summary>
/// Linha de listagem de guias: junta os ids (para reabrir os combos em edição) com os nomes já
/// resolvidos (para mostrar na grid), evitando uma consulta extra por linha.
/// </summary>
public sealed class GuiaResumo
{
    public int Id { get; set; }

    public int? DestinatarioId { get; set; }
    public string? DestinatarioNome { get; set; }
    public string? DestinatarioNif { get; set; }

    public int? ProprietarioId { get; set; }
    public string? ProprietarioNome { get; set; }

    public int? CodigoBarraId { get; set; }
    public string? CodigoBarraCodigo { get; set; }

    public int? RolariaId { get; set; }
    public string? RolariaTipo { get; set; }

    public string? Fornecedor { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
}
