namespace GuiasMadeira.Domain.Entities;

public sealed class Rolaria
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;

    public override string ToString() => Tipo;
}
