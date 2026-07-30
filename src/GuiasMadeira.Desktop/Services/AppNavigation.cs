namespace GuiasMadeira.Desktop.Services;

/// <summary>
/// Ponte simples para uma página (ex. um estado vazio) pedir à MainWindow para mudar
/// de secção, mantendo o item ativo da barra lateral sincronizado com o conteúdo.
/// </summary>
public static class AppNavigation
{
    public static Action<string>? NavigateToSection { get; set; }

    /// <summary>
    /// Atualiza os contadores da barra lateral depois de uma criação/edição/eliminação.
    /// </summary>
    public static Action? RefreshCounts { get; set; }
}
