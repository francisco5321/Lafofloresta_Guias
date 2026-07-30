using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Effects;
using GuiasMadeira.Desktop.Services;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Desktop.Pages;

public partial class PesquisarGuiaPage
{
    private ICollectionView? listaView;

    public PesquisarGuiaPage()
    {
        InitializeComponent();
        Loaded += PesquisarGuiaPage_Loaded;
    }

    private async void PesquisarGuiaPage_Loaded(object sender, RoutedEventArgs e) => await CarregarListaAsync();

    private async Task CarregarListaAsync()
    {
        try
        {
            var guias = await AppServices.Guias.ListResumoAsync();
            listaView = CollectionViewSource.GetDefaultView(guias);
            listaView.Filter = FiltrarLista;
            ListaGrid.ItemsSource = listaView;

            ListaEmptyState.Visibility = guias.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ListaGrid.Visibility = guias.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível ligar à base de dados.\n\n{ex.Message}",
                "Erro de ligação", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool FiltrarLista(object obj)
    {
        if (obj is not GuiaResumo guia)
        {
            return false;
        }

        var termo = PesquisaBox.Text?.Trim();
        if (string.IsNullOrEmpty(termo))
        {
            return true;
        }

        return guia.Id.ToString().Contains(termo, StringComparison.OrdinalIgnoreCase)
            || Contem(guia.DestinatarioNome, termo) || Contem(guia.DestinatarioNif, termo)
            || Contem(guia.ProprietarioNome, termo) || Contem(guia.Fornecedor, termo);
    }

    private static bool Contem(string? valor, string termo) =>
        !string.IsNullOrEmpty(valor) && valor.Contains(termo, StringComparison.OrdinalIgnoreCase);

    private void Pesquisa_TextChanged(object sender, TextChangedEventArgs e) => listaView?.Refresh();

    private async void PreVisualizar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not GuiaResumo guia)
        {
            return;
        }

        try
        {
            var encontrada = await GuiaPrintService.PreviewAsync(guia.Id);
            if (!encontrada)
            {
                MessageBox.Show("Guia não encontrada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao abrir a guia.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CriarGuia_Click(object sender, RoutedEventArgs e) =>
        await AbrirModalAsync(new GuiaModalWindow());

    private async void Editar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not GuiaResumo guia)
        {
            return;
        }

        await AbrirModalAsync(new GuiaModalWindow(guia));
    }

    /// <summary>
    /// Mostra o modal com a janela principal ligeiramente desfocada por baixo — realça o modal
    /// em vez de o deixar como mais uma janela solta sobreposta.
    /// </summary>
    private async Task AbrirModalAsync(Window modal)
    {
        var mainWindow = Window.GetWindow(this);
        var blur = new BlurEffect { Radius = 14 };
        if (mainWindow is not null)
        {
            mainWindow.Effect = blur;
        }

        try
        {
            modal.Owner = mainWindow;
            modal.ShowDialog();
        }
        finally
        {
            if (mainWindow is not null)
            {
                mainWindow.Effect = null;
            }
        }

        await CarregarListaAsync();
    }

    private async void Apagar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not GuiaResumo guia)
        {
            return;
        }

        var confirmar = MessageBox.Show(
            $"Tens a certeza que queres apagar a guia nº {guia.Id}? Esta ação não pode ser desfeita.",
            "Confirmar eliminação", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirmar != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await AppServices.Guias.DeleteAsync(guia.Id);
            await CarregarListaAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao apagar a guia.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
