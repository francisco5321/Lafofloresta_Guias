using Dapper;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Infrastructure.Postgres;

public sealed class RolariaRepository
{
    private readonly PostgresConnectionFactory connectionFactory;

    public RolariaRepository(PostgresConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Rolaria>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "SELECT id AS Id, tipo AS Tipo FROM rolarias ORDER BY tipo",
            cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<Rolaria>(command);
        return result.AsList();
    }

    public async Task<int> InsertAsync(Rolaria rolaria, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "INSERT INTO rolarias (tipo) VALUES (@Tipo) RETURNING id",
            rolaria,
            cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command);
    }
}
