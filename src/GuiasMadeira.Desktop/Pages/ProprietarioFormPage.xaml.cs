using System.Windows;
using System.Windows.Controls;
using GuiasMadeira.Desktop.Services;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Desktop.Pages;

public partial class ProprietarioFormPage
{
    private const int TotalCampos = 6;

    private int? editingId;

    public ProprietarioFormPage()
    {
        InitializeComponent();
        AtualizarProgresso();
    }

    public ProprietarioFormPage(Proprietario proprietarioParaEditar) : this()
    {
        editingId = proprietarioParaEditar.Id;
        NomeBox.Text = proprietarioParaEditar.Nome;
        DistritoBox.Text = proprietarioParaEditar.Distrito;
        ConcelhoBox.Text = proprietarioParaEditar.Concelho;
        FreguesiaBox.Text = proprietarioParaEditar.Freguesia;
        CodigoPropBox.Text = proprietarioParaEditar.CodigoProp;
        ParcelaBox.Text = proprietarioParaEditar.Parcela;
        ToastText.Text = "Proprietário atualizado";
        GuardarButton.Content = "Guardar alterações";
        CancelarEdicaoButton.Visibility = Visibility.Visible;
        AtualizarProgresso();
    }

    private void Campo_Changed(object sender, RoutedEventArgs e) => AtualizarProgresso();

    private void AtualizarProgresso()
    {
        var preenchidos = 0;
        if (!string.IsNullOrWhiteSpace(NomeBox.Text)) preenchidos++;
        if (!string.IsNullOrWhiteSpace(DistritoBox.Text)) preenchidos++;
        if (!string.IsNullOrWhiteSpace(ConcelhoBox.Text)) preenchidos++;
        if (!string.IsNullOrWhiteSpace(FreguesiaBox.Text)) preenchidos++;
        if (!string.IsNullOrWhiteSpace(CodigoPropBox.Text)) preenchidos++;
        if (!string.IsNullOrWhiteSpace(ParcelaBox.Text)) preenchidos++;

        ProgressFillColumn.Width = new GridLength(preenchidos, GridUnitType.Star);
        ProgressRemainderColumn.Width = new GridLength(TotalCampos - preenchidos, GridUnitType.Star);
        ProgressText.Text = $"{preenchidos} de {TotalCampos} campos preenchidos";
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
            if (editingId is int id)
            {
                proprietario.Id = id;
                await AppServices.Proprietarios.UpdateAsync(proprietario);
            }
            else
            {
                await AppServices.Proprietarios.InsertAsync(proprietario);
            }

            Window.GetWindow(this)?.Close();
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

    private void Cancelar_Click(object sender, RoutedEventArgs e) => Window.GetWindow(this)?.Close();

    private bool Validar(out Proprietario proprietario)
    {
        LimparErros();
        proprietario = new Proprietario();
        var valido = true;

        if (string.IsNullOrWhiteSpace(NomeBox.Text))
        {
            MostrarErro(NomeError, NomeBox, "Escreve o nome do proprietário");
            valido = false;
        }

        if (string.IsNullOrWhiteSpace(DistritoBox.Text))
        {
            MostrarErro(DistritoError, valido ? DistritoBox : null, "Escreve o distrito");
            valido = false;
        }

        if (string.IsNullOrWhiteSpace(ConcelhoBox.Text))
        {
            MostrarErro(ConcelhoError, valido ? ConcelhoBox : null, "Escreve o concelho");
            valido = false;
        }

        if (string.IsNullOrWhiteSpace(FreguesiaBox.Text))
        {
            MostrarErro(FreguesiaError, valido ? FreguesiaBox : null, "Escreve a freguesia");
            valido = false;
        }

        if (!valido)
        {
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

    private static void MostrarErro(TextBlock erro, TextBox? campoParaFoco, string mensagem)
    {
        erro.Text = mensagem;
        erro.Visibility = Visibility.Visible;
        campoParaFoco?.Focus();
    }

    private void LimparErros()
    {
        foreach (var erro in new[] { NomeError, DistritoError, ConcelhoError, FreguesiaError })
        {
            erro.Visibility = Visibility.Collapsed;
        }
    }
}
