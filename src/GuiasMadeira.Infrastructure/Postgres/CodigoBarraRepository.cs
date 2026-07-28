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

    public async Task<HashSet<string>> ListCodigosExistentesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition("SELECT codigo FROM codigos_barras", cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<string>(command);
        return new HashSet<string>(result, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<int> InsertManyAsync(IReadOnlyCollection<CodigoBarra> codigosBarras, CancellationToken cancellationToken = default)
    {
        if (codigosBarras.Count == 0)
        {
            return 0;
        }

        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            INSERT INTO codigos_barras (codigo, numero_certificado, numero_ugf)
            VALUES (@Codigo, @NumeroCertificado, @NumeroUgf)
            """,
            codigosBarras,
            cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }
}
