using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GuiasMadeira.Desktop.Services;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Desktop.Pages;

public partial class GuiaPage
{
    private const int TotalCampos = 5;

    private readonly GuiaResumo? guiaEmEdicao;

    public GuiaPage()
    {
        InitializeComponent();
        Loaded += GuiaPage_Loaded;
    }

    public GuiaPage(GuiaResumo guiaParaEditar) : this()
    {
        guiaEmEdicao = guiaParaEditar;
        BreadcrumbCurrentText.Text = "Editar guia";
        PageTitleText.Text = "Editar guia";
        ToastText.Text = "Guia atualizada";
        ImprimirButtonText.Text = "Guardar alterações";
        CancelarEdicaoButton.Visibility = Visibility.Visible;
        FornecedorBox.Text = guiaParaEditar.Fornecedor;
    }

    private async void GuiaPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var destinatarios = await AppServices.Destinatarios.ListAllAsync();
            var proprietarios = await AppServices.Proprietarios.ListAllAsync();
            var codigosBarras = await AppServices.CodigosBarras.ListAllAsync();
            var rolarias = await AppServices.Rolarias.ListAllAsync();

            DestinatarioCombo.ItemsSource = destinatarios;
            DestinatarioEmptyState.Visibility = destinatarios.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            DestinatarioCombo.Visibility = destinatarios.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            ProprietarioCombo.ItemsSource = proprietarios;
            ProprietarioEmptyState.Visibility = proprietarios.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ProprietarioCombo.Visibility = proprietarios.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            CodigoBarraCombo.ItemsSource = codigosBarras;
            CodigoBarraEmptyState.Visibility = codigosBarras.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            CodigoBarraCombo.Visibility = codigosBarras.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            RolariaCombo.ItemsSource = rolarias;
            RolariaEmptyState.Visibility = rolarias.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            RolariaCombo.Visibility = rolarias.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            if (guiaEmEdicao is not null)
            {
                DestinatarioCombo.SelectedItem = destinatarios.FirstOrDefault(d => d.Id == guiaEmEdicao.DestinatarioId);
                ProprietarioCombo.SelectedItem = proprietarios.FirstOrDefault(p => p.Id == guiaEmEdicao.ProprietarioId);
                CodigoBarraCombo.SelectedItem = codigosBarras.FirstOrDefault(c => c.Id == guiaEmEdicao.CodigoBarraId);
                RolariaCombo.SelectedItem = rolarias.FirstOrDefault(r => r.Id == guiaEmEdicao.RolariaId);
            }

            AtualizarEstado();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível ligar à base de dados.\n\n{ex.Message}",
                "Erro de ligação", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Campo_Changed(object sender, RoutedEventArgs e) => AtualizarEstado();

    private void AtualizarEstado()
    {
        var preenchidos = 0;
        if (DestinatarioCombo.SelectedItem is not null) preenchidos++;
        if (ProprietarioCombo.SelectedItem is not null) preenchidos++;
        if (CodigoBarraCombo.SelectedItem is not null) preenchidos++;
        if (RolariaCombo.SelectedItem is not null) preenchidos++;
        if (!string.IsNullOrWhiteSpace(FornecedorBox.Text)) preenchidos++;

        ProgressFillColumn.Width = new GridLength(preenchidos, GridUnitType.Star);
        ProgressRemainderColumn.Width = new GridLength(TotalCampos - preenchidos, GridUnitType.Star);
        ProgressText.Text = $"{preenchidos} de {TotalCampos} campos preenchidos";

        var camposObrigatoriosFaltam = 0;
        if (DestinatarioCombo.SelectedItem is null) camposObrigatoriosFaltam++;
        if (ProprietarioCombo.SelectedItem is null) camposObrigatoriosFaltam++;
        if (CodigoBarraCombo.SelectedItem is null) camposObrigatoriosFaltam++;
        if (RolariaCombo.SelectedItem is null) camposObrigatoriosFaltam++;

        ImprimirButton.IsEnabled = camposObrigatoriosFaltam == 0;
        MicrocopyText.Text = camposObrigatoriosFaltam == 0
            ? string.Empty
            : camposObrigatoriosFaltam == 1
                ? "Falta 1 campo para poderes imprimir"
                : $"Faltam {camposObrigatoriosFaltam} campos para poderes imprimir";
    }

    private async void Imprimir_Click(object sender, RoutedEventArgs e)
    {
        var destinatario = DestinatarioCombo.SelectedItem as Destinatario;
        var proprietario = ProprietarioCombo.SelectedItem as Proprietario;
        var rolaria = RolariaCombo.SelectedItem as Rolaria;
        var codigoBarra = CodigoBarraCombo.SelectedItem as CodigoBarra;

        if (destinatario is null || proprietario is null || rolaria is null)
        {
            return;
        }

        var guia = new Guia
        {
            Id = guiaEmEdicao?.Id ?? 0,
            DestinatarioId = destinatario.Id,
            ProprietarioId = proprietario.Id,
            CodigoBarraId = codigoBarra?.Id,
            RolariaId = rolaria.Id,
            Fornecedor = string.IsNullOrWhiteSpace(FornecedorBox.Text) ? null : FornecedorBox.Text.Trim()
        };

        ImprimirButton.IsEnabled = false;
        try
        {
            int idGuia;
            var estavaEmEdicao = guiaEmEdicao is not null;
            if (guiaEmEdicao is not null)
            {
                await AppServices.Guias.UpdateAsync(guia);
                idGuia = guiaEmEdicao.Id;
            }
            else
            {
                idGuia = await AppServices.Guias.InsertAsync(guia);
            }

            ToastBorder.Visibility = Visibility.Visible;
            await Task.Delay(900);
            ToastBorder.Visibility = Visibility.Collapsed;

            var aberto = await GuiaPrintService.PreviewAsync(idGuia);
            if (!aberto)
            {
                MessageBox.Show("A guia foi guardada, mas não foi possível gerar a pré-visualização.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (estavaEmEdicao)
            {
                AppNavigation.NavigateToSection?.Invoke("Guias");
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao gravar/imprimir a guia.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            AtualizarEstado();
        }
    }

    private void AdicionarDestinatario_Click(object sender, RoutedEventArgs e) =>
        AppNavigation.NavigateToSection?.Invoke("Destinatarios");

    private void AdicionarProprietario_Click(object sender, RoutedEventArgs e) =>
        AppNavigation.NavigateToSection?.Invoke("Proprietarios");

    private void AdicionarRolaria_Click(object sender, RoutedEventArgs e) =>
        AppNavigation.NavigateToSection?.Invoke("Rolarias");

    private void ImportarCodigosBarras_Click(object sender, RoutedEventArgs e) =>
        AppNavigation.NavigateToSection?.Invoke("CodigosBarras");

    private void CancelarEdicao_Click(object sender, RoutedEventArgs e) =>
        AppNavigation.NavigateToSection?.Invoke("Guias");
}
