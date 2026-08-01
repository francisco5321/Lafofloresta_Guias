namespace GuiasMadeira.Domain.Entities;

public sealed class Proprietario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Distrito { get; set; }
    public string? Concelho { get; set; }
    public string? Freguesia { get; set; }

    public override string ToString() => Nome;
}
