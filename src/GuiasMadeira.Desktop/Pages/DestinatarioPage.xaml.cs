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

    private async void CriarDestinatario_Click(object sender, RoutedEventArgs e)
    {
        ModalHelper.ShowModal(this, new FormModalWindow(new DestinatarioFormPage(), "Novo destinatário"));
        await CarregarListaAsync();
        AppNavigation.RefreshCounts?.Invoke();
    }

    private async void Editar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not Destinatario destinatario)
        {
            return;
        }

        ModalHelper.ShowModal(this, new FormModalWindow(new DestinatarioFormPage(destinatario), "Editar destinatário"));
        await CarregarListaAsync();
        AppNavigation.RefreshCounts?.Invoke();
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
}
