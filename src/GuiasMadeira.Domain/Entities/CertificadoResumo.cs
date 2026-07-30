namespace GuiasMadeira.Domain.Entities;

/// <summary>
/// Um número de certificado com a contagem de vinhetas ainda disponíveis (não atribuídas a
/// nenhuma guia), para o seletor de "Número de certificado" em Nova guia.
/// </summary>
public sealed class CertificadoResumo
{
    public string NumeroCertificado { get; set; } = string.Empty;
    public int VinhetasDisponiveis { get; set; }

    public string Rotulo => $"{NumeroCertificado} ({VinhetasDisponiveis} disponíve{(VinhetasDisponiveis == 1 ? "l" : "eis")})";

    public override string ToString() => Rotulo;
}
