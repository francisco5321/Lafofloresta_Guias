using System.Windows;
using GuiasMadeira.Desktop.Services;

namespace GuiasMadeira.Desktop.Pages;

public partial class PesquisarGuiaPage
{
    public PesquisarGuiaPage()
    {
        InitializeComponent();
        Loaded += PesquisarGuiaPage_Loaded;
    }

    private async void PesquisarGuiaPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            GuiaCombo.ItemsSource = await AppServices.Guias.ListAllIdsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível ligar à base de dados.\n\n{ex.Message}",
                "Erro de ligação", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Pesquisar_Click(object sender, RoutedEventArgs e)
    {
        if (GuiaCombo.SelectedItem is not int idGuia)
        {
            MessageBox.Show("Por favor, selecione uma guia na lista antes de continuar.",
                "Seleção obrigatória", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            GuiaCombo.Focus();
            return;
        }

        PesquisarButton.IsEnabled = false;
        try
        {
            var encontrada = await GuiaPrintService.PreviewAsync(idGuia);
            if (!encontrada)
            {
                MessageBox.Show("Guia não encontrada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao abrir a guia.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            PesquisarButton.IsEnabled = true;
        }
    }

}
