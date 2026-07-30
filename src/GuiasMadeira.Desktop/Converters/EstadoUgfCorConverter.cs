using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GuiasMadeira.Domain.Entities;

namespace GuiasMadeira.Desktop.Converters;

/// <summary>
/// Converte o estado do semáforo de um UGF (Verde/Amarelo/Vermelho/Bloqueado) na cor
/// correspondente, para colorir o texto/badge de estado na grid e no formulário.
/// </summary>
public sealed class EstadoUgfCorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not EstadoUgf estado)
        {
            return Brushes.Transparent;
        }

        return estado switch
        {
            EstadoUgf.Verde => (Brush)new SolidColorBrush(Color.FromRgb(0x1E, 0x7A, 0x3D)),
            EstadoUgf.Amarelo => new SolidColorBrush(Color.FromRgb(0xB8, 0x86, 0x0B)),
            EstadoUgf.Vermelho => new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B)),
            EstadoUgf.Bloqueado => new SolidColorBrush(Color.FromRgb(0x6B, 0x21, 0x1A)),
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
