using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public class PPanel : UserControl
{
    public PPanel(string pPanelTitle)
    {
        QPanel.QPanelAttach(this);
        Content = PPanelBorderBuild(new TextBlock
        {
            Text = pPanelTitle,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = QPanel.QPanelTextRead(),
            FontSize = 18
        });
    }

    public static Border PPanelBorderBuild(UIElement pPanelContent) => QPanel.QPanelBuild(pPanelContent);
}
