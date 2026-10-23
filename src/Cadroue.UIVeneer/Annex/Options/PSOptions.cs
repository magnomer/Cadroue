using System.Windows;

namespace Cadroue.UIVeneer;

internal partial class PSOptions : Window
{
    internal PSOptions()
    {
        InitializeComponent();
        PSSubwindow.PSSubwindowApply(this);
    }
}
