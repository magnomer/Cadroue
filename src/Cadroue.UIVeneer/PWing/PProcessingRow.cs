using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PProcessingRow
{
    private static readonly FontFamily pProcessingRowFont = new("Segoe UI");
    private static readonly Brush pProcessingRowLine = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pProcessingRowActive = new SolidColorBrush(Color.FromRgb(0x2C, 0x6C, 0xCE));

    private static readonly IReadOnlyDictionary<bool, Brush> pProcessingIconBrushes = new Dictionary<bool, Brush>
    {
        [true] = pProcessingRowActive,
        [false] = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D)),
    };

    private static readonly IReadOnlyDictionary<bool, Brush> pProcessingTextBrushes = new Dictionary<bool, Brush>
    {
        [true] = pProcessingRowActive,
        [false] = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)),
    };

    private static readonly IReadOnlyDictionary<bool, FontWeight> pProcessingRowWeights =
        new Dictionary<bool, FontWeight>
        {
            [true] = FontWeights.SemiBold,
            [false] = FontWeights.Normal,
        };

    private static readonly IReadOnlyDictionary<bool, Brush> pProcessingRowBackgrounds = new Dictionary<bool, Brush>
    {
        [true] = new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB)),
        [false] = Brushes.White,
    };

    private static readonly IReadOnlyDictionary<bool, Cursor> pProcessingRowCursors = new Dictionary<bool, Cursor>
    {
        [true] = Cursors.Hand,
        [false] = Cursors.Arrow,
    };

    private readonly Border pProcessingRowBorder;
    private readonly Border pProcessingRowBadge;
    private readonly TextBlock pProcessingRowNumber;
    private readonly Image pProcessingRowIcon;
    private readonly TextBlock pProcessingRowLabel;
    private readonly string pProcessingIconPath;

    public PProcessingRow(string pIconPath, string pLabelText, Thickness pPadding, Thickness pLine)
    {
        pProcessingIconPath = pIconPath;
        pProcessingRowNumber = new TextBlock
        {
            FontSize = 10,
            FontFamily = pProcessingRowFont,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        pProcessingRowBadge = new Border
        {
            Width = 18,
            Height = 18,
            CornerRadius = new CornerRadius(9),
            Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xEE, 0xF6)),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed,
            Child = pProcessingRowNumber
        };
        pProcessingRowIcon = new Image
        {
            Width = 14,
            Height = 14,
            Source = PIcon.PIconRead(pIconPath, pProcessingIconBrushes[false]),
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        pProcessingRowLabel = new TextBlock
        {
            Text = pLabelText,
            FontSize = 12,
            FontFamily = pProcessingRowFont,
            Foreground = pProcessingTextBrushes[false],
            VerticalAlignment = VerticalAlignment.Center
        };

        var pContent = new StackPanel { Orientation = Orientation.Horizontal };
        pContent.Children.Add(pProcessingRowBadge);
        pContent.Children.Add(pProcessingRowIcon);
        pContent.Children.Add(pProcessingRowLabel);
        pProcessingRowBorder = new Border
        {
            Padding = pPadding,
            Background = Brushes.White,
            BorderBrush = pProcessingRowLine,
            BorderThickness = pLine,
            Cursor = Cursors.Hand,
            Child = pContent
        };
        AutomationProperties.SetName(pProcessingRowBorder, pLabelText);
    }

    public Border PProcessingRowBorder => pProcessingRowBorder;

    public void PProcessingActiveApply(bool pActive)
    {
        pProcessingRowIcon.Source = PIcon.PIconRead(pProcessingIconPath, pProcessingIconBrushes[pActive]);
        pProcessingRowLabel.Foreground = pProcessingTextBrushes[pActive];
        pProcessingRowLabel.FontWeight = pProcessingRowWeights[pActive];
    }

    public void PProcessingNumberApply(string pNumber, bool pShown)
    {
        pProcessingRowNumber.Text = pNumber;
        pProcessingRowBadge.Visibility = PLook.PLookVisible[pShown];
    }

    public void PProcessingSelectApply(bool pSelected) =>
        pProcessingRowBorder.Background = pProcessingRowBackgrounds[pSelected];

    public void PProcessingStateApply(bool pEnabled, double pOpacity, string? pNotice, string pHelp)
    {
        pProcessingRowBorder.IsEnabled = pEnabled;
        pProcessingRowBorder.Opacity = pOpacity;
        pProcessingRowBorder.Cursor = pProcessingRowCursors[pEnabled];
        pProcessingRowBorder.ToolTip = pNotice;
        AutomationProperties.SetHelpText(pProcessingRowBorder, pHelp);
    }
}
