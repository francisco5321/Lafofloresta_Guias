using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace GuiasMadeira.Desktop;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .Build();

        var connectionString = configuration.GetConnectionString("GuiasDb")
            ?? throw new InvalidOperationException(
                "Falta a connection string 'GuiasDb' em appsettings.json.");

        // TODO: remover este fallback (e a chave "GuiasDbFallback" em appsettings.Development.json)
        // quando o servidor PostgreSQL de produção estiver disponível e configurado em "GuiasDb".
        var fallbackConnectionString = configuration.GetConnectionString("GuiasDbFallback");
        connectionString = ResolveConnectionString(connectionString, fallbackConnectionString);

        AppServices.Initialize(connectionString);

        base.OnStartup(e);
    }

    private static string ResolveConnectionString(string primary, string? fallback)
    {
        if (fallback is null)
        {
            return primary;
        }

        try
        {
            using var connection = new NpgsqlConnection(primary);
            connection.Open();
            return primary;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível ligar ao servidor principal ({ex.Message}).\nA usar base de dados local de teste.",
                "Fallback de base de dados",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return fallback;
        }
    }
}
