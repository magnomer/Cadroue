using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal static class PInspectorButton
{
    internal static Button PInspectorButtonBuild(string pIconPath, string pTooltip, Action pClick)
    {
        Button pButton = PInspectorGlyphBuild(pIconPath, pTooltip);
        PInspectorClickAttach(pButton, pClick);
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
        return pButton;
    }

    private static Button PInspectorGlyphBuild(string pIconPath, string pTooltip) => new()
    {
        Content = new Image
        {
            Width = 20,
            Height = 20,
            Source = QIcon.QIconRead(pIconPath, PInspectorLook.PInspectorIconBrush),
            Stretch = Stretch.Uniform
        },
        ToolTip = pTooltip,
        Width = 28,
        Height = 26
    };

    internal static void PInspectorClickAttach(Button pButton, Action pClick) =>
        pButton.Click += (_, _) => pClick();
}
