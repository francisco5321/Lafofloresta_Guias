using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GuiasMadeira.Desktop.Services;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Desktop.Pages;

public partial class GuiaPage
{
    private const int TotalCampos = 5;

    private readonly GuiaResumo? guiaEmEdicao;
    private Dictionary<string, UgfResumo> ugfsPorCodigo = new(StringComparer.OrdinalIgnoreCase);

    private ICollectionView? destinatariosView;
    private ICollectionView? proprietariosView;
    private ICollectionView? certificadosView;
    private ICollectionView? rolariasView;

    public GuiaPage()
    {
        InitializeComponent();
        Loaded += GuiaPage_Loaded;
    }

    public GuiaPage(GuiaResumo guiaParaEditar) : this()
    {
        guiaEmEdicao = guiaParaEditar;
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
            var certificados = await AppServices.CodigosBarras.ListCertificadosDisponiveisAsync(guiaEmEdicao?.Id);
            var rolarias = await AppServices.Rolarias.ListAllAsync();
            var ugfs = await AppServices.Ugfs.ListResumoAsync();
            ugfsPorCodigo = ugfs
                .Where(u => !string.IsNullOrWhiteSpace(u.Codigo))
                .ToDictionary(u => u.Codigo, u => u, StringComparer.OrdinalIgnoreCase);
            AplicarLimiteUgf(certificados);

            destinatariosView = CriarViewFiltravel(destinatarios, DestinatarioCombo, d => d.Nome);
            DestinatarioCombo.ItemsSource = destinatariosView;
            DestinatarioEmptyState.Visibility = destinatarios.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            DestinatarioCombo.Visibility = destinatarios.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            proprietariosView = CriarViewFiltravel(proprietarios, ProprietarioCombo, p => p.Nome);
            ProprietarioCombo.ItemsSource = proprietariosView;
            ProprietarioEmptyState.Visibility = proprietarios.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ProprietarioCombo.Visibility = proprietarios.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            certificadosView = CriarViewFiltravel(certificados, CertificadoCombo, c => c.Rotulo);
            CertificadoCombo.ItemsSource = certificadosView;
            CertificadoEmptyState.Visibility = certificados.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            CertificadoCombo.Visibility = certificados.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            rolariasView = CriarViewFiltravel(rolarias, RolariaCombo, r => r.Tipo);
            RolariaCombo.ItemsSource = rolariasView;
            RolariaEmptyState.Visibility = rolarias.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            RolariaCombo.Visibility = rolarias.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            if (guiaEmEdicao is not null)
            {
                DestinatarioCombo.SelectedItem = destinatarios.FirstOrDefault(d => d.Id == guiaEmEdicao.DestinatarioId);
                ProprietarioCombo.SelectedItem = proprietarios.FirstOrDefault(p => p.Id == guiaEmEdicao.ProprietarioId);
                CertificadoCombo.SelectedItem = certificados.FirstOrDefault(
                    c => string.Equals(c.NumeroCertificado, guiaEmEdicao.CodigoBarraNumeroCertificado, StringComparison.OrdinalIgnoreCase));
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

    private void Campo_Changed(object sender, RoutedEventArgs e)
    {
        DestinatarioCombo.Tag = null;
        ProprietarioCombo.Tag = null;
        CertificadoCombo.Tag = null;
        RolariaCombo.Tag = null;
        AtualizarEstado();
    }

    /// <summary>
    /// Vista filtrável para uma combobox editável: enquanto o utilizador escreve, a lista de
    /// opções mostrada no dropdown fica reduzida às que contêm o texto escrito (em vez de só
    /// completar automaticamente para a correspondência mais próxima).
    /// </summary>
    private static ICollectionView CriarViewFiltravel<T>(IReadOnlyList<T> itens, ComboBox combo, Func<T, string> obterTexto)
    {
        var view = CollectionViewSource.GetDefaultView(itens);
        view.Filter = item => string.IsNullOrEmpty(combo.Text) ||
            (item is T tItem && obterTexto(tItem).Contains(combo.Text, StringComparison.OrdinalIgnoreCase));
        return view;
    }

    private void DestinatarioCombo_GotFocus(object sender, RoutedEventArgs e) => DestinatarioCombo.IsDropDownOpen = true;

    private void DestinatarioCombo_TextChanged(object sender, TextChangedEventArgs e)
    {
        destinatariosView?.Refresh();
        if (DestinatarioCombo.IsKeyboardFocusWithin)
        {
            DestinatarioCombo.IsDropDownOpen = true;
        }
    }

    private void ProprietarioCombo_GotFocus(object sender, RoutedEventArgs e) => ProprietarioCombo.IsDropDownOpen = true;

    private void ProprietarioCombo_TextChanged(object sender, TextChangedEventArgs e)
    {
        proprietariosView?.Refresh();
        if (ProprietarioCombo.IsKeyboardFocusWithin)
        {
            ProprietarioCombo.IsDropDownOpen = true;
        }
    }

    private void CertificadoCombo_GotFocus(object sender, RoutedEventArgs e) => CertificadoCombo.IsDropDownOpen = true;

    private void CertificadoCombo_TextChanged(object sender, TextChangedEventArgs e)
    {
        certificadosView?.Refresh();
        if (CertificadoCombo.IsKeyboardFocusWithin)
        {
            CertificadoCombo.IsDropDownOpen = true;
        }
    }

    private void RolariaCombo_GotFocus(object sender, RoutedEventArgs e) => RolariaCombo.IsDropDownOpen = true;

    private void RolariaCombo_TextChanged(object sender, TextChangedEventArgs e)
    {
        rolariasView?.Refresh();
        if (RolariaCombo.IsKeyboardFocusWithin)
        {
            RolariaCombo.IsDropDownOpen = true;
        }
    }

    private void AtualizarEstado()
    {
        var preenchidos = 0;
        if (DestinatarioCombo.SelectedItem is not null) preenchidos++;
        if (ProprietarioCombo.SelectedItem is not null) preenchidos++;
        if (CertificadoCombo.SelectedItem is not null) preenchidos++;
        if (RolariaCombo.SelectedItem is not null) preenchidos++;
        if (!string.IsNullOrWhiteSpace(FornecedorBox.Text)) preenchidos++;

        ProgressFillColumn.Width = new GridLength(preenchidos, GridUnitType.Star);
        ProgressRemainderColumn.Width = new GridLength(TotalCampos - preenchidos, GridUnitType.Star);
        ProgressText.Text = $"{preenchidos} de {TotalCampos} campos preenchidos";

        var camposObrigatoriosFaltam = 0;
        if (DestinatarioCombo.SelectedItem is null) camposObrigatoriosFaltam++;
        if (ProprietarioCombo.SelectedItem is null) camposObrigatoriosFaltam++;
        if (CertificadoCombo.SelectedItem is null) camposObrigatoriosFaltam++;
        if (RolariaCombo.SelectedItem is null) camposObrigatoriosFaltam++;

        var ugfBloqueado = ObterUgfBloqueado();
        if (ugfBloqueado is not null)
        {
            CertificadoUgfError.Text = ugfBloqueado.LimiteGuiasAtingido
                ? $"Limite de guias atingido para o certificado \"{ugfBloqueado.Codigo}\" ({ugfBloqueado.GuiasCriadas}/{ugfBloqueado.NumeroMaximoGuias}). Escolhe outro certificado."
                : $"O UGF \"{ugfBloqueado.Codigo}\" já atingiu o limite de toneladas (+20%) e está bloqueado. Escolhe outro certificado.";
            CertificadoUgfError.Visibility = Visibility.Visible;
            CertificadoAvisoTolerancia.Visibility = Visibility.Collapsed;
        }
        else
        {
            CertificadoUgfError.Visibility = Visibility.Collapsed;

            var ugfEmAviso = ObterUgfEmAvisoDeGuias();
            if (ugfEmAviso is not null)
            {
                CertificadoAvisoTolerancia.Text =
                    $"Atenção: as guias já criadas para \"{ugfEmAviso.Codigo}\" estão a consumir a tolerância de 20% ({ugfEmAviso.GuiasCriadas}/{ugfEmAviso.NumeroMaximoGuias}).";
                CertificadoAvisoTolerancia.Visibility = Visibility.Visible;
            }
            else
            {
                CertificadoAvisoTolerancia.Visibility = Visibility.Collapsed;
            }
        }

        ImprimirButton.IsEnabled = camposObrigatoriosFaltam == 0 && ugfBloqueado is null;
        MicrocopyText.Text = camposObrigatoriosFaltam == 0
            ? (ugfBloqueado is null ? string.Empty : "Não é possível imprimir: certificado com UGF bloqueado")
            : camposObrigatoriosFaltam == 1
                ? "Falta 1 campo para poderes imprimir"
                : $"Faltam {camposObrigatoriosFaltam} campos para poderes imprimir";
    }

    private UgfResumo? ObterUgfBloqueado()
    {
        // O código UGF (formato "UGFPT#####") está gravado no mesmo valor que identifica o
        // certificado — ver CodigoBarraRepository.ListNumerosUgfExistentesAsync.
        if (CertificadoCombo.SelectedItem is not CertificadoResumo certificado)
        {
            return null;
        }

        return ugfsPorCodigo.TryGetValue(certificado.NumeroCertificado, out var ugf) && (ugf.Estado == EstadoUgf.Bloqueado || ugf.LimiteGuiasAtingido)
            ? ugf
            : null;
    }

    private UgfResumo? ObterUgfEmAvisoDeGuias()
    {
        if (CertificadoCombo.SelectedItem is not CertificadoResumo certificado)
        {
            return null;
        }

        return ugfsPorCodigo.TryGetValue(certificado.NumeroCertificado, out var ugf) && ugf.LimiteGuiasEmTolerancia
            ? ugf
            : null;
    }

    private async void Imprimir_Click(object sender, RoutedEventArgs e)
    {
        var destinatario = DestinatarioCombo.SelectedItem as Destinatario;
        var proprietario = ProprietarioCombo.SelectedItem as Proprietario;
        var rolaria = RolariaCombo.SelectedItem as Rolaria;
        var certificado = CertificadoCombo.SelectedItem as CertificadoResumo;

        if (destinatario is null || proprietario is null || rolaria is null || certificado is null || ObterUgfBloqueado() is not null)
        {
            return;
        }

        var guia = new Guia
        {
            Id = guiaEmEdicao?.Id ?? 0,
            DestinatarioId = destinatario.Id,
            ProprietarioId = proprietario.Id,
            RolariaId = rolaria.Id,
            Fornecedor = string.IsNullOrWhiteSpace(FornecedorBox.Text) ? null : FornecedorBox.Text.Trim()
        };

        ImprimirButton.IsEnabled = false;
        try
        {
            int idGuia;
            if (guiaEmEdicao is not null)
            {
                var certificadoInalterado = string.Equals(
                    certificado.NumeroCertificado, guiaEmEdicao.CodigoBarraNumeroCertificado, StringComparison.OrdinalIgnoreCase);

                bool sucesso;
                if (certificadoInalterado)
                {
                    guia.CodigoBarraId = guiaEmEdicao.CodigoBarraId;
                    await AppServices.Guias.UpdateAsync(guia);
                    sucesso = true;
                }
                else
                {
                    sucesso = await AppServices.Guias.AtualizarComCertificadoAsync(guia, certificado.NumeroCertificado);
                }

                if (!sucesso)
                {
                    MessageBox.Show("Já não há vinhetas disponíveis para este certificado. Escolhe outro.",
                        "Sem vinhetas disponíveis", MessageBoxButton.OK, MessageBoxImage.Warning);
                    await RecarregarCertificadosAsync();
                    return;
                }

                idGuia = guiaEmEdicao.Id;
            }
            else
            {
                var novoId = await AppServices.Guias.InsertComCertificadoAsync(guia, certificado.NumeroCertificado);
                if (novoId is null)
                {
                    MessageBox.Show("Já não há vinhetas disponíveis para este certificado. Escolhe outro.",
                        "Sem vinhetas disponíveis", MessageBoxButton.OK, MessageBoxImage.Warning);
                    await RecarregarCertificadosAsync();
                    return;
                }

                idGuia = novoId.Value;
            }

            DestinatarioCombo.Tag = "Success";
            ProprietarioCombo.Tag = "Success";
            CertificadoCombo.Tag = "Success";
            RolariaCombo.Tag = "Success";

            ToastBorder.Visibility = Visibility.Visible;
            await Task.Delay(900);
            ToastBorder.Visibility = Visibility.Collapsed;

            var aberto = await GuiaPrintService.PreviewAsync(idGuia);
            if (!aberto)
            {
                MessageBox.Show("A guia foi guardada, mas não foi possível gerar a pré-visualização.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            Window.GetWindow(this)?.Close();
            return;
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

    private async Task RecarregarCertificadosAsync()
    {
        try
        {
            var certificados = await AppServices.CodigosBarras.ListCertificadosDisponiveisAsync(guiaEmEdicao?.Id);
            AplicarLimiteUgf(certificados);
            certificadosView = CriarViewFiltravel(certificados, CertificadoCombo, c => c.Rotulo);
            CertificadoCombo.ItemsSource = certificadosView;
            CertificadoEmptyState.Visibility = certificados.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            CertificadoCombo.Visibility = certificados.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        catch
        {
            // Se falhar aqui, o próprio Guardar/Imprimir já mostrou o erro relevante.
        }
    }

    /// <summary>
    /// O número "X disponíveis" mostrado no seletor de certificado deve refletir o limite real de
    /// guias — não só as vinhetas físicas, mas também o limite por toneladas/carga média definido
    /// em Limites UGF (o menor dos dois, ver CertificadoResumo.DisponiveisEfetivo).
    /// </summary>
    private void AplicarLimiteUgf(IReadOnlyList<CertificadoResumo> certificados)
    {
        foreach (var certificado in certificados)
        {
            certificado.GuiasRestantesUgf = ugfsPorCodigo.TryGetValue(certificado.NumeroCertificado, out var ugf)
                ? ugf.GuiasRestantes
                : null;
        }
    }

    private void AdicionarDestinatario_Click(object sender, RoutedEventArgs e) => FecharENavegarPara("Destinatarios");

    private void AdicionarProprietario_Click(object sender, RoutedEventArgs e) => FecharENavegarPara("Proprietarios");

    private void AdicionarRolaria_Click(object sender, RoutedEventArgs e) => FecharENavegarPara("Rolarias");

    private void ImportarCodigosBarras_Click(object sender, RoutedEventArgs e) => FecharENavegarPara("CodigosBarras");

    /// <summary>
    /// Fecha este modal e só depois pede à janela principal para mudar de secção — como a GuiaPage
    /// já não vive na MainFrame, deixar o modal aberto por cima do ecrã que muda por baixo seria
    /// confuso.
    /// </summary>
    private void FecharENavegarPara(string seccao)
    {
        Window.GetWindow(this)?.Close();
        AppNavigation.NavigateToSection?.Invoke(seccao);
    }

    private void CancelarEdicao_Click(object sender, RoutedEventArgs e) => Window.GetWindow(this)?.Close();
}
