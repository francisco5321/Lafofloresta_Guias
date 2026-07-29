namespace GuiasMadeira.Infrastructure.Postgres;

/// <summary>
/// Lançada quando se tenta apagar um registo que ainda está referenciado por guias existentes
/// (o Postgres recusa o DELETE por causa do ON DELETE RESTRICT definido no schema).
/// </summary>
public sealed class RegistoEmUsoException : Exception
{
    public RegistoEmUsoException(string message) : base(message)
    {
    }
}
