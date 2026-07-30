using Dapper;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Infrastructure.Postgres;

public sealed class UgfRepository
{
    private readonly PostgresConnectionFactory connectionFactory;

    public UgfRepository(PostgresConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<UgfResumo>> ListResumoAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            SELECT
                u.id AS Id,
                u.codigo AS Codigo,
                u.toneladas_certificado AS ToneladasCertificado,
                COALESCE(SUM(e.toneladas), 0) AS ToneladasImportadas
            FROM ugfs u
            LEFT JOIN ugf_entradas e ON e.ugf_id = u.id
            GROUP BY u.id, u.codigo, u.toneladas_certificado
            ORDER BY u.codigo
            """,
            cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<UgfResumo>(command);
        return result.AsList();
    }

    public async Task<int> InsertAsync(Ugf ugf, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            INSERT INTO ugfs (codigo, toneladas_certificado)
            VALUES (@Codigo, @ToneladasCertificado)
            RETURNING id
            """,
            ugf,
            cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command);
    }

    public async Task UpdateAsync(Ugf ugf, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "UPDATE ugfs SET codigo = @Codigo, toneladas_certificado = @ToneladasCertificado WHERE id = @Id",
            ugf,
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "DELETE FROM ugfs WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task RegistarEntradaAsync(int ugfId, decimal toneladas, string? origem, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "INSERT INTO ugf_entradas (ugf_id, toneladas, origem) VALUES (@ugfId, @toneladas, @origem)",
            new { ugfId, toneladas, origem },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}
