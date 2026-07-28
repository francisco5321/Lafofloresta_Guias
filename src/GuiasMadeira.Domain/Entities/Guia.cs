namespace GuiasMadeira.Domain.Entities;

public sealed class Guia
{
    public int Id { get; set; }
    public int? DestinatarioId { get; set; }
    public int? ProprietarioId { get; set; }
    public int? CodigoBarraId { get; set; }
    public int? RolariaId { get; set; }
    public string? Fornecedor { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
}
