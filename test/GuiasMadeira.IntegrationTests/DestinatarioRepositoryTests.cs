using GuiasMadeira.Domain.Entities;
using GuiasMadeira.Infrastructure.Postgres;

namespace GuiasMadeira.IntegrationTests;

public class DestinatarioRepositoryTests : RepositoryTestBase
{
    private readonly DestinatarioRepository repository;

    public DestinatarioRepositoryTests(PostgresContainerFixture fixture) : base(fixture)
    {
        repository = new DestinatarioRepository(fixture.ConnectionFactory);
    }

    [Fact]
    public async Task InsertAsync_DevolveIdGeradoEPersisteTodosOsCampos()
    {
        var id = await repository.InsertAsync(new Destinatario
        {
            Nome = "Empresa Lda",
            Nif = "123456789",
            Morada = "Rua Principal, 1",
            Concelho = "Coimbra"
        });

        var lista = await repository.ListAllAsync();
        var guardado = Assert.Single(lista);
        Assert.Equal(id, guardado.Id);
        Assert.Equal("123456789", guardado.Nif);
    }

    [Fact]
    public async Task UpdateAsync_AlteraOsCamposDoRegistoExistente()
    {
        var id = await repository.InsertAsync(new Destinatario { Nome = "Original" });

        await repository.UpdateAsync(new Destinatario { Id = id, Nome = "Atualizado", Nif = "999" });

        var guardado = Assert.Single(await repository.ListAllAsync());
        Assert.Equal("Atualizado", guardado.Nome);
        Assert.Equal("999", guardado.Nif);
    }

    [Fact]
    public async Task DeleteAsync_LancaRegistoEmUsoException_QuandoDestinatarioTemGuiasAssociadas()
    {
        var destinatarioId = await repository.InsertAsync(new Destinatario { Nome = "Com guia" });
        var guiaRepository = new GuiaRepository(Fixture.ConnectionFactory);
        await guiaRepository.InsertAsync(new Guia { DestinatarioId = destinatarioId });

        await Assert.ThrowsAsync<RegistoEmUsoException>(() => repository.DeleteAsync(destinatarioId));
    }
}
