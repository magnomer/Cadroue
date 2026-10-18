using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PGroupCardName
{
    private static readonly FontFamily pGroupFontFamily = new("Segoe UI");
    private static readonly Brush pGroupTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));

    private static readonly IReadOnlyDictionary<bool, Func<PGroupCardName, QGroupCard, UIElement>> pGroupNames =
        new Dictionary<bool, Func<PGroupCardName, QGroupCard, UIElement>>
        {
            [true] = (pName, lCard) => pName.PGroupEditBuild(lCard),
            [false] = (pName, lCard) => pName.PGroupLabelBuild(lCard),
        };

    private readonly QGroupName lGroupName;

    public PGroupCardName(QGroupName lName) => lGroupName = lName;

    public UIElement PGroupNameBuild(QGroupCard lCard) => pGroupNames[lCard.QGroupCardEditing](this, lCard);

    private TextBlock PGroupLabelBuild(QGroupCard lCard)
    {
        TextBlock pNameLabel = PGroupLabelCreate(lCard);
        PGroupLabelAttach(pNameLabel, lCard);
        return pNameLabel;
    }

    private static TextBlock PGroupLabelCreate(QGroupCard lCard) => new()
    {
        Text = lCard.QGroupCardName,
        FontSize = 12,
        FontFamily = pGroupFontFamily,
        FontWeight = FontWeights.SemiBold,
        Foreground = pGroupTitleBrush,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis,
        ToolTip = QLocalizationCatalog.QLocalizationTextRead("Group.Rename.Tooltip")
    };

    private void PGroupLabelAttach(TextBlock pNameLabel, QGroupCard lCard) =>
        pNameLabel.MouseLeftButtonDown += (_, pNameEvent) =>
            pNameEvent.Handled = lGroupName.QGroupLabelHandle(lCard.QGroupCardIndex, pNameEvent.ClickCount);

    private TextBox PGroupEditBuild(QGroupCard lCard)
    {
        TextBox pNameBox = PGroupEditCreate(lCard);
        PGroupEditAttach(pNameBox);
        return pNameBox;
    }

    private static TextBox PGroupEditCreate(QGroupCard lCard) => new()
    {
        Text = lCard.QGroupCardName,
        FontSize = 12,
        FontFamily = pGroupFontFamily,
        FontWeight = FontWeights.SemiBold,
        VerticalAlignment = VerticalAlignment.Center,
        Padding = new Thickness(2, 0, 2, 0),
        Margin = new Thickness(0, 0, 6, 0)
    };

    private void PGroupEditAttach(TextBox pNameBox)
    {
        pNameBox.Loaded += (_, _) => PGroupNameSelect(pNameBox);
        pNameBox.KeyDown += (_, pKeyEvent) =>
            pKeyEvent.Handled = lGroupName.QGroupKeyRun(pKeyEvent.Key.ToString(), pNameBox.Text);
        pNameBox.LostKeyboardFocus += (_, _) => lGroupName.QGroupNameCommit(pNameBox.Text);
    }

    private static void PGroupNameSelect(TextBox pNameBox)
    {
        pNameBox.Focus();
        pNameBox.SelectAll();
    }
}
