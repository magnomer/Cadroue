using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;

namespace Cadroue.UIVeneer.PWing;

public sealed class PInfo : UserControl
{
    private static readonly SolidColorBrush PInfoTextBrush = new(Color.FromRgb(0x4B, 0x55, 0x63));
    private static readonly SolidColorBrush PInfoMutedBrush = new(Color.FromRgb(0x9C, 0xA3, 0xAF));
    private static readonly SolidColorBrush PInfoBorderBrush = new(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly SolidColorBrush PInfoSeparatorBrush = new(Color.FromRgb(0xE5, 0xE7, 0xEB));
    private static readonly SolidColorBrush PInfoGoodBrush = new(Color.FromRgb(0x3A, 0x8B, 0xE0));
    private static readonly SolidColorBrush PInfoBadBrush = new(Color.FromRgb(0xE0, 0x53, 0x53));

    private static readonly IReadOnlyDictionary<string, Func<string, UIElement>> pInfoBuilders =
        new Dictionary<string, Func<string, UIElement>>
        {
            [QInfo.QInfoKindMuted] = pText => PInfoTextBuild(pText, PInfoMutedBrush),
            [QInfo.QInfoKindText] = pText => PInfoTextBuild(pText, PInfoTextBrush),
            [QInfo.QInfoKindGrey] = pText => PInfoStatusBuild(pText, PInfoMutedBrush),
            [QInfo.QInfoKindGood] = pText => PInfoStatusBuild(pText, PInfoGoodBrush),
            [QInfo.QInfoKindBad] = pText => PInfoStatusBuild(pText, PInfoBadBrush),
            [QInfo.QInfoKindSeparator] = pText => PInfoSeparatorBuild(),
        };

    private readonly StackPanel pInfoItemPanel;

    public QInfo QInfo { get; }

    public PInfo(QInfo lInfo)
    {
        QInfo = lInfo;
        MinHeight = 38;

        pInfoItemPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        var pContentRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pContentRow.Children.Add(PInfoIconCreate());
        pContentRow.Children.Add(pInfoItemPanel);

        Content = new Border
        {
            Padding = new Thickness(14, 4, 14, 4),
            BorderBrush = PInfoBorderBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Child = pContentRow
        };

        PInfoUpdate();
        QInfo.QInfoChanged += PInfoChangeHandle;
        Unloaded += PInfoUnloadedHandle;
    }

    private void PInfoUnloadedHandle(object pSender, RoutedEventArgs pEvent) => QInfo.QInfoDetach();

    private void PInfoChangeHandle() => Dispatcher.BeginInvoke(PInfoUpdate);

    private void PInfoUpdate()
    {
        pInfoItemPanel.Children.Clear();
        QInfo.QInfoRowsRead().Select(PInfoRowBuild).ToList().ForEach(pItem => pInfoItemPanel.Children.Add(pItem));
    }

    private static UIElement PInfoRowBuild(QInfoRow lRow) => pInfoBuilders[lRow.QInfoRowKind](lRow.QInfoRowText);

    private static TextBlock PInfoTextBuild(string pText, Brush pForeground) => new()
    {
        Text = pText,
        FontSize = 11,
        Foreground = pForeground,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static StackPanel PInfoStatusBuild(string pText, Brush pDotBrush)
    {
        var pRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pRow.Children.Add(new Ellipse
        {
            Width = 7,
            Height = 7,
            Fill = pDotBrush,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        pRow.Children.Add(PInfoTextBuild(pText, PInfoTextBrush));
        return pRow;
    }

    private static Border PInfoSeparatorBuild() => new()
    {
        Width = 1,
        Height = 12,
        Background = PInfoSeparatorBrush,
        Margin = new Thickness(14, 0, 14, 0),
        VerticalAlignment = VerticalAlignment.Center
    };

    private static Image PInfoIconCreate() => new()
    {
        Width = 20,
        Height = 20,
        Margin = new Thickness(0, 0, 10, 0),
        Stretch = Stretch.Uniform,
        Source = PIcon.PIconRead("/PAsset/PPanel/PInfo.svg")
    };
}
