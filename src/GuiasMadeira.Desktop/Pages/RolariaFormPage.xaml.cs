using System.Windows;
using GuiasMadeira.Desktop.Services;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Desktop.Pages;

public partial class RolariaFormPage
{
    private const int TotalCampos = 1;

    private int? editingId;

    public RolariaFormPage()
    {
        InitializeComponent();
        AtualizarProgresso();
    }

    public RolariaFormPage(Rolaria rolariaParaEditar) : this()
    {
        editingId = rolariaParaEditar.Id;
        TipoBox.Text = rolariaParaEditar.Tipo;
        ToastText.Text = "Rolaria atualizada";
        GuardarButton.Content = "Guardar alterações";
        CancelarEdicaoButton.Visibility = Visibility.Visible;
        AtualizarProgresso();
    }

    private void Campo_Changed(object sender, RoutedEventArgs e) => AtualizarProgresso();

    private void AtualizarProgresso()
    {
        var preenchidos = string.IsNullOrWhiteSpace(TipoBox.Text) ? 0 : 1;

        ProgressFillColumn.Width = new GridLength(preenchidos, GridUnitType.Star);
        ProgressRemainderColumn.Width = new GridLength(TotalCampos - preenchidos, GridUnitType.Star);
        ProgressText.Text = $"{preenchidos} de {TotalCampos} campo preenchido";
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        TipoError.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(TipoBox.Text))
        {
            TipoError.Text = "Escreve o tipo de rolaria";
            TipoError.Visibility = Visibility.Visible;
            TipoBox.Focus();
            return;
        }

        var rolaria = new Rolaria { Tipo = TipoBox.Text.Trim() };

        GuardarButton.IsEnabled = false;
        try
        {
            if (editingId is int id)
            {
                rolaria.Id = id;
                await AppServices.Rolarias.UpdateAsync(rolaria);
            }
            else
            {
                await AppServices.Rolarias.InsertAsync(rolaria);
            }

            Window.GetWindow(this)?.Close();
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

    private void Cancelar_Click(object sender, RoutedEventArgs e) => Window.GetWindow(this)?.Close();
}
