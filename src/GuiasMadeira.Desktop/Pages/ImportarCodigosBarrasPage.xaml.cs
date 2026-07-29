using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GuiasMadeira.Desktop.Services;
using GuiasMadeira.Domain.Entities;
using GuiasMadeira.Infrastructure.Postgres;
using Microsoft.Win32;

namespace GuiasMadeira.Desktop.Pages;

public partial class ImportarCodigosBarrasPage
{
    private sealed class LinhaImportacao
    {
        public int Linha { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string? NumeroCertificado { get; init; }
        public string? NumeroUgf { get; init; }
        public string Estado { get; init; } = string.Empty;
        public bool Novo { get; init; }
    }

    private readonly List<LinhaImportacao> linhas = new();
    private int? editingVinhetaId;
    private ICollectionView? vinhetasView;

    public ImportarCodigosBarrasPage()
    {
        InitializeComponent();
        Loaded += ImportarCodigosBarrasPage_Loaded;
    }

    private async void ImportarCodigosBarrasPage_Loaded(object sender, RoutedEventArgs e) => await CarregarVinhetasAsync();

    private async Task CarregarVinhetasAsync()
    {
        try
        {
            var vinhetas = await AppServices.CodigosBarras.ListAllAsync();
            vinhetasView = CollectionViewSource.GetDefaultView(vinhetas);
            vinhetasView.Filter = FiltrarVinhetas;
            VinhetaListaGrid.ItemsSource = vinhetasView;

            VinhetaListaEmptyState.Visibility = vinhetas.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            VinhetaListaGrid.Visibility = vinhetas.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível ligar à base de dados.\n\n{ex.Message}",
                "Erro de ligação", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool FiltrarVinhetas(object obj)
    {
        if (obj is not CodigoBarra codigoBarra)
        {
            return false;
        }

        var termo = VinhetaPesquisaBox.Text?.Trim();
        if (string.IsNullOrEmpty(termo))
        {
            return true;
        }

        return Contem(codigoBarra.Codigo, termo) || Contem(codigoBarra.NumeroCertificado, termo)
            || Contem(codigoBarra.NumeroUgf, termo);
    }

    private static bool Contem(string? valor, string termo) =>
        !string.IsNullOrEmpty(valor) && valor.Contains(termo, StringComparison.OrdinalIgnoreCase);

    private void VinhetaPesquisa_TextChanged(object sender, TextChangedEventArgs e) => vinhetasView?.Refresh();

    private void VinhetaEditar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not CodigoBarra codigoBarra)
        {
            return;
        }

        editingVinhetaId = codigoBarra.Id;
        VinhetaCodigoBox.Text = codigoBarra.Codigo;
        VinhetaNumeroCertificadoBox.Text = codigoBarra.NumeroCertificado;
        VinhetaNumeroUgfBox.Text = codigoBarra.NumeroUgf;
        VinhetaCodigoError.Visibility = Visibility.Collapsed;

        VinhetaAjudaText.Text = $"A editar a vinheta \"{codigoBarra.Codigo}\".";
        VinhetaCodigoBox.IsEnabled = true;
        VinhetaNumeroCertificadoBox.IsEnabled = true;
        VinhetaNumeroUgfBox.IsEnabled = true;
        VinhetaGuardarButton.IsEnabled = true;
        VinhetaCancelarButton.Visibility = Visibility.Visible;
        VinhetaCodigoBox.Focus();
    }

    private void VinhetaCancelar_Click(object sender, RoutedEventArgs e) => SairModoEdicaoVinheta();

    private void SairModoEdicaoVinheta()
    {
        editingVinhetaId = null;
        VinhetaCodigoBox.Clear();
        VinhetaNumeroCertificadoBox.Clear();
        VinhetaNumeroUgfBox.Clear();
        VinhetaCodigoError.Visibility = Visibility.Collapsed;

        VinhetaAjudaText.Text = "Escolhe uma vinheta na lista ao lado para editar ou apagar.";
        VinhetaCodigoBox.IsEnabled = false;
        VinhetaNumeroCertificadoBox.IsEnabled = false;
        VinhetaNumeroUgfBox.IsEnabled = false;
        VinhetaGuardarButton.IsEnabled = false;
        VinhetaCancelarButton.Visibility = Visibility.Collapsed;
    }

    private async void VinhetaGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (editingVinhetaId is not int id)
        {
            return;
        }

        VinhetaCodigoError.Visibility = Visibility.Collapsed;
        if (string.IsNullOrWhiteSpace(VinhetaCodigoBox.Text))
        {
            VinhetaCodigoError.Text = "Escreve o código de barras";
            VinhetaCodigoError.Visibility = Visibility.Visible;
            VinhetaCodigoBox.Focus();
            return;
        }

