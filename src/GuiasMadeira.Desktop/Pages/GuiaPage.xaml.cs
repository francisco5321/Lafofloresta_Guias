using System.Windows;
using GuiasMadeira.Desktop.Services;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Desktop.Pages;

public partial class GuiaPage
{
    public GuiaPage()
    {
        InitializeComponent();
        Loaded += GuiaPage_Loaded;
    }

    private async void GuiaPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            DestinatarioCombo.ItemsSource = await AppServices.Destinatarios.ListAllAsync();
            ProprietarioCombo.ItemsSource = await AppServices.Proprietarios.ListAllAsync();
            CodigoBarraCombo.ItemsSource = await AppServices.CodigosBarras.ListAllAsync();
            RolariaCombo.ItemsSource = await AppServices.Rolarias.ListAllAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível ligar à base de dados.\n\n{ex.Message}",
                "Erro de ligação", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Imprimir_Click(object sender, RoutedEventArgs e)
    {
        var destinatario = DestinatarioCombo.SelectedItem as Destinatario;
        var proprietario = ProprietarioCombo.SelectedItem as Proprietario;
        var rolaria = RolariaCombo.SelectedItem as Rolaria;
        var codigoBarra = CodigoBarraCombo.SelectedItem as CodigoBarra;

        if (destinatario is null)
        {
            AvisarCampoObrigatorio("um Destinatário");
            DestinatarioCombo.Focus();
            return;
        }

        if (proprietario is null)
        {
            AvisarCampoObrigatorio("um Proprietário");
            ProprietarioCombo.Focus();
            return;
        }

        if (rolaria is null)
        {
            AvisarCampoObrigatorio("um Tipo de Rolaria");
            RolariaCombo.Focus();
            return;
        }

        var guia = new Guia
        {
            DestinatarioId = destinatario.Id,
            ProprietarioId = proprietario.Id,
            CodigoBarraId = codigoBarra?.Id,
            RolariaId = rolaria.Id,
            Fornecedor = string.IsNullOrWhiteSpace(FornecedorBox.Text) ? null : FornecedorBox.Text.Trim()
        };

        ImprimirButton.IsEnabled = false;
        try
        {
            var idGuia = await AppServices.Guias.InsertAsync(guia);
            var aberto = await GuiaPrintService.PreviewAsync(idGuia);
            if (!aberto)
            {
                MessageBox.Show("A guia foi gravada, mas não foi possível gerar a pré-visualização.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao gravar/imprimir a guia.\n\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ImprimirButton.IsEnabled = true;
        }
    }

    private static void AvisarCampoObrigatorio(string campo) =>
        MessageBox.Show($"Por favor, selecione {campo} antes de continuar.",
            "Campo obrigatório", MessageBoxButton.OK, MessageBoxImage.Exclamation);
}
