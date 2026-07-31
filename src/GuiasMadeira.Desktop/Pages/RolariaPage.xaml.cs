using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GuiasMadeira.Desktop.Services;
using GuiasMadeira.Domain.Entities;
using GuiasMadeira.Infrastructure.Postgres;

namespace GuiasMadeira.Desktop.Pages;

public partial class RolariaPage
{
    private ICollectionView? listaView;

    public RolariaPage()
    {
        InitializeComponent();
        Loaded += RolariaPage_Loaded;
    }

    private async void RolariaPage_Loaded(object sender, RoutedEventArgs e) => await CarregarListaAsync();

    private async Task CarregarListaAsync()
    {
        try
        {
            var rolarias = await AppServices.Rolarias.ListAllAsync();
            listaView = CollectionViewSource.GetDefaultView(rolarias);
            listaView.Filter = FiltrarLista;
            ListaGrid.ItemsSource = listaView;

            ListaEmptyState.Visibility = rolarias.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ListaGrid.Visibility = rolarias.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível ligar à base de dados.\n\n{ex.Message}",
                "Erro de ligação", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool FiltrarLista(object obj)
    {
        if (obj is not Rolaria rolaria)
        {
            return false;
        }

        var termo = PesquisaBox.Text?.Trim();
        return string.IsNullOrEmpty(termo) || rolaria.Tipo.Contains(termo, StringComparison.OrdinalIgnoreCase);
    }

    private void Pesquisa_TextChanged(object sender, TextChangedEventArgs e) => listaView?.Refresh();

    private async void CriarRolaria_Click(object sender, RoutedEventArgs e)
    {
        ModalHelper.ShowModal(this, new FormModalWindow(new RolariaFormPage(), "Nova rolaria"));
        await CarregarListaAsync();
        AppNavigation.RefreshCounts?.Invoke();
    }

    private async void Editar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not Rolaria rolaria)
        {
            return;
        }

        ModalHelper.ShowModal(this, new FormModalWindow(new RolariaFormPage(rolaria), "Editar rolaria"));
        await CarregarListaAsync();
        AppNavigation.RefreshCounts?.Invoke();
    }

    private async void Apagar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not Rolaria rolaria)
        {
            return;
        }

        var confirmar = MessageBox.Show(
            $"Tens a certeza que queres apagar \"{rolaria.Tipo}\"?",
            "Confirmar eliminação", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmar != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await AppServices.Rolarias.DeleteAsync(rolaria.Id);
            await CarregarListaAsync();
            AppNavigation.RefreshCounts?.Invoke();
        }
        catch (RegistoEmUsoException ex)
        {
            MessageBox.Show(ex.Message, "Não é possível apagar", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao apagar a rolaria.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
