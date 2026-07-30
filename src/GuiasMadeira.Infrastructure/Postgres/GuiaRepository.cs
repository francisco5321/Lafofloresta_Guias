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

    /// <summary>
    /// Cria a guia atribuindo, de forma atómica, a próxima vinheta ainda disponível do certificado
    /// indicado (FOR UPDATE SKIP LOCKED evita que duas guias criadas em simultâneo fiquem com a
    /// mesma vinheta). Devolve null se não houver nenhuma vinheta disponível para esse certificado.
    /// </summary>
    public async Task<int?> InsertComCertificadoAsync(Guia guia, string numeroCertificado, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            WITH vinheta AS (
                SELECT c.id
                FROM codigos_barras c
                WHERE c.numero_certificado = @numeroCertificado
                  AND NOT EXISTS (SELECT 1 FROM guias g WHERE g.codigo_barra_id = c.id)
                ORDER BY c.codigo
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            )
            INSERT INTO guias (destinatario_id, proprietario_id, codigo_barra_id, rolaria_id, fornecedor)
            SELECT @DestinatarioId, @ProprietarioId, vinheta.id, @RolariaId, @Fornecedor
            FROM vinheta
            RETURNING id
            """,
            new { guia.DestinatarioId, guia.ProprietarioId, guia.RolariaId, guia.Fornecedor, numeroCertificado },
            cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int?>(command);
    }

    /// <summary>
    /// Equivalente a <see cref="InsertComCertificadoAsync"/> mas para uma guia já existente cujo
    /// certificado mudou — a própria guia é excluída do "já em uso" para não bloquear a sua vinheta
    /// atual enquanto a troca não é confirmada. Devolve false se não houver vinheta disponível.
    /// </summary>
    public async Task<bool> AtualizarComCertificadoAsync(Guia guia, string numeroCertificado, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            WITH vinheta AS (
                SELECT c.id
                FROM codigos_barras c
                WHERE c.numero_certificado = @numeroCertificado
                  AND NOT EXISTS (
                      SELECT 1 FROM guias g
                      WHERE g.codigo_barra_id = c.id AND g.id <> @Id
                  )
                ORDER BY c.codigo
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            )
            UPDATE guias
            SET destinatario_id = @DestinatarioId, proprietario_id = @ProprietarioId,
                rolaria_id = @RolariaId, fornecedor = @Fornecedor, codigo_barra_id = vinheta.id
            FROM vinheta
            WHERE guias.id = @Id
            RETURNING guias.id
            """,
            new { guia.Id, guia.DestinatarioId, guia.ProprietarioId, guia.RolariaId, guia.Fornecedor, numeroCertificado },
            cancellationToken: cancellationToken);
        var idAtualizado = await connection.ExecuteScalarAsync<int?>(command);
        return idAtualizado is not null;
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

    public async Task<IReadOnlyList<GuiaResumo>> ListResumoAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            SELECT
                g.id AS Id,
                d.id AS DestinatarioId,
                d.nome AS DestinatarioNome,
                d.nif AS DestinatarioNif,
                p.id AS ProprietarioId,
                p.nome AS ProprietarioNome,
                c.id AS CodigoBarraId,
                c.codigo AS CodigoBarraCodigo,
                c.numero_certificado AS CodigoBarraNumeroCertificado,
                r.id AS RolariaId,
                r.tipo AS RolariaTipo,
                g.fornecedor AS Fornecedor,
                g.criado_em AS CriadoEm
            FROM guias g
            LEFT JOIN destinatarios d ON g.destinatario_id = d.id
            LEFT JOIN proprietarios p ON g.proprietario_id = p.id
            LEFT JOIN rolarias r ON g.rolaria_id = r.id
            LEFT JOIN codigos_barras c ON g.codigo_barra_id = c.id
            ORDER BY g.id DESC
            """,
            cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<GuiaResumo>(command);
        return result.AsList();
    }

    public async Task UpdateAsync(Guia guia, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            UPDATE guias
            SET destinatario_id = @DestinatarioId, proprietario_id = @ProprietarioId,
                codigo_barra_id = @CodigoBarraId, rolaria_id = @RolariaId, fornecedor = @Fornecedor
            WHERE id = @Id
            """,
            guia,
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "DELETE FROM guias WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
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
