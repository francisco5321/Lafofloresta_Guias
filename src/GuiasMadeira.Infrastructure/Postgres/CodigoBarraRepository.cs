using Dapper;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Infrastructure.Postgres;

public sealed class CodigoBarraRepository
{
    private readonly PostgresConnectionFactory connectionFactory;

    public CodigoBarraRepository(PostgresConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<CodigoBarra>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            SELECT id AS Id, codigo AS Codigo, numero_certificado AS NumeroCertificado, numero_ugf AS NumeroUgf
            FROM codigos_barras
            ORDER BY codigo
            """,
            cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<CodigoBarra>(command);
        return result.AsList();
    }
}
