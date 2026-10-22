using System;
using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PInspector : PPanel
{
    public const double PInspectorStripWidth = 48;

    public PInspector(QInspectorFrame lFrame)
        : base("")
    {
        QInspectorFrame = lFrame;
        var pSheet = new ResourceDictionary
        {
            Source = new Uri("/Cadroue.UIVeneer;component/Wing/Inspector/PInspector.xaml", UriKind.RelativeOrAbsolute)
        };
        var pInspector = (FrameworkElement)pSheet[nameof(PInspector)];
        Content = pInspector;
        QInspectorFrame.QInspectorFrameBuild(pInspector);
    }

    public QInspectorFrame QInspectorFrame { get; }
}
