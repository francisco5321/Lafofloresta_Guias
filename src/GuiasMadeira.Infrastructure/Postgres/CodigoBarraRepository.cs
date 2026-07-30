using Dapper;
using GuiasMadeira.Domain.Entities;
using Npgsql;

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

    /// <summary>
    /// Códigos UGF já em uso nas vinhetas, para a página de Limites UGF sugerir. O código UGF
    /// (formato "UGFPT#####") está guardado na coluna numero_certificado — a coluna numero_ugf
    /// guarda antes a referência de Chain of Custody (ex. "GFA-FM/COC-######").
    /// </summary>
    public async Task<IReadOnlyList<string>> ListNumerosUgfExistentesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            SELECT DISTINCT numero_certificado
            FROM codigos_barras
            WHERE numero_certificado IS NOT NULL AND numero_certificado <> ''
            ORDER BY numero_certificado
            """,
            cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<string>(command);
        return result.AsList();
    }

    /// <summary>
    /// Certificados com pelo menos uma vinheta ainda não atribuída a nenhuma guia, com a respetiva
    /// contagem, para o seletor de "Número de certificado" em Nova guia. Passar o id de uma guia em
    /// edição em <paramref name="guiaIdParaExcluir"/> faz a vinheta já atribuída a essa guia contar
    /// como disponível para ela própria (pré-preenchimento sem reatribuir vinheta).
    /// </summary>
    public async Task<IReadOnlyList<CertificadoResumo>> ListCertificadosDisponiveisAsync(int? guiaIdParaExcluir = null, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            SELECT numero_certificado AS NumeroCertificado, COUNT(*) AS VinhetasDisponiveis
            FROM codigos_barras c
            WHERE numero_certificado IS NOT NULL AND numero_certificado <> ''
              AND NOT EXISTS (
                  SELECT 1 FROM guias g
                  WHERE g.codigo_barra_id = c.id AND (@guiaIdParaExcluir::int IS NULL OR g.id <> @guiaIdParaExcluir)
              )
            GROUP BY numero_certificado
            ORDER BY numero_certificado
            """,
            new { guiaIdParaExcluir },
            cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<CertificadoResumo>(command);
        return result.AsList();
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

    public async Task UpdateAsync(CodigoBarra codigoBarra, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            UPDATE codigos_barras
            SET codigo = @Codigo, numero_certificado = @NumeroCertificado, numero_ugf = @NumeroUgf
            WHERE id = @Id
            """,
            codigoBarra,
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "DELETE FROM codigos_barras WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken);
        try
        {
            await connection.ExecuteAsync(command);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw new RegistoEmUsoException(
                "Não é possível apagar esta vinheta: está associada a guias existentes.");
        }
    }
}
