using System.Windows;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Desktop.Pages;

public partial class ProprietarioPage
{
    public ProprietarioPage()
    {
        InitializeComponent();
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (!Validar(out var proprietario))
        {
            return;
        }

        GuardarButton.IsEnabled = false;
        try
        {
            await AppServices.Proprietarios.InsertAsync(proprietario);
            MessageBox.Show("Proprietário guardado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            LimparCampos();
            NomeBox.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao guardar o proprietário.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            GuardarButton.IsEnabled = true;
        }
    }

    private bool Validar(out Proprietario proprietario)
    {
        proprietario = new Proprietario();

        if (string.IsNullOrWhiteSpace(NomeBox.Text))
        {
            AvisarCampoObrigatorio("o Nome do Proprietário");
            NomeBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(DistritoBox.Text))
        {
            AvisarCampoObrigatorio("o Distrito");
            DistritoBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(ConcelhoBox.Text))
        {
            AvisarCampoObrigatorio("o Concelho");
            ConcelhoBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(FreguesiaBox.Text))
        {
            AvisarCampoObrigatorio("a Freguesia");
            FreguesiaBox.Focus();
            return false;
        }

        proprietario.Nome = NomeBox.Text.Trim();
        proprietario.Distrito = DistritoBox.Text.Trim();
        proprietario.Concelho = ConcelhoBox.Text.Trim();
        proprietario.Freguesia = FreguesiaBox.Text.Trim();
        proprietario.CodigoProp = string.IsNullOrWhiteSpace(CodigoPropBox.Text) ? null : CodigoPropBox.Text.Trim();
        proprietario.Parcela = string.IsNullOrWhiteSpace(ParcelaBox.Text) ? null : ParcelaBox.Text.Trim();
        return true;
    }

    private void LimparCampos()
    {
        NomeBox.Clear();
        DistritoBox.Clear();
        ConcelhoBox.Clear();
        FreguesiaBox.Clear();
        CodigoPropBox.Clear();
        ParcelaBox.Clear();
    }

    private static void AvisarCampoObrigatorio(string campo) =>
        MessageBox.Show($"Introduza {campo}.", "Campo obrigatório", MessageBoxButton.OK, MessageBoxImage.Exclamation);
}
