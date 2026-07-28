using System.Windows;
using GuiasMadeira.Desktop.Pages;

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
