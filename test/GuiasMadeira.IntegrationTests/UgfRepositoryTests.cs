using GuiasMadeira.Domain.Entities;
using GuiasMadeira.Infrastructure.Postgres;

namespace GuiasMadeira.IntegrationTests;

public class UgfRepositoryTests : RepositoryTestBase
{
    private readonly UgfRepository repository;

    public UgfRepositoryTests(PostgresContainerFixture fixture) : base(fixture)
    {
        repository = new UgfRepository(fixture.ConnectionFactory);
    }

    [Fact]
    public async Task InsertAsync_DevolveIdGeradoEPersisteTodosOsCampos()
    {
        var id = await repository.InsertAsync(new Ugf
        {
            Codigo = "UGFPT00001",
            ToneladasCertificado = 500,
            CargaMediaToneladas = 12.5m
        });

        var resumo = Assert.Single(await repository.ListResumoAsync());
        Assert.Equal(id, resumo.Id);
        Assert.Equal(500, resumo.ToneladasCertificado);
        Assert.Equal(12.5m, resumo.CargaMediaToneladas);
    }

    [Fact]
    public async Task ListResumoAsync_SomaAsEntradasDoUgf()
    {
        var id = await repository.InsertAsync(new Ugf { Codigo = "UGFPT00002", ToneladasCertificado = 100 });

        await repository.RegistarEntradaAsync(id, 30, "entrega 1");
        await repository.RegistarEntradaAsync(id, 20, "entrega 2");

        var resumo = Assert.Single(await repository.ListResumoAsync());
        Assert.Equal(50, resumo.ToneladasImportadas);
        Assert.Equal(50, resumo.ToneladasDisponiveis);
    }

    [Fact]
    public async Task ListResumoAsync_ContaAsGuiasCriadasParaOCertificadoCorrespondente()
    {
        var ugfId = await repository.InsertAsync(new Ugf { Codigo = "UGFPT00003", ToneladasCertificado = 100 });
        var codigosBarras = new CodigoBarraRepository(Fixture.ConnectionFactory);
        await codigosBarras.InsertManyAsync(
        [
            new CodigoBarra { Codigo = "111", NumeroCertificado = "UGFPT00003" },
            new CodigoBarra { Codigo = "222", NumeroCertificado = "UGFPT00003" },
        ]);
        var guiaRepository = new GuiaRepository(Fixture.ConnectionFactory);
        await guiaRepository.InsertComCertificadoAsync(new Guia(), "UGFPT00003");

        var resumo = Assert.Single(await repository.ListResumoAsync());
        Assert.Equal(1, resumo.GuiasCriadas);
        Assert.Equal(ugfId, resumo.Id);
    }

    [Fact]
    public async Task UpdateAsync_AlteraOsCamposDoRegistoExistente()
    {
        var id = await repository.InsertAsync(new Ugf { Codigo = "UGFPT00004", ToneladasCertificado = 100 });

        await repository.UpdateAsync(new Ugf { Id = id, Codigo = "UGFPT00004", ToneladasCertificado = 200, CargaMediaToneladas = 5 });

        var resumo = Assert.Single(await repository.ListResumoAsync());
        Assert.Equal(200, resumo.ToneladasCertificado);
        Assert.Equal(5, resumo.CargaMediaToneladas);
    }

    [Fact]
    public async Task DeleteAsync_RemoveOUgfEAsSuasEntradas()
    {
        var id = await repository.InsertAsync(new Ugf { Codigo = "UGFPT00005", ToneladasCertificado = 100 });
        await repository.RegistarEntradaAsync(id, 10, null);

        await repository.DeleteAsync(id);

        Assert.Empty(await repository.ListResumoAsync());
    }
}
