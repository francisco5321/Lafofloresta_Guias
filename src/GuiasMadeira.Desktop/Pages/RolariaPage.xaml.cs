using System.Windows;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Desktop.Pages;

public partial class RolariaPage
{
    public RolariaPage()
    {
        InitializeComponent();
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TipoBox.Text))
        {
            MessageBox.Show("Introduza o Tipo de Rolaria.", "Campo obrigatório", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            TipoBox.Focus();
            return;
        }

        var rolaria = new Rolaria { Tipo = TipoBox.Text.Trim() };

        GuardarButton.IsEnabled = false;
        try
        {
            await AppServices.Rolarias.InsertAsync(rolaria);
            MessageBox.Show("Rolaria guardada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            TipoBox.Clear();
            TipoBox.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao guardar a rolaria.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            GuardarButton.IsEnabled = true;
        }
    }

}
