using Dapper;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Infrastructure.Postgres;

public sealed class DestinatarioRepository
{
    private readonly PostgresConnectionFactory connectionFactory;

    public DestinatarioRepository(PostgresConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Destinatario>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            SELECT id AS Id, nome AS Nome, nif AS Nif, morada AS Morada, concelho AS Concelho
            FROM destinatarios
            ORDER BY nome
            """,
            cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<Destinatario>(command);
        return result.AsList();
    }

    public async Task<int> InsertAsync(Destinatario destinatario, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            INSERT INTO destinatarios (nome, nif, morada, concelho)
            VALUES (@Nome, @Nif, @Morada, @Concelho)
            RETURNING id
            """,
            destinatario,
            cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command);
    }
}
