namespace GuiasMadeira.Domain.Entities;

/// <summary>
/// Um número de certificado com a contagem de vinhetas ainda disponíveis (não atribuídas a
/// nenhuma guia), para o seletor de "Número de certificado" em Nova guia.
/// </summary>
public sealed class CertificadoResumo
{
    public string NumeroCertificado { get; set; } = string.Empty;
    public int VinhetasDisponiveis { get; set; }

    /// <summary>
    /// Guias ainda permitidas pelo limite do UGF associado (página Limites UGF), preenchido pela
    /// página de Nova guia depois de carregar os dois conjuntos de dados — null se esse UGF não
    /// tiver "Valor médio da carga" definido (sem limite de guias configurado).
    /// </summary>
    public int? GuiasRestantesUgf { get; set; }

    /// <summary>
    /// Quantas guias se podem realmente ainda criar para este certificado: o menor entre as
    /// vinhetas físicas disponíveis e o limite de guias do UGF, quando este estiver definido.
    /// </summary>
    public int DisponiveisEfetivo => GuiasRestantesUgf is int restantes ? Math.Min(VinhetasDisponiveis, restantes) : VinhetasDisponiveis;

    public string Rotulo => $"{NumeroCertificado} ({DisponiveisEfetivo} disponíve{(DisponiveisEfetivo == 1 ? "l" : "eis")})";

    public override string ToString() => Rotulo;
}
