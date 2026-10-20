using System.Windows.Controls;

namespace Cadroue.UIVeneer;

public partial class PSEncoderPicture : UserControl
{
    public PSEncoderPicture()
    {
        InitializeComponent();
        PSEncoderChip.PSEncoderChipApply(psEncoderCodecSelection);
    }
}
