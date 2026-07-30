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
                u.carga_media_toneladas AS CargaMediaToneladas,
                COALESCE(entradas.total, 0) AS ToneladasImportadas,
                COALESCE(guias.total, 0) AS GuiasCriadas
            FROM ugfs u
            LEFT JOIN (
                SELECT ugf_id, SUM(toneladas) AS total
                FROM ugf_entradas
                GROUP BY ugf_id
            ) entradas ON entradas.ugf_id = u.id
            LEFT JOIN (
                SELECT c.numero_certificado AS codigo, COUNT(*) AS total
                FROM codigos_barras c
                JOIN guias g ON g.codigo_barra_id = c.id
                GROUP BY c.numero_certificado
            ) guias ON guias.codigo = u.codigo
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
            INSERT INTO ugfs (codigo, toneladas_certificado, carga_media_toneladas)
            VALUES (@Codigo, @ToneladasCertificado, @CargaMediaToneladas)
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
            """
            UPDATE ugfs
            SET codigo = @Codigo, toneladas_certificado = @ToneladasCertificado, carga_media_toneladas = @CargaMediaToneladas
            WHERE id = @Id
            """,
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
