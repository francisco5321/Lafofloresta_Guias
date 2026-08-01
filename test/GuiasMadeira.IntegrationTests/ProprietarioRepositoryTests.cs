using GuiasMadeira.Domain.Entities;
using GuiasMadeira.Infrastructure.Postgres;

namespace GuiasMadeira.IntegrationTests;

public class ProprietarioRepositoryTests : RepositoryTestBase
{
    private readonly ProprietarioRepository repository;

    public ProprietarioRepositoryTests(PostgresContainerFixture fixture) : base(fixture)
    {
        repository = new ProprietarioRepository(fixture.ConnectionFactory);
    }

    [Fact]
    public async Task InsertAsync_DevolveIdGeradoEPersisteTodosOsCampos()
    {
        var id = await repository.InsertAsync(new Proprietario
        {
            Nome = "João Silva",
            Distrito = "Viseu",
            Concelho = "Tondela",
            Freguesia = "Guardão"
        });

        var lista = await repository.ListAllAsync();
        var guardado = Assert.Single(lista);
        Assert.Equal(id, guardado.Id);
        Assert.Equal("João Silva", guardado.Nome);
        Assert.Equal("Tondela", guardado.Concelho);
    }

    [Fact]
    public async Task ListAllAsync_DevolveOrdenadoPorNome()
    {
        await repository.InsertAsync(new Proprietario { Nome = "Zeferino" });
        await repository.InsertAsync(new Proprietario { Nome = "Ana" });

        var lista = await repository.ListAllAsync();

        Assert.Equal(["Ana", "Zeferino"], lista.Select(p => p.Nome));
    }

    [Fact]
    public async Task CountAsync_ReflecteNumeroDeRegistos()
    {
        Assert.Equal(0, await repository.CountAsync());

        await repository.InsertAsync(new Proprietario { Nome = "Um" });
        await repository.InsertAsync(new Proprietario { Nome = "Dois" });

        Assert.Equal(2, await repository.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_AlteraOsCamposDoRegistoExistente()
    {
        var id = await repository.InsertAsync(new Proprietario { Nome = "Original" });

        await repository.UpdateAsync(new Proprietario { Id = id, Nome = "Atualizado", Concelho = "Novo Concelho" });

        var lista = await repository.ListAllAsync();
        var guardado = Assert.Single(lista);
        Assert.Equal("Atualizado", guardado.Nome);
        Assert.Equal("Novo Concelho", guardado.Concelho);
    }

    [Fact]
    public async Task DeleteAsync_RemoveORegisto_QuandoNaoTemGuiasAssociadas()
    {
        var id = await repository.InsertAsync(new Proprietario { Nome = "Descartável" });

        await repository.DeleteAsync(id);

        Assert.Empty(await repository.ListAllAsync());
    }

    [Fact]
    public async Task DeleteAsync_LancaRegistoEmUsoException_QuandoProprietarioTemGuiasAssociadas()
    {
        var proprietarioId = await repository.InsertAsync(new Proprietario { Nome = "Com guia" });
        var guiaRepository = new GuiaRepository(Fixture.ConnectionFactory);
        await guiaRepository.InsertAsync(new Guia { ProprietarioId = proprietarioId });

        await Assert.ThrowsAsync<RegistoEmUsoException>(() => repository.DeleteAsync(proprietarioId));
    }
}
