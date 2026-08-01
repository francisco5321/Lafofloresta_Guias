using GuiasMadeira.Domain.Entities;
using GuiasMadeira.Infrastructure.Postgres;

namespace GuiasMadeira.IntegrationTests;

public class CodigoBarraRepositoryTests : RepositoryTestBase
{
    private readonly CodigoBarraRepository repository;

    public CodigoBarraRepositoryTests(PostgresContainerFixture fixture) : base(fixture)
    {
        repository = new CodigoBarraRepository(fixture.ConnectionFactory);
    }

    [Fact]
    public async Task InsertManyAsync_InserePercebendoZeroQuandoListaVazia()
    {
        var inseridos = await repository.InsertManyAsync([]);

        Assert.Equal(0, inseridos);
        Assert.Empty(await repository.ListAllAsync());
    }

    [Fact]
    public async Task InsertManyAsync_InsereTodosOsCodigosNumaSoOperacao()
    {
        var inseridos = await repository.InsertManyAsync(
        [
            new CodigoBarra { Codigo = "111", NumeroCertificado = "CERT-1", NumeroUgf = "UGFPT00001" },
            new CodigoBarra { Codigo = "222", NumeroCertificado = "CERT-1", NumeroUgf = "UGFPT00001" },
        ]);

        Assert.Equal(2, inseridos);
        Assert.Equal(2, (await repository.ListAllAsync()).Count);
    }

    [Fact]
    public async Task ListCodigosExistentesAsync_NaoDistingueMaiusculasDeMinusculas()
    {
        await repository.InsertManyAsync([new CodigoBarra { Codigo = "AbC123" }]);

        var existentes = await repository.ListCodigosExistentesAsync();

        Assert.Contains("abc123", existentes);
        Assert.Contains("ABC123", existentes);
    }

    [Fact]
    public async Task ListCertificadosDisponiveisAsync_SoDevolveCertificadosComVinhetaLivre()
    {
        await repository.InsertManyAsync(
        [
            new CodigoBarra { Codigo = "111", NumeroCertificado = "CERT-LIVRE" },
            new CodigoBarra { Codigo = "222", NumeroCertificado = "CERT-OCUPADO" },
        ]);
        var codigos = await repository.ListAllAsync();
        var ocupado = codigos.Single(c => c.NumeroCertificado == "CERT-OCUPADO");

        var guiaRepository = new GuiaRepository(Fixture.ConnectionFactory);
        await guiaRepository.InsertAsync(new Guia { CodigoBarraId = ocupado.Id });

        var disponiveis = await repository.ListCertificadosDisponiveisAsync();

        var resumo = Assert.Single(disponiveis);
        Assert.Equal("CERT-LIVRE", resumo.NumeroCertificado);
        Assert.Equal(1, resumo.VinhetasDisponiveis);
    }

    [Fact]
    public async Task ListCertificadosDisponiveisAsync_ContaAVinhetaDaGuiaEmEdicaoComoDisponivelParaEla()
    {
        await repository.InsertManyAsync([new CodigoBarra { Codigo = "111", NumeroCertificado = "CERT-A" }]);
        var codigo = Assert.Single(await repository.ListAllAsync());

        var guiaRepository = new GuiaRepository(Fixture.ConnectionFactory);
        var guiaId = await guiaRepository.InsertAsync(new Guia { CodigoBarraId = codigo.Id });

        var disponiveisExcluindoGuia = await repository.ListCertificadosDisponiveisAsync(guiaId);
        var disponiveisSemExcluir = await repository.ListCertificadosDisponiveisAsync();

        Assert.Single(disponiveisExcluindoGuia);
        Assert.Empty(disponiveisSemExcluir);
    }

    [Fact]
    public async Task DeleteAsync_LancaRegistoEmUsoException_QuandoVinhetaTemGuiaAssociada()
    {
        await repository.InsertManyAsync([new CodigoBarra { Codigo = "111" }]);
        var codigo = Assert.Single(await repository.ListAllAsync());
        var guiaRepository = new GuiaRepository(Fixture.ConnectionFactory);
        await guiaRepository.InsertAsync(new Guia { CodigoBarraId = codigo.Id });

        await Assert.ThrowsAsync<RegistoEmUsoException>(() => repository.DeleteAsync(codigo.Id));
    }
}
