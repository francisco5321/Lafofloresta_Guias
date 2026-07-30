using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Desktop.Pages;

/// <summary>
/// Janela modal que aloja a GuiaPage (criar ou editar). A própria GuiaPage é responsável por
/// fechar esta janela (via Window.GetWindow(this)) depois de gravar com sucesso ou cancelar.
/// Sem chrome nativo (WindowStyle=None + AllowsTransparency) para se apresentar como um cartão
/// flutuante com sombra e cantos arredondados, com uma pequena animação de entrada em vez de
/// aparecer instantaneamente como uma janela comum.
/// </summary>
public partial class GuiaModalWindow
{
    public GuiaModalWindow()
    {
        InitializeComponent();
        PosicionarNoEcra();
        Title = "Nova guia";
        HeaderTitleText.Text = "Nova guia";
        ContentFrame.Navigate(new GuiaPage());
        Loaded += (_, _) => AnimarEntrada();
    }

    public GuiaModalWindow(GuiaResumo guiaParaEditar)
    {
        InitializeComponent();
        PosicionarNoEcra();
        Title = "Editar guia";
        HeaderTitleText.Text = "Editar guia";
        ContentFrame.Navigate(new GuiaPage(guiaParaEditar));
        Loaded += (_, _) => AnimarEntrada();
    }

    /// <summary>
    /// Cobre a área útil do ecrã (a mesma usada por MainWindow.SizeToScreen) em vez de copiar os
    /// limites da janela principal — se esta estiver maximizada, Owner.Left/Top/ActualWidth/
    /// ActualHeight podem não refletir com fiabilidade os limites reais no ecrã, o que deixava a
    /// sobreposição a cobrir só uma parte do ecrã. Feito no construtor (antes do ShowDialog),
    /// porque numa janela AllowsTransparency=True mudar Left/Top/Width/Height depois de já ter
    /// começado a aparecer não é fiável.
    /// </summary>
    private void PosicionarNoEcra()
    {
        var areaUtil = SystemParameters.WorkArea;
        Left = areaUtil.Left;
        Top = areaUtil.Top;
        Width = areaUtil.Width;
        Height = areaUtil.Height;
    }

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
