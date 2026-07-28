using GuiasMadeira.Infrastructure.Postgres;
using GuiasMadeira.Pdf;

namespace GuiasMadeira.Desktop;

/// <summary>
/// Raiz de composição simples: a aplicação é pequena e não justifica
/// um contentor de injeção de dependências completo.
/// </summary>
public static class AppServices
{
    public static ProprietarioRepository Proprietarios { get; private set; } = null!;
    public static DestinatarioRepository Destinatarios { get; private set; } = null!;
    public static RolariaRepository Rolarias { get; private set; } = null!;
    public static CodigoBarraRepository CodigosBarras { get; private set; } = null!;
    public static GuiaRepository Guias { get; private set; } = null!;
    public static GuiaReportGenerator ReportGenerator { get; private set; } = null!;

    public static void Initialize(string connectionString)
    {
        var connectionFactory = new PostgresConnectionFactory(connectionString);
        Proprietarios = new ProprietarioRepository(connectionFactory);
        Destinatarios = new DestinatarioRepository(connectionFactory);
        Rolarias = new RolariaRepository(connectionFactory);
        CodigosBarras = new CodigoBarraRepository(connectionFactory);
        Guias = new GuiaRepository(connectionFactory);
        ReportGenerator = new GuiaReportGenerator();
    }
}
