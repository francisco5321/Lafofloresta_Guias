namespace GuiasMadeira.Domain.Entities;

public sealed class Destinatario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Nif { get; set; }
    public string? Morada { get; set; }
    public string? Concelho { get; set; }

    public override string ToString() => Nome;
}
