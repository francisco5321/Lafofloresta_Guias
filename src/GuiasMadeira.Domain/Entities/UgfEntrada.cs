namespace GuiasMadeira.Domain.Entities;

/// <summary>
/// Registo de uma entrada de madeira na fábrica (importada do ficheiro de entregas) que consome
/// parte do limite de toneladas certificado de um UGF.
/// </summary>
public sealed class UgfEntrada
{
    public int Id { get; set; }
    public int UgfId { get; set; }
    public decimal Toneladas { get; set; }
    public string? Origem { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
}
