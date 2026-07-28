using Dapper;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Infrastructure.Postgres;

public sealed class ProprietarioRepository
{
    private readonly PostgresConnectionFactory connectionFactory;

    public ProprietarioRepository(PostgresConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Proprietario>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            SELECT id AS Id, nome AS Nome, distrito AS Distrito, concelho AS Concelho,
                   freguesia AS Freguesia, codigo_prop AS CodigoProp, parcela AS Parcela
            FROM proprietarios
            ORDER BY nome
            """,
            cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<Proprietario>(command);
        return result.AsList();
    }

    public async Task<int> InsertAsync(Proprietario proprietario, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            INSERT INTO proprietarios (nome, distrito, concelho, freguesia, codigo_prop, parcela)
            VALUES (@Nome, @Distrito, @Concelho, @Freguesia, @CodigoProp, @Parcela)
            RETURNING id
            """,
            proprietario,
            cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command);
    }
}
