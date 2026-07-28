namespace GuiasMadeira.Domain.Entities;

public sealed class CodigoBarra
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string? NumeroCertificado { get; set; }
    public string? NumeroUgf { get; set; }

    public override string ToString() => Codigo;
}
