using Dapper;
using GuiasMadeira.Domain.Entities;
using Npgsql;

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

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition("SELECT count(*) FROM proprietarios", cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command);
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

    public async Task UpdateAsync(Proprietario proprietario, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            UPDATE proprietarios
            SET nome = @Nome, distrito = @Distrito, concelho = @Concelho,
                freguesia = @Freguesia, codigo_prop = @CodigoProp, parcela = @Parcela
            WHERE id = @Id
            """,
            proprietario,
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "DELETE FROM proprietarios WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken);
        try
        {
            await connection.ExecuteAsync(command);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw new RegistoEmUsoException(
                "Não é possível apagar este proprietário: está associado a guias existentes.");
        }
    }
}
