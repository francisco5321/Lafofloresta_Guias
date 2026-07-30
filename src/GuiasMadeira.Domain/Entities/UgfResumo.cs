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

    public decimal ToneladasDisponiveis => ToneladasCertificado - ToneladasImportadas;

    public decimal ToneladasDisponiveisComTolerancia => ToneladasCertificado * 1.2m - ToneladasImportadas;

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
