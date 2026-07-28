using Dapper;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Infrastructure.Postgres;

public sealed class GuiaRepository
{
    private readonly PostgresConnectionFactory connectionFactory;

    public GuiaRepository(PostgresConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<int> InsertAsync(Guia guia, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            INSERT INTO guias (destinatario_id, proprietario_id, codigo_barra_id, rolaria_id, fornecedor)
            VALUES (@DestinatarioId, @ProprietarioId, @CodigoBarraId, @RolariaId, @Fornecedor)
            RETURNING id
            """,
            guia,
            cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command);
    }

    public async Task<IReadOnlyList<int>> ListAllIdsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "SELECT id FROM guias ORDER BY id DESC",
            cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<int>(command);
        return result.AsList();
    }

    /// <summary>
    /// Equivalente à query "Relatorio" do Access (sem o produto cartesiano com Vias).
    /// </summary>
    public async Task<GuiaImpressao?> GetGuiaImpressaoAsync(int idGuia, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            SELECT
                g.id AS IdGuia,
                d.nome AS DestinatarioNome,
                d.nif AS DestinatarioNif,
                d.morada AS DestinatarioMorada,
                p.nome AS ProprietarioNome,
                p.distrito AS ProprietarioDistrito,
                p.concelho AS ProprietarioConcelho,
                p.freguesia AS ProprietarioFreguesia,
                g.fornecedor AS Fornecedor,
                r.tipo AS RolariaTipo,
                c.codigo AS CodigoBarraCodigo,
                c.numero_certificado AS NumeroCertificado,
                c.numero_ugf AS NumeroUgf
            FROM guias g
            LEFT JOIN destinatarios d ON g.destinatario_id = d.id
            LEFT JOIN proprietarios p ON g.proprietario_id = p.id
            LEFT JOIN rolarias r ON g.rolaria_id = r.id
            LEFT JOIN codigos_barras c ON g.codigo_barra_id = c.id
            WHERE g.id = @idGuia
            """,
            new { idGuia },
            cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<GuiaImpressao>(command);
    }
}
