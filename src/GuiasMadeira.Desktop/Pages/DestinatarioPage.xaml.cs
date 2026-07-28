using System.Windows;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Desktop.Pages;

public partial class DestinatarioPage
{
    public DestinatarioPage()
    {
        InitializeComponent();
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (!Validar(out var destinatario))
        {
            return;
        }

        GuardarButton.IsEnabled = false;
        try
        {
            await AppServices.Destinatarios.InsertAsync(destinatario);
            MessageBox.Show("Destinatário guardado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            LimparCampos();
            NomeBox.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao guardar o destinatário.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            GuardarButton.IsEnabled = true;
        }
    }

    private bool Validar(out Destinatario destinatario)
    {
        destinatario = new Destinatario();

        if (string.IsNullOrWhiteSpace(NomeBox.Text))
        {
            AvisarCampoObrigatorio("o Nome do Destinatário");
            NomeBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(NifBox.Text))
        {
            AvisarCampoObrigatorio("o NIF");
            NifBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(MoradaBox.Text))
        {
            AvisarCampoObrigatorio("a Morada");
            MoradaBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(ConcelhoBox.Text))
        {
            AvisarCampoObrigatorio("o Concelho");
            ConcelhoBox.Focus();
            return false;
        }

        destinatario.Nome = NomeBox.Text.Trim();
        destinatario.Nif = NifBox.Text.Trim();
        destinatario.Morada = MoradaBox.Text.Trim();
        destinatario.Concelho = ConcelhoBox.Text.Trim();
        return true;
    }

    private void LimparCampos()
    {
        NomeBox.Clear();
        NifBox.Clear();
        MoradaBox.Clear();
        ConcelhoBox.Clear();
    }

    private static void AvisarCampoObrigatorio(string campo) =>
        MessageBox.Show($"Introduza {campo}.", "Campo obrigatório", MessageBoxButton.OK, MessageBoxImage.Exclamation);
}
