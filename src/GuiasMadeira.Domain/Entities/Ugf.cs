namespace GuiasMadeira.Domain.Entities;

public sealed class Ugf
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public decimal ToneladasCertificado { get; set; }

    public override string ToString() => Codigo;
}
