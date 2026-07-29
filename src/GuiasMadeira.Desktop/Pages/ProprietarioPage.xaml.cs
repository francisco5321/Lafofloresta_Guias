using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GuiasMadeira.Desktop.Services;
using GuiasMadeira.Domain.Entities;
using GuiasMadeira.Infrastructure.Postgres;

namespace GuiasMadeira.Desktop.Pages;

public partial class ProprietarioPage
{
    private int? editingId;
    private ICollectionView? listaView;

    public ProprietarioPage()
    {
        InitializeComponent();
        Loaded += ProprietarioPage_Loaded;
    }

    private async void ProprietarioPage_Loaded(object sender, RoutedEventArgs e) => await CarregarListaAsync();

    private async Task CarregarListaAsync()
    {
        try
        {
            var proprietarios = await AppServices.Proprietarios.ListAllAsync();
            listaView = CollectionViewSource.GetDefaultView(proprietarios);
            listaView.Filter = FiltrarLista;
            ListaGrid.ItemsSource = listaView;

            ListaEmptyState.Visibility = proprietarios.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ListaGrid.Visibility = proprietarios.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível ligar à base de dados.\n\n{ex.Message}",
                "Erro de ligação", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool FiltrarLista(object obj)
    {
        if (obj is not Proprietario proprietario)
        {
            return false;
        }

        var termo = PesquisaBox.Text?.Trim();
        if (string.IsNullOrEmpty(termo))
        {
            return true;
        }

        return Contem(proprietario.Nome, termo) || Contem(proprietario.Distrito, termo)
            || Contem(proprietario.Concelho, termo) || Contem(proprietario.Freguesia, termo)
            || Contem(proprietario.CodigoProp, termo) || Contem(proprietario.Parcela, termo);
    }

    private static bool Contem(string? valor, string termo) =>
        !string.IsNullOrEmpty(valor) && valor.Contains(termo, StringComparison.OrdinalIgnoreCase);

    private void Pesquisa_TextChanged(object sender, TextChangedEventArgs e) => listaView?.Refresh();

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
                ToastText.Text = "Proprietário atualizado";
            }
            else
            {
                await AppServices.Proprietarios.InsertAsync(proprietario);
                ToastText.Text = "Proprietário guardado";
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
            MessageBox.Show($"Ocorreu um erro ao guardar o proprietário.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            GuardarButton.IsEnabled = true;
        }
    }

    private void Editar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not Proprietario proprietario)
        {
            return;
        }

        editingId = proprietario.Id;
        NomeBox.Text = proprietario.Nome;
        DistritoBox.Text = proprietario.Distrito;
        ConcelhoBox.Text = proprietario.Concelho;
        FreguesiaBox.Text = proprietario.Freguesia;
        CodigoPropBox.Text = proprietario.CodigoProp;
        ParcelaBox.Text = proprietario.Parcela;
        LimparErros();

        FormTitleText.Text = "Editar proprietário";
        BreadcrumbCurrentText.Text = "Editar proprietário";
        GuardarButton.Content = "Guardar alterações";
        CancelarEdicaoButton.Visibility = Visibility.Visible;
        NomeBox.Focus();
    }

    private void CancelarEdicao_Click(object sender, RoutedEventArgs e) => EntrarModoCriacao();

    private void EntrarModoCriacao()
    {
        editingId = null;
        LimparCampos();
        FormTitleText.Text = "Novo proprietário";
        BreadcrumbCurrentText.Text = "Novo proprietário";
        GuardarButton.Content = "Guardar";
        CancelarEdicaoButton.Visibility = Visibility.Collapsed;
    }

    private async void Apagar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not Proprietario proprietario)
        {
            return;
        }

        var confirmar = MessageBox.Show(
            $"Tens a certeza que queres apagar \"{proprietario.Nome}\"?",
            "Confirmar eliminação", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmar != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await AppServices.Proprietarios.DeleteAsync(proprietario.Id);

            if (editingId == proprietario.Id)
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
            MessageBox.Show($"Ocorreu um erro ao apagar o proprietário.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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

    private void LimparCampos()
    {
        NomeBox.Clear();
        DistritoBox.Clear();
        ConcelhoBox.Clear();
        FreguesiaBox.Clear();
        CodigoPropBox.Clear();
        ParcelaBox.Clear();
        LimparErros();
    }
}
