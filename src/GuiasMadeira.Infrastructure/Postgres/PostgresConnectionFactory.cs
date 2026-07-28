using Npgsql;

namespace GuiasMadeira.Infrastructure.Postgres;

public sealed class PostgresConnectionFactory
{
    private readonly string connectionString;

    public PostgresConnectionFactory(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public NpgsqlConnection CreateConnection() => new(connectionString);
}
