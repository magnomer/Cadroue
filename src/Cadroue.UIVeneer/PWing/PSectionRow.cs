using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PSectionRow
{
    private const double PSectionBadgeSize = 18;
    private const double PSectionBadgeRadius = 9;
    private const double PSectionBadgePadding = 6;
    private const double PSectionAffixWidth = 62;
    private const string PSectionArrowText = " → ";

    private static readonly FontFamily pSectionFontFamily = new("Segoe UI");
    private static readonly Brush pSectionTextBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly Brush pSectionMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pSectionTimeBrush = new SolidColorBrush(Color.FromRgb(0x56, 0x62, 0x73));
    private static readonly Brush pSectionLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pSectionSelectBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB));
    private static readonly Brush pSectionFieldBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9));

    private static readonly IReadOnlyDictionary<bool, Brush> pSectionBackgrounds = new Dictionary<bool, Brush>
    {
        [true] = pSectionSelectBrush,
        [false] = Brushes.White,
    };

    private static readonly IReadOnlyDictionary<bool, Brush> pSectionNameBrushes = new Dictionary<bool, Brush>
    {
        [true] = pSectionMutedBrush,
        [false] = pSectionTextBrush,
    };

    private static readonly IReadOnlyDictionary<bool, Func<PSectionRow, LSectionRow, UIElement>> pSectionNames =
        new Dictionary<bool, Func<PSectionRow, LSectionRow, UIElement>>
        {
            [true] = (pRow, lRow) => pRow.PSectionEditorBuild(lRow),
            [false] = (pRow, lRow) => pRow.PSectionTextBuild(lRow),
        };

    private static readonly IReadOnlyDictionary<bool, Action<PSection>> pSectionReleases =
        new Dictionary<bool, Action<PSection>>
        {
            [true] = pPanel => pPanel.PSectionCaptureRelease(),
            [false] = pPanel => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<PSection>> pSectionCaptures =
        new Dictionary<bool, Action<PSection>>
        {
            [true] = pPanel => pPanel.PSectionCaptureClaim(),
            [false] = pPanel => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<Action>> pSectionSteps =
        new Dictionary<bool, Action<Action>>
        {
            [true] = pNext => pNext(),
            [false] = pNext => { },
        };

    private readonly PSection pSectionPanel;
    private readonly LSection lSection;
    private readonly TextBlock pSectionBadgeText;
    private readonly List<TextBox> pSectionBoxes = [];

    public PSectionRow(PSection pPanel, LSectionRow lRow)
    {
        pSectionPanel = pPanel;
        lSection = pPanel.LSection;
        pSectionBadgeText = new TextBlock
        {
            Text = lRow.LSectionRowNumber,
            FontSize = PSection.PSectionNameSize,
            FontFamily = pSectionFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        var pColorDot = new Border
        {
            MinWidth = PSectionBadgeSize,
            Height = PSectionBadgeSize,
            CornerRadius = new CornerRadius(PSectionBadgeRadius),
            Background = PSectionPalette.PSectionBadgeRead(lRow.LSectionRowColor),
            Padding = new Thickness(PSectionBadgePadding, 0, PSectionBadgePadding, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            ToolTip = LLocalization.LLocalizationTextRead("Section.Toggle.Tooltip"),
            Child = pSectionBadgeText
        };
        pColorDot.MouseLeftButtonDown += PSectionToggleHandle;

        UIElement pNameHost = pSectionNames[lRow.LSectionRowEditing](this, lRow);

        TextBlock pBeginLabel = PSectionTimeBuild(lRow.LSectionRowOrigin);
        pBeginLabel.Margin = new Thickness(8, 0, 0, 0);
        pBeginLabel.MouseLeftButtonDown += PSectionOriginHandle;

        TextBlock pEndLabel = PSectionTimeBuild(lRow.LSectionRowEnd);
        pEndLabel.MouseLeftButtonDown += PSectionEndHandle;

        var pTimeLabel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pTimeLabel.Children.Add(pBeginLabel);
        pTimeLabel.Children.Add(PSectionTimeBuild(PSectionArrowText));
        pTimeLabel.Children.Add(pEndLabel);
        pTimeLabel.Children.Add(PSectionTimeBuild(lRow.LSectionRowSpan));

        var pRowContent = new Grid { Opacity = PLook.PLookOpacity[!lRow.LSectionRowHidden] };
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pColorDot, 0);
        Grid.SetColumn(pNameHost, 1);
        Grid.SetColumn(pTimeLabel, 2);
        pRowContent.Children.Add(pColorDot);
        pRowContent.Children.Add(pNameHost);
        pRowContent.Children.Add(pTimeLabel);

        PSectionRowBorder = new Border
        {
            Padding = new Thickness(12, 7, 12, 7),
            Background = pSectionBackgrounds[lRow.LSectionRowSelected],
            BorderBrush = pSectionLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Cursor = Cursors.Hand,
            Child = pRowContent,
            Tag = this
        };
        PSectionRowBorder.PreviewMouseLeftButtonDown += PSectionPressHandle;
        PSectionRowBorder.MouseLeftButtonDown += PSectionOriginHandle;
    }

    public Border PSectionRowBorder { get; }

    public void PSectionSelectedSet(bool lSelected) => PSectionRowBorder.Background = pSectionBackgrounds[lSelected];

    public void PSectionNumberSet(string lNumber) => pSectionBadgeText.Text = lNumber;

    private int PSectionIndexRead() => pSectionPanel.PSectionIndexRead(this);

    private void PSectionPressHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        Point pOrigin = pSectionPanel.PSectionPointRead(pEvent);
        Point pGrab = pEvent.GetPosition(PSectionRowBorder);
        bool pCapture = lSection.LSectionDrag.LSectionPressHandle(
            PSectionIndexRead(), pEvent.ClickCount, pOrigin.X, pOrigin.Y, pGrab.X, pGrab.Y);
        pSectionCaptures[pCapture](pSectionPanel);
    }

    private void PSectionToggleHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        pSectionPanel.PSectionCaptureRelease();
        lSection.LSectionToggleHandle(PSectionIndexRead());
        pEvent.Handled = true;
    }

    private void PSectionOriginHandle(object pSender, MouseButtonEventArgs pEvent) => PSectionSeekHandle(false, pEvent);

    private void PSectionEndHandle(object pSender, MouseButtonEventArgs pEvent) => PSectionSeekHandle(true, pEvent);

    private void PSectionSeekHandle(bool pSeekEnd, MouseButtonEventArgs pEvent)
    {
        bool pSeek = lSection.LSectionSeekHandle(PSectionIndexRead(), pEvent.ClickCount, pSeekEnd);
        pSectionReleases[pSeek](pSectionPanel);
        pEvent.Handled = pSeek;
    }

    private void PSectionRenameHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        bool pRename = lSection.LSectionRenameHandle(PSectionIndexRead(), pEvent.ClickCount);
        pSectionReleases[pRename](pSectionPanel);
        pEvent.Handled = pRename;
    }

    private TextBlock PSectionTextBuild(LSectionRow lRow)
    {
        var pNameText = new TextBlock
        {
            FontSize = PSection.PSectionNameSize,
            FontFamily = pSectionFontFamily,
            Foreground = pSectionTextBrush,
            Padding = new Thickness(2, 0, 2, 1),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        pNameText.Inlines.Add(
            new Run(lRow.LSectionRowText) { Foreground = pSectionNameBrushes[lRow.LSectionRowUnnamed] });
        lRow.LSectionRowAffixes.Select(PSectionAffixBuild).ToList().ForEach(pNameText.Inlines.Add);
        pNameText.MouseLeftButtonDown += PSectionRenameHandle;
        return pNameText;
    }

    private static Run PSectionAffixBuild(string lAffix) => new(lAffix) { Foreground = pSectionMutedBrush };

    private UIElement PSectionEditorBuild(LSectionRow lRow)
    {
        int pSerial = lSection.LSectionEditSerial;
        TextBox pNameBox = PSectionFieldBuild(lRow.LSectionRowName, double.NaN, pSerial);
        TextBox pPrefixBox = PSectionFieldBuild(lRow.LSectionRowPrefix, PSectionAffixWidth, pSerial);
        TextBox pSuffixBox = PSectionFieldBuild(lRow.LSectionRowSuffix, PSectionAffixWidth, pSerial);
        TextBlock pPrefixMark = PSectionMarkBuild();
        TextBlock pSuffixMark = PSectionMarkBuild();
        PSectionAffixShow(pPrefixBox, pPrefixMark, lRow.LSectionRowPrefixed);
        PSectionAffixShow(pSuffixBox, pSuffixMark, lRow.LSectionRowSuffixed);

        Action pTextSync = () => lSection.LSectionTextSet(pNameBox.Text, pPrefixBox.Text, pSuffixBox.Text);
        pSectionBoxes.ForEach(pBox => pBox.TextChanged += (_, _) => pTextSync());
        PSectionStepAttach(pNameBox, () => PSectionAffixSelect(pPrefixBox, pPrefixMark));
        PSectionStepAttach(pPrefixBox, () => PSectionAffixSelect(pSuffixBox, pSuffixMark));
        PSectionStepAttach(pSuffixBox, () => { });

        var pEditorPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pEditorPanel.Children.Add(pNameBox);
        pEditorPanel.Children.Add(pPrefixMark);
        pEditorPanel.Children.Add(pPrefixBox);
        pEditorPanel.Children.Add(pSuffixMark);
        pEditorPanel.Children.Add(pSuffixBox);

        pNameBox.Loaded += (_, _) =>
        {
            pNameBox.Focus();
            pNameBox.SelectAll();
        };
        return pEditorPanel;
    }

    private TextBox PSectionFieldBuild(string pFieldText, double pFieldWidth, int pSerial)
    {
        var pFieldBox = new TextBox
        {
            Text = pFieldText,
            MinWidth = 24,
            FontSize = PSection.PSectionNameSize,
            FontFamily = pSectionFontFamily,
            Foreground = pSectionTextBrush,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = pSectionFieldBrush,
            Padding = new Thickness(2, 0, 2, 1),
            VerticalAlignment = VerticalAlignment.Center,
            FocusVisualStyle = null
        };
        pFieldBox.Width = pFieldWidth;
        pFieldBox.LostFocus += (_, _) => PSectionEditClose(pSerial);
        pFieldBox.KeyDown += (_, pEvent) => pEvent.Handled = lSection.LSectionKeyRun(pEvent.Key.ToString());
        pSectionBoxes.Add(pFieldBox);
        return pFieldBox;
    }

    private static TextBlock PSectionMarkBuild() => new()
    {
        Text = "/",
        Margin = new Thickness(5, 0, 5, 0),
        FontSize = PSection.PSectionNameSize,
        FontFamily = pSectionFontFamily,
        Foreground = pSectionMutedBrush,
        VerticalAlignment = VerticalAlignment.Center,
        Visibility = Visibility.Collapsed
    };

    private static void PSectionAffixShow(TextBox pAffixBox, TextBlock pMark, bool pAffixVisible)
    {
        pAffixBox.Visibility = PLook.PLookVisible[pAffixVisible];
        pMark.Visibility = PLook.PLookVisible[pAffixVisible];
    }

    private static void PSectionAffixSelect(TextBox pNextBox, TextBlock pMark)
    {
        PSectionAffixShow(pNextBox, pMark, true);
        pNextBox.Focus();
        Keyboard.Focus(pNextBox);
        pNextBox.SelectAll();
    }

    private static void PSectionStepAttach(TextBox pFieldBox, Action pNext)
    {
        pFieldBox.PreviewTextInput += (_, pFieldEvent) =>
        {
            pFieldEvent.Handled = LSection.LSectionStepCheck(pFieldEvent.Text);
            pSectionSteps[pFieldEvent.Handled](pNext);
        };
    }

    private void PSectionEditClose(int pSerial) =>
        pSectionPanel.Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() => lSection.LSectionBlurHandle(
                pSerial, pSectionBoxes.Contains((Keyboard.FocusedElement as TextBox)!))));

    private static TextBlock PSectionTimeBuild(string pTimeText) => new()
    {
        Text = pTimeText,
        FontSize = 11,
        FontFamily = pSectionFontFamily,
        Foreground = pSectionTimeBrush,
        Background = Brushes.Transparent,
        VerticalAlignment = VerticalAlignment.Center
    };
}
