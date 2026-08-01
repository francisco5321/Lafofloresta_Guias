namespace GuiasMadeira.IntegrationTests;

/// <summary>
/// Cada método de teste xUnit recebe uma nova instância da classe de teste, por isso limpar as
/// tabelas em InitializeAsync dá a cada teste uma base de dados limpa sem recriar o container
/// Postgres (partilhado por toda a coleção "Postgres").
/// </summary>
[Collection("Postgres")]
public abstract class RepositoryTestBase : IAsyncLifetime
{
    protected RepositoryTestBase(PostgresContainerFixture fixture)
    {
        Fixture = fixture;
    }

    protected PostgresContainerFixture Fixture { get; }

    public Task InitializeAsync() => Fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
