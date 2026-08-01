using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GuiasMadeira.Desktop.Services;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Desktop.Pages;

public partial class DestinatarioFormPage
{
    private const int TotalCampos = 4;

    private int? editingId;

    public DestinatarioFormPage()
    {
        InitializeComponent();
        AtualizarProgresso();
    }

    public DestinatarioFormPage(Destinatario destinatarioParaEditar) : this()
    {
        editingId = destinatarioParaEditar.Id;
        NomeBox.Text = destinatarioParaEditar.Nome;
        NifBox.Text = destinatarioParaEditar.Nif;
        MoradaBox.Text = destinatarioParaEditar.Morada;
        ConcelhoBox.Text = destinatarioParaEditar.Concelho;
        ToastText.Text = "Destinatário atualizado";
        GuardarButton.Content = "Guardar alterações";
        CancelarEdicaoButton.Visibility = Visibility.Visible;
        AtualizarProgresso();
    }

    private void Campo_Changed(object sender, RoutedEventArgs e)
    {
        NomeBox.Tag = null;
        NifBox.Tag = null;
        MoradaBox.Tag = null;
        ConcelhoBox.Tag = null;
        AtualizarProgresso();
    }

    private static readonly Regex NaoDigitos = new(@"[^\d]", RegexOptions.Compiled);

    private void NifBox_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = NaoDigitos.IsMatch(e.Text);

    private void NifBox_PreviewKeyDown(object sender, KeyEventArgs e) =>
        e.Handled = e.Key is Key.Space;

    private void NifBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text) &&
            e.DataObject.GetData(DataFormats.Text) is string texto && !NaoDigitos.IsMatch(texto))
        {
            return;
        }

        e.CancelCommand();
    }

    private void AtualizarProgresso()
    {
        var preenchidos = 0;
        if (!string.IsNullOrWhiteSpace(NomeBox.Text)) preenchidos++;
        if (!string.IsNullOrWhiteSpace(NifBox.Text)) preenchidos++;
        if (!string.IsNullOrWhiteSpace(MoradaBox.Text)) preenchidos++;
        if (!string.IsNullOrWhiteSpace(ConcelhoBox.Text)) preenchidos++;

        ProgressFillColumn.Width = new GridLength(preenchidos, GridUnitType.Star);
        ProgressRemainderColumn.Width = new GridLength(TotalCampos - preenchidos, GridUnitType.Star);
        ProgressText.Text = $"{preenchidos} de {TotalCampos} campos preenchidos";
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
            if (editingId is int id)
            {
                destinatario.Id = id;
                await AppServices.Destinatarios.UpdateAsync(destinatario);
            }
            else
            {
                await AppServices.Destinatarios.InsertAsync(destinatario);
            }

            NomeBox.Tag = "Success";
            NifBox.Tag = "Success";
            MoradaBox.Tag = "Success";
            ConcelhoBox.Tag = "Success";

            ToastBorder.Visibility = Visibility.Visible;
            await Task.Delay(900);
            Window.GetWindow(this)?.Close();
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

    private void Cancelar_Click(object sender, RoutedEventArgs e) => Window.GetWindow(this)?.Close();

    private bool Validar(out Destinatario destinatario)
    {
        LimparErros();
        destinatario = new Destinatario();
        var valido = true;

        if (string.IsNullOrWhiteSpace(NomeBox.Text))
        {
            MostrarErro(NomeError, NomeBox, "Escreve o nome do destinatário");
            valido = false;
        }

        if (string.IsNullOrWhiteSpace(NifBox.Text))
        {
            MostrarErro(NifError, valido ? NifBox : null, "Escreve o NIF");
            valido = false;
        }
        else if (NifBox.Text.Trim().Length != 9)
        {
            MostrarErro(NifError, valido ? NifBox : null, "O NIF deve ter 9 números");
            valido = false;
        }

        if (string.IsNullOrWhiteSpace(MoradaBox.Text))
        {
            MostrarErro(MoradaError, valido ? MoradaBox : null, "Escreve a morada");
            valido = false;
        }

        if (string.IsNullOrWhiteSpace(ConcelhoBox.Text))
        {
            MostrarErro(ConcelhoError, valido ? ConcelhoBox : null, "Escreve o concelho");
            valido = false;
        }

        if (!valido)
        {
            return false;
        }

        destinatario.Nome = NomeBox.Text.Trim();
        destinatario.Nif = NifBox.Text.Trim();
        destinatario.Morada = MoradaBox.Text.Trim();
        destinatario.Concelho = ConcelhoBox.Text.Trim();
        return true;
    }

    private static void MostrarErro(TextBlock erro, TextBox? campoParaFoco, string mensagem)
    {
        erro.Text = mensagem;
        erro.Visibility = Visibility.Visible;
        campoParaFoco?.Focus();
    }

    private void LimparErros()
    {
        foreach (var erro in new[] { NomeError, NifError, MoradaError, ConcelhoError })
        {
            erro.Visibility = Visibility.Collapsed;
        }
    }
}
