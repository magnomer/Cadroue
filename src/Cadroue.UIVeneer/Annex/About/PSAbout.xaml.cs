using System.Windows;

namespace Cadroue.UIVeneer;

public partial class PSAbout : Window
{
    public PSAbout()
    {
        InitializeComponent();
        PSDialog.PSDialogApply(this, Background);
    }
}
