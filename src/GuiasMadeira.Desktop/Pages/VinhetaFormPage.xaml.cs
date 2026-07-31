using System.Windows;
using GuiasMadeira.Desktop.Services;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Desktop.Pages;

/// <summary>
/// Só edita uma vinheta já existente — a criação é sempre em massa (ImportarCodigosBarrasPage),
/// por isso não há construtor sem parâmetros.
/// </summary>
public partial class VinhetaFormPage
{
    private readonly int id;

    public VinhetaFormPage(CodigoBarra codigoBarraParaEditar)
    {
        InitializeComponent();
        id = codigoBarraParaEditar.Id;
        CodigoBox.Text = codigoBarraParaEditar.Codigo;
        NumeroCertificadoBox.Text = codigoBarraParaEditar.NumeroCertificado;
        NumeroUgfBox.Text = codigoBarraParaEditar.NumeroUgf;
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        CodigoError.Visibility = Visibility.Collapsed;
        if (string.IsNullOrWhiteSpace(CodigoBox.Text))
        {
            CodigoError.Text = "Escreve o código de barras";
            CodigoError.Visibility = Visibility.Visible;
            CodigoBox.Focus();
            return;
        }

        var codigoBarra = new CodigoBarra
        {
            Id = id,
            Codigo = CodigoBox.Text.Trim(),
            NumeroCertificado = string.IsNullOrWhiteSpace(NumeroCertificadoBox.Text) ? null : NumeroCertificadoBox.Text.Trim(),
            NumeroUgf = string.IsNullOrWhiteSpace(NumeroUgfBox.Text) ? null : NumeroUgfBox.Text.Trim()
        };

        GuardarButton.IsEnabled = false;
        try
        {
            await AppServices.CodigosBarras.UpdateAsync(codigoBarra);
            Window.GetWindow(this)?.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao guardar a vinheta.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            GuardarButton.IsEnabled = true;
        }
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => Window.GetWindow(this)?.Close();
}
