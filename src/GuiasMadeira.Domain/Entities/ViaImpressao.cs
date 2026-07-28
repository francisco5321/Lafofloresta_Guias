namespace GuiasMadeira.Domain.Entities;

public enum ViaImpressao
{
    Original = 1,
    Duplicado = 2,
    Triplicado = 3,
    Quadriplicado = 4
}

public static class ViaImpressaoExtensions
{
    public static string NomeVia(this ViaImpressao via) => via switch
    {
        ViaImpressao.Original => "ORIGINAL",
        ViaImpressao.Duplicado => "DUPLICADO",
        ViaImpressao.Triplicado => "TRIPLICADO",
        ViaImpressao.Quadriplicado => "QUADRIPLICADO",
        _ => via.ToString().ToUpperInvariant()
    };
}
