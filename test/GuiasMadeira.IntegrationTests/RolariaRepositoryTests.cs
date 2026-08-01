using GuiasMadeira.Domain.Entities;
using GuiasMadeira.Infrastructure.Postgres;

namespace GuiasMadeira.IntegrationTests;

public class RolariaRepositoryTests : RepositoryTestBase
{
    private readonly RolariaRepository repository;

    public RolariaRepositoryTests(PostgresContainerFixture fixture) : base(fixture)
    {
        repository = new RolariaRepository(fixture.ConnectionFactory);
    }

    [Fact]
    public async Task InsertAsync_DevolveIdGeradoEPersisteOTipo()
    {
        var id = await repository.InsertAsync(new Rolaria { Tipo = "Pinho" });

        var guardado = Assert.Single(await repository.ListAllAsync());
        Assert.Equal(id, guardado.Id);
        Assert.Equal("Pinho", guardado.Tipo);
    }

    [Fact]
    public async Task DeleteAsync_LancaRegistoEmUsoException_QuandoRolariaTemGuiasAssociadas()
    {
        var rolariaId = await repository.InsertAsync(new Rolaria { Tipo = "Eucalipto" });
        var guiaRepository = new GuiaRepository(Fixture.ConnectionFactory);
        await guiaRepository.InsertAsync(new Guia { RolariaId = rolariaId });

        await Assert.ThrowsAsync<RegistoEmUsoException>(() => repository.DeleteAsync(rolariaId));
    }

    [Fact]
    public async Task DeleteAsync_RemoveORegisto_QuandoNaoTemGuiasAssociadas()
    {
        var id = await repository.InsertAsync(new Rolaria { Tipo = "Descartável" });

        await repository.DeleteAsync(id);

        Assert.Empty(await repository.ListAllAsync());
    }
}
