using System.Globalization;
using System.Windows.Data;

namespace Cadroue.UIDeportment;

public sealed class QStripImage : IValueConverter
{
    public bool QStripImageActive { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        QStripIcon.QStripIconRead((string)value, QStripImageActive);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
