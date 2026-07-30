using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using GuiasMadeira.Desktop.Converters;
using GuiasMadeira.Desktop.Services;
using GuiasMadeira.Domain.Entities;
using Microsoft.Win32;
using Npgsql;

namespace GuiasMadeira.Desktop.Pages;

public partial class UgfPage
{
    private static readonly EstadoUgfCorConverter EstadoCorConverter = new();

    private int? editingId;
    private decimal importadasAtual;
    private bool isCarregandoSelecao;
    private List<UgfResumo> ugfsCarregados = new();
    private ICollectionView? listaView;

    public UgfPage()
    {
        InitializeComponent();
        Loaded += UgfPage_Loaded;
    }

    private async void UgfPage_Loaded(object sender, RoutedEventArgs e) => await CarregarListaAsync();

    private async Task CarregarListaAsync()
    {
        try
        {
            ugfsCarregados = (await AppServices.Ugfs.ListResumoAsync()).ToList();

            listaView = CollectionViewSource.GetDefaultView(ugfsCarregados);
            listaView.Filter = FiltrarLista;
            ListaGrid.ItemsSource = listaView;

            ListaEmptyState.Visibility = ugfsCarregados.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ListaGrid.Visibility = ugfsCarregados.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            // A combobox mostra tanto os UGFs já com limite definido como os códigos que já
            // aparecem em vinhetas mas ainda não têm limite — para se poder escolher um destes
            // últimos e criar logo o respetivo limite, sem ter de escrever o código à mão.
            var codigosVinhetas = await AppServices.CodigosBarras.ListNumerosUgfExistentesAsync();
            CodigoCombo.ItemsSource = ugfsCarregados.Select(u => u.Codigo)
                .Union(codigosVinhetas, StringComparer.OrdinalIgnoreCase)
                .OrderBy(codigo => codigo, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (editingId is int id)
            {
                var atual = ugfsCarregados.FirstOrDefault(u => u.Id == id);
                if (atual is not null)
                {
                    importadasAtual = atual.ToneladasImportadas;
                    AtualizarPreview();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível ligar à base de dados.\n\n{ex.Message}",
                "Erro de ligação", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool FiltrarLista(object obj) =>
        obj is not UgfResumo ugf ||
        string.IsNullOrEmpty(PesquisaBox.Text) ||
        ugf.Codigo.Contains(PesquisaBox.Text.Trim(), StringComparison.OrdinalIgnoreCase);

    private void Pesquisa_TextChanged(object sender, TextChangedEventArgs e) => listaView?.Refresh();

    private void CodigoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isCarregandoSelecao || CodigoCombo.SelectedItem is not string codigo)
        {
            return;
        }

        var correspondente = ugfsCarregados.FirstOrDefault(u => string.Equals(u.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
        if (correspondente is not null)
        {
            CarregarParaEdicao(correspondente);
        }
        else
        {
            // Código já usado numa vinheta mas ainda sem limite definido: prepara o formulário
            // para criar um UGF novo com este código, sem apagar o que já está selecionado.
            editingId = null;
            importadasAtual = 0m;
            CertificadoBox.Clear();
            LimparErros();
            AtualizarPreview();

            FormTitleText.Text = "Novo UGF";
            BreadcrumbCurrentText.Text = "Novo UGF";
            GuardarButton.Content = "Guardar";
            CancelarEdicaoButton.Visibility = Visibility.Collapsed;
        }
    }

    private void CarregarParaEdicao(UgfResumo ugf)
    {
        editingId = ugf.Id;
        importadasAtual = ugf.ToneladasImportadas;

        isCarregandoSelecao = true;
        CodigoCombo.SelectedItem = ugf.Codigo;
        CodigoCombo.Text = ugf.Codigo;
        isCarregandoSelecao = false;

        CertificadoBox.Text = ugf.ToneladasCertificado.ToString("0.###", CultureInfo.InvariantCulture);
        LimparErros();
        AtualizarPreview();

        FormTitleText.Text = "Editar UGF";
        BreadcrumbCurrentText.Text = "Editar UGF";
        GuardarButton.Content = "Guardar alterações";
        CancelarEdicaoButton.Visibility = Visibility.Visible;
    }

    private void CancelarEdicao_Click(object sender, RoutedEventArgs e) => EntrarModoCriacao();

    private void EntrarModoCriacao()
    {
        editingId = null;
        importadasAtual = 0m;

        isCarregandoSelecao = true;
        CodigoCombo.SelectedItem = null;
        CodigoCombo.Text = string.Empty;
        isCarregandoSelecao = false;

        CertificadoBox.Clear();
        LimparErros();
        AtualizarPreview();

        FormTitleText.Text = "Novo UGF";
        BreadcrumbCurrentText.Text = "Novo UGF";
        GuardarButton.Content = "Guardar";
        CancelarEdicaoButton.Visibility = Visibility.Collapsed;
    }

    private void CertificadoBox_TextChanged(object sender, TextChangedEventArgs e) => AtualizarPreview();

    private void AtualizarPreview()
    {
        if (TentarConverterDecimal(CertificadoBox.Text, out var certificado) && certificado > 0)
        {
            var preview = new UgfResumo { Codigo = string.Empty, ToneladasCertificado = certificado, ToneladasImportadas = importadasAtual };
            DisponivelText.Text = preview.ToneladasDisponiveis.ToString("0.000", CultureInfo.InvariantCulture);
            DisponivelToleranciaText.Text = preview.ToneladasDisponiveisComTolerancia.ToString("0.000", CultureInfo.InvariantCulture);
            EstadoText.Text = preview.Estado.ToString();
            EstadoText.Foreground = (Brush)EstadoCorConverter.Convert(preview.Estado, typeof(Brush), null, CultureInfo.InvariantCulture);
        }
        else
        {
            DisponivelText.Text = "—";
            DisponivelToleranciaText.Text = "—";
            EstadoText.Text = "—";
            EstadoText.Foreground = (Brush)FindResource("TextMutedBrush");
        }
    }

    private static bool TentarConverterDecimal(string? texto, out decimal valor)
    {
        valor = 0m;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        return decimal.TryParse(texto.Trim().Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        LimparErros();
        var codigo = CodigoCombo.Text?.Trim();
        var valido = true;

        if (string.IsNullOrWhiteSpace(codigo))
        {
            CodigoError.Text = "Escreve o código UGF";
            CodigoError.Visibility = Visibility.Visible;
            valido = false;
        }

        if (!TentarConverterDecimal(CertificadoBox.Text, out var certificado) || certificado <= 0)
        {
            CertificadoError.Text = "Escreve um valor de toneladas válido";
            CertificadoError.Visibility = Visibility.Visible;
            valido = false;
        }

        if (!valido)
        {
            return;
        }

        var ugf = new Ugf { Codigo = codigo!, ToneladasCertificado = certificado };

        GuardarButton.IsEnabled = false;
        try
        {
            if (editingId is int id)
            {
                ugf.Id = id;
                await AppServices.Ugfs.UpdateAsync(ugf);
                ToastText.Text = "UGF atualizado";
            }
            else
            {
                await AppServices.Ugfs.InsertAsync(ugf);
                ToastText.Text = "UGF guardado";
            }

            ToastBorder.Visibility = Visibility.Visible;
            EntrarModoCriacao();
            await CarregarListaAsync();
            await Task.Delay(1600);
            ToastBorder.Visibility = Visibility.Collapsed;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            MessageBox.Show("Já existe um UGF com este código.", "Não é possível guardar",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao guardar o UGF.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            GuardarButton.IsEnabled = true;
        }
    }

    private void Editar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is UgfResumo ugf)
        {
            CarregarParaEdicao(ugf);
        }
    }

    private void ListaGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ListaGrid.SelectedItem is UgfResumo ugf)
        {
            CarregarParaEdicao(ugf);
        }
    }

    private async void Apagar_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not UgfResumo ugf)
        {
            return;
        }

        var confirmar = MessageBox.Show(
            $"Tens a certeza que queres apagar o UGF \"{ugf.Codigo}\"? O histórico de entradas associado também é apagado.",
            "Confirmar eliminação", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirmar != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await AppServices.Ugfs.DeleteAsync(ugf.Id);

            if (editingId == ugf.Id)
            {
                EntrarModoCriacao();
            }

            await CarregarListaAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao apagar o UGF.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ImportarEntrada_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not UgfResumo ugf)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = $"Selecionar ficheiro de entregas para o UGF \"{ugf.Codigo}\"",
            Filter = "Ficheiros de entregas (*.xls;*.xlsx;*.htm;*.html)|*.xls;*.xlsx;*.htm;*.html|Todos os ficheiros (*.*)|*.*",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var resumo = EntregaExcelImporter.Ler(dialog.FileName);
            if (resumo.LinhasLidas == 0)
            {
                MessageBox.Show("Não foi possível encontrar a coluna \"QT.\" com quantidades neste ficheiro.",
                    "Nada para importar", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmar = MessageBox.Show(
                $"Foram lidas {resumo.LinhasLidas} linha(s), totalizando {resumo.TotalToneladas.ToString("0.000", CultureInfo.InvariantCulture)} toneladas.\n\n" +
                $"Importar esta entrada para o UGF \"{ugf.Codigo}\"?",
                "Confirmar importação", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmar != MessageBoxResult.Yes)
            {
                return;
            }

            await AppServices.Ugfs.RegistarEntradaAsync(ugf.Id, resumo.TotalToneladas, Path.GetFileName(dialog.FileName));
            await CarregarListaAsync();

            MessageBox.Show("Entrada registada com sucesso.", "Importação concluída",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao importar o ficheiro de entregas.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LimparErros()
    {
        CodigoError.Visibility = Visibility.Collapsed;
        CertificadoError.Visibility = Visibility.Collapsed;
    }
}