        var codigoBarra = new CodigoBarra
        {
            Id = id,
            Codigo = VinhetaCodigoBox.Text.Trim(),
            NumeroCertificado = string.IsNullOrWhiteSpace(VinhetaNumeroCertificadoBox.Text) ? null : VinhetaNumeroCertificadoBox.Text.Trim(),
            NumeroUgf = string.IsNullOrWhiteSpace(VinhetaNumeroUgfBox.Text) ? null : VinhetaNumeroUgfBox.Text.Trim()
        };

        VinhetaGuardarButton.IsEnabled = false;
        try
        {
            await AppServices.CodigosBarras.UpdateAsync(codigoBarra);

            VinhetaToastText.Text = "Vinheta atualizada";
            VinhetaToastBorder.Visibility = Visibility.Visible;
            SairModoEdicaoVinheta();
            await CarregarVinhetasAsync();
            await Task.Delay(1600);
            VinhetaToastBorder.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao guardar a vinheta.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            VinhetaGuardarButton.IsEnabled = true;
        }
    }

    private async void VinhetaApagar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not CodigoBarra codigoBarra)
        {
            return;
        }

        var confirmar = MessageBox.Show(
            $"Tens a certeza que queres apagar a vinheta \"{codigoBarra.Codigo}\"?",
            "Confirmar eliminação", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmar != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await AppServices.CodigosBarras.DeleteAsync(codigoBarra.Id);

            if (editingVinhetaId == codigoBarra.Id)
            {
                SairModoEdicaoVinheta();
            }

            await CarregarVinhetasAsync();
        }
        catch (RegistoEmUsoException ex)
        {
            MessageBox.Show(ex.Message, "Não é possível apagar", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao apagar a vinheta.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SelecionarFicheiro_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Selecionar ficheiro de vinhetas",
            Filter = "Ficheiros Excel (*.xlsx)|*.xlsx",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        FicheiroText.Text = dialog.FileName;
        _ = CarregarPreviewAsync(dialog.FileName);
    }

    private async Task CarregarPreviewAsync(string caminhoFicheiro)
    {
        ImportarButton.IsEnabled = false;
        ResumoText.Text = "A ler ficheiro...";
        PreviewGrid.ItemsSource = null;
        linhas.Clear();

        try
        {
            var lidas = await Task.Run(() => CodigoBarraExcelImporter.Ler(caminhoFicheiro));
            if (lidas.Count == 0)
            {
                ResumoText.Text = "Não foram encontradas linhas válidas no ficheiro.";
                return;
            }

            var existentes = await AppServices.CodigosBarras.ListCodigosExistentesAsync();
            var codigosNoFicheiro = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var linha in lidas)
            {
                var duplicadoNaBd = existentes.Contains(linha.Codigo);
                var duplicadoNoFicheiro = !codigosNoFicheiro.Add(linha.Codigo);
                var novo = !duplicadoNaBd && !duplicadoNoFicheiro;

                linhas.Add(new LinhaImportacao
                {
                    Linha = linha.Linha,
                    Codigo = linha.Codigo,
                    NumeroCertificado = linha.NumeroCertificado,
                    NumeroUgf = linha.NumeroUgf,
                    Estado = duplicadoNaBd ? "Já existe na base de dados" : duplicadoNoFicheiro ? "Repetido no ficheiro" : "Novo",
                    Novo = novo
                });
            }

            PreviewGrid.ItemsSource = linhas;

            var totalNovos = linhas.Count(l => l.Novo);
            var totalIgnorados = linhas.Count - totalNovos;
            ResumoText.Text = $"{lidas.Count} linha(s) lida(s) — {totalNovos} novo(s), {totalIgnorados} ignorado(s) por serem duplicado(s).";
            ImportarButton.IsEnabled = totalNovos > 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível ler o ficheiro selecionado.\n\n{ex.Message}",
                "Erro ao ler ficheiro", MessageBoxButton.OK, MessageBoxImage.Error);
            ResumoText.Text = string.Empty;
        }
    }

    private async void Importar_Click(object sender, RoutedEventArgs e)
    {
        var novos = linhas.Where(l => l.Novo)
            .Select(l => new CodigoBarra { Codigo = l.Codigo, NumeroCertificado = l.NumeroCertificado, NumeroUgf = l.NumeroUgf })
            .ToList();

        if (novos.Count == 0)
        {
            return;
        }

        ImportarButton.IsEnabled = false;
        SelecionarFicheiroButton.IsEnabled = false;
        try
        {
            var inseridos = await AppServices.CodigosBarras.InsertManyAsync(novos);

            ToastText.Text = inseridos == 1 ? "1 código importado" : $"{inseridos} códigos importados";
            ToastBorder.Visibility = Visibility.Visible;

            FicheiroText.Text = "Nenhum ficheiro selecionado.";
            ResumoText.Text = string.Empty;
            PreviewGrid.ItemsSource = null;
            linhas.Clear();
            await CarregarVinhetasAsync();

            await Task.Delay(1600);
            ToastBorder.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao importar os códigos de barras.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            ImportarButton.IsEnabled = true;
        }
        finally
        {
            SelecionarFicheiroButton.IsEnabled = true;
        }
    }
}
