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
    private int? editingId;
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
                ToastText.Text = "Rolaria atualizada";
            }
            else
            {
                await AppServices.Rolarias.InsertAsync(rolaria);
                ToastText.Text = "Rolaria guardada";
            }

            ToastBorder.Visibility = Visibility.Visible;
            EntrarModoCriacao();
            TipoBox.Focus();
            await CarregarListaAsync();
            AppNavigation.RefreshCounts?.Invoke();
            await Task.Delay(1600);
            ToastBorder.Visibility = Visibility.Collapsed;
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

    private void Editar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not Rolaria rolaria)
        {
            return;
        }

        editingId = rolaria.Id;
        TipoBox.Text = rolaria.Tipo;
        TipoError.Visibility = Visibility.Collapsed;

        FormTitleText.Text = "Editar rolaria";
        BreadcrumbCurrentText.Text = "Editar rolaria";
        GuardarButton.Content = "Guardar alterações";
        CancelarEdicaoButton.Visibility = Visibility.Visible;
        TipoBox.Focus();
    }

    private void CancelarEdicao_Click(object sender, RoutedEventArgs e) => EntrarModoCriacao();

    private void EntrarModoCriacao()
    {
        editingId = null;
        TipoBox.Clear();
        TipoError.Visibility = Visibility.Collapsed;
        FormTitleText.Text = "Nova rolaria";
        BreadcrumbCurrentText.Text = "Nova rolaria";
        GuardarButton.Content = "Guardar";
        CancelarEdicaoButton.Visibility = Visibility.Collapsed;
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

            if (editingId == rolaria.Id)
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
            MessageBox.Show($"Ocorreu um erro ao apagar a rolaria.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
