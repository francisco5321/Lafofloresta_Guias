namespace GuiasMadeira.Domain.Entities;

/// <summary>
/// Linha de listagem de um UGF: junta o limite certificado com o total já importado, e calcula
/// o disponível (a 100% e com a tolerância de 20%) e o estado do semáforo.
/// </summary>
public sealed class UgfResumo
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public decimal ToneladasCertificado { get; set; }
    public decimal ToneladasImportadas { get; set; }
    public decimal? CargaMediaToneladas { get; set; }
    public int GuiasCriadas { get; set; }

    public decimal ToneladasDisponiveis => Math.Max(0, ToneladasCertificado - ToneladasImportadas);

    public decimal ToneladasDisponiveisComTolerancia => Math.Max(0, ToneladasCertificado * 1.2m - ToneladasImportadas);

    /// <summary>Quantas guias caberiam só nos 100% base (sem contar a tolerância de 20%).</summary>
    public int? NumeroMaximoGuiasBase => CalcularNumeroMaximoGuias(ToneladasDisponiveis);

    /// <summary>Teto real de guias para este certificado, já a contar com a tolerância de 20%.</summary>
    public int? NumeroMaximoGuias => CalcularNumeroMaximoGuias(ToneladasDisponiveisComTolerancia);

    public int? GuiasRestantes => NumeroMaximoGuias is int maximo ? Math.Max(0, maximo - GuiasCriadas) : null;

    /// <summary>Já não há guias possíveis para este certificado (teto dos 120% atingido).</summary>
    public bool LimiteGuiasAtingido => GuiasRestantes == 0;

    /// <summary>Guias já criadas ultrapassam os 100% base — a consumir a tolerância de 20%, mas ainda não bloqueado.</summary>
    public bool LimiteGuiasEmTolerancia =>
        !LimiteGuiasAtingido && NumeroMaximoGuiasBase is int maximoBase && GuiasCriadas >= maximoBase;

    public string GuiasRotulo => NumeroMaximoGuias is int maximo ? $"{GuiasCriadas} / {maximo}" : "—";

    private int? CalcularNumeroMaximoGuias(decimal toneladasDisponiveis)
    {
        if (CargaMediaToneladas is not decimal cargaMedia || cargaMedia <= 0)
        {
            return null;
        }

        return Math.Max(0, (int)Math.Floor(toneladasDisponiveis / cargaMedia));
    }

    public EstadoUgf Estado
    {
        get
        {
            if (ToneladasCertificado <= 0 || ToneladasDisponiveisComTolerancia <= 0)
            {
                return EstadoUgf.Bloqueado;
            }

            var restantePercentagem = ToneladasDisponiveis / ToneladasCertificado * 100;
            if (restantePercentagem <= 10)
            {
                return EstadoUgf.Vermelho;
            }

            if (restantePercentagem <= 30)
            {
                return EstadoUgf.Amarelo;
            }

            return EstadoUgf.Verde;
        }
    }
}
