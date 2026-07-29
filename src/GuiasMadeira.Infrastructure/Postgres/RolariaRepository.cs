using Dapper;
using GuiasMadeira.Domain.Entities;
using Npgsql;

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

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition("SELECT count(*) FROM rolarias", cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command);
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

    public async Task UpdateAsync(Rolaria rolaria, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "UPDATE rolarias SET tipo = @Tipo WHERE id = @Id",
            rolaria,
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "DELETE FROM rolarias WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken);
        try
        {
            await connection.ExecuteAsync(command);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw new RegistoEmUsoException(
                "Não é possível apagar esta rolaria: está associada a guias existentes.");
        }
    }
}
