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
            || Contem(proprietario.Concelho, termo) || Contem(proprietario.Freguesia, termo);
    }

    private static bool Contem(string? valor, string termo) =>
        !string.IsNullOrEmpty(valor) && valor.Contains(termo, StringComparison.OrdinalIgnoreCase);

    private void Pesquisa_TextChanged(object sender, TextChangedEventArgs e) => listaView?.Refresh();

    private async void CriarProprietario_Click(object sender, RoutedEventArgs e)
    {
        ModalHelper.ShowModal(this, new FormModalWindow(new ProprietarioFormPage(), "Novo proprietário"));
        await CarregarListaAsync();
        AppNavigation.RefreshCounts?.Invoke();
    }

    private async void Editar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not Proprietario proprietario)
        {
            return;
        }

        ModalHelper.ShowModal(this, new FormModalWindow(new ProprietarioFormPage(proprietario), "Editar proprietário"));
        await CarregarListaAsync();
        AppNavigation.RefreshCounts?.Invoke();
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
}
