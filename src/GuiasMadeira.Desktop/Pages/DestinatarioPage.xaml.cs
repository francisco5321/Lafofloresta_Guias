using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GuiasMadeira.Desktop.Services;
using GuiasMadeira.Domain.Entities;
using GuiasMadeira.Infrastructure.Postgres;

namespace GuiasMadeira.Desktop.Pages;

public partial class DestinatarioPage
{
    private int? editingId;
    private ICollectionView? listaView;

    public DestinatarioPage()
    {
        InitializeComponent();
        Loaded += DestinatarioPage_Loaded;
    }

    private async void DestinatarioPage_Loaded(object sender, RoutedEventArgs e) => await CarregarListaAsync();

    private async Task CarregarListaAsync()
    {
        try
        {
            var destinatarios = await AppServices.Destinatarios.ListAllAsync();
            listaView = CollectionViewSource.GetDefaultView(destinatarios);
            listaView.Filter = FiltrarLista;
            ListaGrid.ItemsSource = listaView;

            ListaEmptyState.Visibility = destinatarios.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ListaGrid.Visibility = destinatarios.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível ligar à base de dados.\n\n{ex.Message}",
                "Erro de ligação", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool FiltrarLista(object obj)
    {
        if (obj is not Destinatario destinatario)
        {
            return false;
        }

        var termo = PesquisaBox.Text?.Trim();
        if (string.IsNullOrEmpty(termo))
        {
            return true;
        }

        return Contem(destinatario.Nome, termo) || Contem(destinatario.Nif, termo)
            || Contem(destinatario.Morada, termo) || Contem(destinatario.Concelho, termo);
    }

    private static bool Contem(string? valor, string termo) =>
        !string.IsNullOrEmpty(valor) && valor.Contains(termo, StringComparison.OrdinalIgnoreCase);

    private void Pesquisa_TextChanged(object sender, TextChangedEventArgs e) => listaView?.Refresh();

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
                ToastText.Text = "Destinatário atualizado";
            }
            else
            {
                await AppServices.Destinatarios.InsertAsync(destinatario);
                ToastText.Text = "Destinatário guardado";
            }

            ToastBorder.Visibility = Visibility.Visible;
            EntrarModoCriacao();
            NomeBox.Focus();
            await CarregarListaAsync();
            AppNavigation.RefreshCounts?.Invoke();
            await Task.Delay(1600);
            ToastBorder.Visibility = Visibility.Collapsed;
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

    private void Editar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not Destinatario destinatario)
        {
            return;
        }

        editingId = destinatario.Id;
        NomeBox.Text = destinatario.Nome;
        NifBox.Text = destinatario.Nif;
        MoradaBox.Text = destinatario.Morada;
        ConcelhoBox.Text = destinatario.Concelho;
        LimparErros();

        FormTitleText.Text = "Editar destinatário";
        BreadcrumbCurrentText.Text = "Editar destinatário";
        GuardarButton.Content = "Guardar alterações";
        CancelarEdicaoButton.Visibility = Visibility.Visible;
        NomeBox.Focus();
    }

    private void CancelarEdicao_Click(object sender, RoutedEventArgs e) => EntrarModoCriacao();

    private void EntrarModoCriacao()
    {
        editingId = null;
        LimparCampos();
        FormTitleText.Text = "Novo destinatário";
        BreadcrumbCurrentText.Text = "Novo destinatário";
        GuardarButton.Content = "Guardar";
        CancelarEdicaoButton.Visibility = Visibility.Collapsed;
    }

    private async void Apagar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not Destinatario destinatario)
        {
            return;
        }

        var confirmar = MessageBox.Show(
            $"Tens a certeza que queres apagar \"{destinatario.Nome}\"?",
            "Confirmar eliminação", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmar != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await AppServices.Destinatarios.DeleteAsync(destinatario.Id);

            if (editingId == destinatario.Id)
            {
                EntrarModoCriacao();
            }

            await CarregarListaAsync();
            AppNavigation.RefreshCounts?.Invoke();
        }
        catch (RegistoEmUsoException ex)
        {
            MessageBox.Show(ex.Message, "Não é possível apagar", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao apagar o destinatário.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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

    private void LimparCampos()
    {
        NomeBox.Clear();
        NifBox.Clear();
        MoradaBox.Clear();
        ConcelhoBox.Clear();
        LimparErros();
    }
}
