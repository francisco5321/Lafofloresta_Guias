using System.Windows;
using System.Windows.Media.Effects;

namespace GuiasMadeira.Desktop.Services;

/// <summary>
/// Mostra uma janela modal com a janela principal ligeiramente desfocada por baixo — realça o
/// modal em vez de o deixar como mais uma janela solta sobreposta. Usado por todas as páginas de
/// listagem (Guias, Destinatários, Proprietários, Rolarias) para abrir o respetivo FormModalWindow.
/// </summary>
public static class ModalHelper
{
    public static void ShowModal(FrameworkElement chamador, Window modal)
    {
        var mainWindow = Window.GetWindow(chamador);
        var blur = new BlurEffect { Radius = 14 };
        if (mainWindow is not null)
        {
            mainWindow.Effect = blur;
            modal.Owner = mainWindow;
        }

        try
        {
            modal.ShowDialog();
        }
        finally
        {
            if (mainWindow is not null)
            {
                mainWindow.Effect = null;
            }
        }
    }
}
