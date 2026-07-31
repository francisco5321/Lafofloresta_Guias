using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GuiasMadeira.Desktop.Pages;

/// <summary>
/// Janela modal genérica que aloja qualquer página de formulário (criar ou editar). A própria
/// página alojada é responsável por fechar esta janela (via Window.GetWindow(this)) depois de
/// gravar com sucesso ou cancelar. Sem chrome nativo (WindowStyle=None + AllowsTransparency) para
/// se apresentar como um cartão flutuante com sombra e cantos arredondados, com uma pequena
/// animação de entrada em vez de aparecer instantaneamente como uma janela comum.
/// </summary>
public partial class FormModalWindow
{
    /// <param name="conteudo">A página de formulário a mostrar (ex. GuiaPage, DestinatarioFormPage).</param>
    /// <param name="titulo">Título mostrado no cabeçalho (ex. "Nova guia", "Editar destinatário").</param>
    public FormModalWindow(Page conteudo, string titulo)
    {
        InitializeComponent();
        PosicionarNoEcra();

        Title = titulo;
        HeaderTitleText.Text = titulo;
        ContentFrame.Navigate(conteudo);
        Loaded += (_, _) => AnimarEntrada();
    }

    /// <summary>
    /// Cobre a área útil do ecrã em vez de copiar os limites da janela principal — se esta estiver
    /// maximizada, Owner.Left/Top/ActualWidth/ActualHeight podem não refletir com fiabilidade os
    /// limites reais no ecrã, o que deixava a sobreposição a cobrir só uma parte do ecrã. Feito no
    /// construtor (antes do ShowDialog), porque numa janela AllowsTransparency=True mudar
    /// Left/Top/Width/Height depois de já ter começado a aparecer não é fiável. Também dá um
    /// MaxHeight de segurança ao cartão (que se dimensiona ao conteúdo) para nunca ultrapassar o
    /// ecrã caso um formulário futuro seja invulgarmente alto.
    /// </summary>
    private void PosicionarNoEcra()
    {
        var areaUtil = SystemParameters.WorkArea;
        Left = areaUtil.Left;
        Top = areaUtil.Top;
        Width = areaUtil.Width;
        Height = areaUtil.Height;
        CardBorder.MaxHeight = Math.Max(400, areaUtil.Height - 80);
    }

    private void CardBorder_SizeChanged(object sender, SizeChangedEventArgs e) =>
        CardClip.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);

    private void RootGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, RootGrid))
        {
            Close();
        }
    }

    private void AnimarEntrada()
    {
        var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));

        var escala = new DoubleAnimation(0.95, 1.0, TimeSpan.FromMilliseconds(220)) { EasingFunction = easing };
        CardScale.BeginAnimation(ScaleTransform.ScaleXProperty, escala);
        CardScale.BeginAnimation(ScaleTransform.ScaleYProperty, escala);
    }

    private void Fechar_Click(object sender, RoutedEventArgs e) => Close();
}
