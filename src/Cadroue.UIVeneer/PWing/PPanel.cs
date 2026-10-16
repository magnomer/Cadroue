using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public class PPanel : UserControl
{
    protected static readonly Brush PPanelLineBrush = QPanel.QPanelLineRead();
    protected static readonly Brush PPanelTextBrush = QPanel.QPanelTextRead();

    public PPanel(string pPanelTitle)
    {
        QPanel.QPanelAttach(this);
        Content = PPanelBorderBuild(new TextBlock
        {
            Text = pPanelTitle,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = PPanelTextBrush,
            FontSize = 18
        });
    }

    public static Border PPanelBorderBuild(UIElement pPanelContent) => QPanel.QPanelBuild(pPanelContent);
}
