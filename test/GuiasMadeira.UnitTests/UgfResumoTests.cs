using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.UnitTests;

public class UgfResumoTests
{
    [Fact]
    public void ToneladasDisponiveis_NaoFicaNegativa_QuandoImportadasExcedemCertificado()
    {
        var ugf = new UgfResumo { ToneladasCertificado = 100, ToneladasImportadas = 150 };

        Assert.Equal(0, ugf.ToneladasDisponiveis);
    }

    [Fact]
    public void ToneladasDisponiveisComTolerancia_AdicionaVinte_Porcento_AoCertificado()
    {
        var ugf = new UgfResumo { ToneladasCertificado = 100, ToneladasImportadas = 100 };

        Assert.Equal(20, ugf.ToneladasDisponiveisComTolerancia);
    }

    [Theory]
    [InlineData(120, EstadoUgf.Bloqueado)]    // ultrapassou mesmo a tolerância de 20%
    [InlineData(100, EstadoUgf.Vermelho)]     // 0% disponível (mas ainda há tolerância) -> vermelho
    [InlineData(91, EstadoUgf.Vermelho)]      // 9% disponível
    [InlineData(90, EstadoUgf.Vermelho)]      // fronteira: exatamente 10% disponível
    [InlineData(89, EstadoUgf.Amarelo)]       // 11% disponível
    [InlineData(70, EstadoUgf.Amarelo)]       // fronteira: exatamente 30% disponível
    [InlineData(69, EstadoUgf.Verde)]         // 31% disponível
    public void Estado_ReflecteSemaforo_DeAcordoComPercentagemDisponivel(decimal importadas, EstadoUgf esperado)
    {
        var ugf = new UgfResumo { ToneladasCertificado = 100, ToneladasImportadas = importadas };

        Assert.Equal(esperado, ugf.Estado);
    }

    [Fact]
    public void Estado_EBloqueado_QuandoCertificadoNaoTemLimiteDefinido()
    {
        var ugf = new UgfResumo { ToneladasCertificado = 0, ToneladasImportadas = 0 };

        Assert.Equal(EstadoUgf.Bloqueado, ugf.Estado);
    }

    [Fact]
    public void NumeroMaximoGuias_EhNull_QuandoNaoHaCargaMediaDefinida()
    {
        var ugf = new UgfResumo { ToneladasCertificado = 100, ToneladasImportadas = 0, CargaMediaToneladas = null };

        Assert.Null(ugf.NumeroMaximoGuiasBase);
        Assert.Null(ugf.NumeroMaximoGuias);
        Assert.Null(ugf.GuiasRestantes);
        Assert.False(ugf.LimiteGuiasAtingido);
        Assert.Equal("—", ugf.GuiasRotulo);
    }

    [Fact]
    public void NumeroMaximoGuias_ContaComTolerancia_NumeroMaximoGuiasBase_NaoConta()
    {
        // 100t certificadas, carga média de 10t/guia: base = 10 guias, com tolerância de 20% = 12 guias.
        var ugf = new UgfResumo { ToneladasCertificado = 100, ToneladasImportadas = 0, CargaMediaToneladas = 10 };

        Assert.Equal(10, ugf.NumeroMaximoGuiasBase);
        Assert.Equal(12, ugf.NumeroMaximoGuias);
    }

    [Fact]
    public void LimiteGuiasEmTolerancia_EhVerdadeiro_EntreOBaseEOMaximo()
    {
        var ugf = new UgfResumo
        {
            ToneladasCertificado = 100,
            ToneladasImportadas = 0,
            CargaMediaToneladas = 10,
            GuiasCriadas = 11 // base=10, máximo=12 -> já passou o base mas ainda não atingiu o máximo
        };

        Assert.False(ugf.LimiteGuiasAtingido);
        Assert.True(ugf.LimiteGuiasEmTolerancia);
        Assert.Equal(1, ugf.GuiasRestantes);
        Assert.Equal("11 / 12", ugf.GuiasRotulo);
    }

    [Fact]
    public void LimiteGuiasAtingido_EhVerdadeiro_QuandoGuiasCriadasIgualaOMaximo()
    {
        var ugf = new UgfResumo
        {
            ToneladasCertificado = 100,
            ToneladasImportadas = 0,
            CargaMediaToneladas = 10,
            GuiasCriadas = 12
        };

        Assert.True(ugf.LimiteGuiasAtingido);
        Assert.False(ugf.LimiteGuiasEmTolerancia);
        Assert.Equal(0, ugf.GuiasRestantes);
    }
}
