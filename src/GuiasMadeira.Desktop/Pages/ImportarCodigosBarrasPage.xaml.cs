using System.Windows;
using GuiasMadeira.Desktop.Services;
using GuiasMadeira.Domain.Entities;
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

    public ImportarCodigosBarrasPage()
    {
        InitializeComponent();
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
            MessageBox.Show($"{inseridos} código(s) de barras importado(s) com sucesso!",
                "Importação concluída", MessageBoxButton.OK, MessageBoxImage.Information);

            FicheiroText.Text = "Nenhum ficheiro selecionado.";
            ResumoText.Text = string.Empty;
            PreviewGrid.ItemsSource = null;
            linhas.Clear();
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
