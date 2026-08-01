using Dapper;
using GuiasMadeira.Infrastructure.Postgres;
using Npgsql;
using System.IO;
using Testcontainers.PostgreSql;

namespace GuiasMadeira.IntegrationTests;

/// <summary>
/// Disponibiliza um Postgres para a coleção de testes, de uma de duas formas:
///
/// 1. Se a variável de ambiente <see cref="AdminConnectionEnvVar"/> estiver definida (uma connection
///    string com permissão para CREATE/DROP DATABASE, ex. ligação ao Postgres local de
///    desenvolvimento), cria uma base de dados descartável com nome único nesse servidor, aplica o
///    schema.sql e apaga-a no fim. Não toca em nenhuma base de dados existente (ex. "guias_madeira").
///    Não requer Docker.
/// 2. Caso contrário, usa Testcontainers para subir um Postgres efémero em Docker (comportamento
///    por omissão, preferido quando o Docker está disponível).
///
/// Em ambos os casos aplica-se o mesmo schema.sql real do projeto e, depois, os repositórios
/// passam a ligar-se com um role restrito (mesmos privilégios mínimos do
/// provisionar_role_app.sql de produção) em vez do superuser usado para preparar a base de dados —
/// isto faz a suite inteira validar que esses privilégios mínimos chegam mesmo para a aplicação
/// funcionar, não é só documentação.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private const string AdminConnectionEnvVar = "GUIASMADEIRA_TEST_PG_CONNECTION";

    private PostgreSqlContainer? container;
    private string? adminConnectionString;
    private string? databaseName;
    private string? restrictedRoleName;
    private string adminDatabaseConnectionString = null!;

    /// <summary>Connection factory que liga como o role restrito — o que os repositórios usam nos testes.</summary>
    public PostgresConnectionFactory ConnectionFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable(AdminConnectionEnvVar);
        adminDatabaseConnectionString = !string.IsNullOrWhiteSpace(connectionString)
            ? await InitializeFromExistingServerAsync(connectionString)
            : await InitializeFromTestcontainersAsync();

        var schema = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "schema.sql"));
        await using var adminDbConnection = new NpgsqlConnection(adminDatabaseConnectionString);
        await adminDbConnection.OpenAsync();
        await adminDbConnection.ExecuteAsync(schema);

        ConnectionFactory = await ProvisionRestrictedRoleAsync(adminDbConnection, adminDatabaseConnectionString);
    }

    private async Task<string> InitializeFromExistingServerAsync(string connectionString)
    {
        adminConnectionString = connectionString;
        databaseName = $"guiasmadeira_tests_{Guid.NewGuid():N}";

        await using var adminConnection = new NpgsqlConnection(adminConnectionString);
        await adminConnection.OpenAsync();
        await adminConnection.ExecuteAsync($"CREATE DATABASE \"{databaseName}\"");

        return new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = databaseName }.ConnectionString;
    }

    private async Task<string> InitializeFromTestcontainersAsync()
    {
        container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("guias_madeira_tests")
            .WithUsername("guias_test")
            .WithPassword("guias_test")
            .Build();

        await container.StartAsync();

        return container.GetConnectionString();
    }

    /// <summary>
    /// Cria um role com o mesmo desenho mínimo do
    /// <c>GuiasMadeira.Infrastructure/Postgres/provisionar_role_app.sql</c> de produção (sem
    /// SUPERUSER/CREATEDB/CREATEROLE, CRUD só nas tabelas do schema public) e devolve uma
    /// connection factory que liga como esse role em vez do superuser usado para preparar a base.
    /// </summary>
    private async Task<PostgresConnectionFactory> ProvisionRestrictedRoleAsync(NpgsqlConnection adminDbConnection, string adminDatabaseConnectionString)
    {
        restrictedRoleName = $"guiasmadeira_role_{Guid.NewGuid():N}";
        var rolePassword = Guid.NewGuid().ToString("N");
        var databaseAtual = adminDbConnection.Database!;

        await adminDbConnection.ExecuteAsync(
            $"""
            CREATE ROLE "{restrictedRoleName}" LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION PASSWORD '{rolePassword}';
            GRANT CONNECT ON DATABASE "{databaseAtual}" TO "{restrictedRoleName}";
            GRANT USAGE ON SCHEMA public TO "{restrictedRoleName}";
            REVOKE CREATE ON SCHEMA public FROM "{restrictedRoleName}";
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO "{restrictedRoleName}";
            GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO "{restrictedRoleName}";
            """);

        var builder = new NpgsqlConnectionStringBuilder(adminDatabaseConnectionString)
        {
            Username = restrictedRoleName,
            Password = rolePassword
        };
        return new PostgresConnectionFactory(builder.ConnectionString);
    }

    /// <summary>
    /// Usa a ligação de administração (não o role restrito) porque TRUNCATE é uma limpeza da
    /// infraestrutura de testes entre casos, não uma operação que a aplicação em produção alguma
    /// vez precise de fazer — por isso não faz parte dos privilégios concedidos ao role da app.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(adminDatabaseConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            """
            TRUNCATE TABLE ugf_entradas, ugfs, guias, codigos_barras, rolarias, destinatarios, proprietarios
            RESTART IDENTITY CASCADE
            """);
    }

    public async Task DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
            return;
        }

        if (adminConnectionString is not null && databaseName is not null)
        {
            await using var adminConnection = new NpgsqlConnection(adminConnectionString);
            await adminConnection.OpenAsync();
            // Fecha ligações residuais do pool do Npgsql antes do DROP DATABASE, que falha se
            // houver sessões ativas nessa base de dados.
            await adminConnection.ExecuteAsync(
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @databaseName AND pid <> pg_backend_pid()",
                new { databaseName });
            await adminConnection.ExecuteAsync($"DROP DATABASE IF EXISTS \"{databaseName}\"");

            if (restrictedRoleName is not null)
            {
                await adminConnection.ExecuteAsync($"DROP ROLE IF EXISTS \"{restrictedRoleName}\"");
            }
        }
    }
}

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>;
