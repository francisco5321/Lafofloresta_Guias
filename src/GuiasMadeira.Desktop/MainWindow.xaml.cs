using System.Windows;
using GuiasMadeira.Desktop.Pages;
using GuiasMadeira.Desktop.Services;

namespace GuiasMadeira.Desktop;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        WindowState = WindowState.Normal;
        SizeToScreen();
        NavGuia.IsChecked = true;
        Loaded += (_, _) => InvalidateVisual();
        Loaded += MainWindow_Loaded;

        AppNavigation.NavigateToSection = secao =>
        {
            switch (secao)
            {
                case "Destinatarios":
                    NavDestinatarios.IsChecked = true;
                    break;
                case "Proprietarios":
                    NavProprietarios.IsChecked = true;
                    break;
                case "Rolarias":
                    NavRolarias.IsChecked = true;
                    break;
                case "CodigosBarras":
                    NavImportarCodigosBarras.IsChecked = true;
                    break;
                case "Guias":
                    NavPesquisar.IsChecked = true;
                    break;
            }
        };

        AppNavigation.NavigateToPage = page => NavigateTo(page);
        AppNavigation.RefreshCounts = () => _ = RefreshCountsAsync();
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e) => await RefreshCountsAsync();

    private async Task RefreshCountsAsync()
    {
        try
        {
            DestinatariosCountText.Text = (await AppServices.Destinatarios.CountAsync()).ToString();
            ProprietariosCountText.Text = (await AppServices.Proprietarios.CountAsync()).ToString();
            RolariasCountText.Text = (await AppServices.Rolarias.CountAsync()).ToString();
        }
        catch
        {
            // Contadores são só um extra informativo; se a ligação falhar aqui,
            // o próprio ecrã de destino já mostra o erro de ligação de forma mais clara.
            DestinatariosCountText.Text = "–";
            ProprietariosCountText.Text = "–";
            RolariasCountText.Text = "–";
        }
    }

    private void SizeToScreen()
    {
        var workArea = SystemParameters.WorkArea;
        Width = Math.Clamp(workArea.Width * 0.8, MinWidth, 1280);
        Height = Math.Clamp(workArea.Height * 0.85, MinHeight, 900);
    }

    private void NavGuia_Checked(object sender, RoutedEventArgs e) => NavigateTo(new GuiaPage());

    private void NavDestinatarios_Checked(object sender, RoutedEventArgs e) => NavigateTo(new DestinatarioPage());

    private void NavProprietarios_Checked(object sender, RoutedEventArgs e) => NavigateTo(new ProprietarioPage());

    private void NavRolarias_Checked(object sender, RoutedEventArgs e) => NavigateTo(new RolariaPage());

    private void NavImportarCodigosBarras_Checked(object sender, RoutedEventArgs e) => NavigateTo(new ImportarCodigosBarrasPage());

    private void NavUgf_Checked(object sender, RoutedEventArgs e) => NavigateTo(new UgfPage());

    private void NavPesquisar_Checked(object sender, RoutedEventArgs e) => NavigateTo(new PesquisarGuiaPage());

    private void NavigateTo(System.Windows.Controls.Page page)
    {
        MainFrame.Navigate(page);
        while (MainFrame.NavigationService.CanGoBack)
        {
            MainFrame.NavigationService.RemoveBackEntry();
        }
    }

    private void Sair_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
