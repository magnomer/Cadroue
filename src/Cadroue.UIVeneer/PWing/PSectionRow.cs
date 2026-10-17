using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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

    private static readonly IReadOnlyDictionary<bool, Func<PSectionRow, QSectionRow, UIElement>> pSectionNames =
        new Dictionary<bool, Func<PSectionRow, QSectionRow, UIElement>>
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
    private readonly QSection lSection;
    private readonly TextBlock pSectionBadgeText;
    private readonly List<TextBox> pSectionBoxes = [];

    public PSectionRow(PSection pPanel, QSectionRow lRow)
    {
        pSectionPanel = pPanel;
        lSection = pPanel.QSection;
        pSectionBadgeText = PSectionBadgeBuild(lRow.QSectionRowNumber);
        Border pColorDot = PSectionDotBuild(lRow);
        UIElement pNameHost = PSectionNameBuild(lRow);
        TextBlock pBeginLabel = PSectionTimeBuild(lRow.QSectionRowOrigin);
        TextBlock pEndLabel = PSectionTimeBuild(lRow.QSectionRowEnd);
        StackPanel pTimeLabel = PSectionSpanBuild(lRow, pBeginLabel, pEndLabel);
        PSectionRowBorder = PSectionBorderBuild(lRow, pColorDot, pNameHost, pTimeLabel);
        PSectionRowAttach(pColorDot, pBeginLabel, pEndLabel);
    }

    private static TextBlock PSectionBadgeBuild(string lNumber) => new()
    {
        Text = lNumber,
        FontSize = PSection.PSectionNameSize,
        FontFamily = pSectionFontFamily,
        FontWeight = FontWeights.SemiBold,
        Foreground = Brushes.White,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        TextAlignment = TextAlignment.Center
    };

    private Border PSectionDotBuild(QSectionRow lRow) => new()
    {
        MinWidth = PSectionBadgeSize,
        Height = PSectionBadgeSize,
        CornerRadius = new CornerRadius(PSectionBadgeRadius),
        Background = PSectionPalette.PSectionBadgeRead(lRow.QSectionRowColor),
        Padding = new Thickness(PSectionBadgePadding, 0, PSectionBadgePadding, 0),
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 8, 0),
        Cursor = Cursors.Hand,
        ToolTip = QLocalizationCatalog.QLocalizationTextRead("Section.Toggle.Tooltip"),
        Child = pSectionBadgeText
    };

    private UIElement PSectionNameBuild(QSectionRow lRow) => pSectionNames[lRow.QSectionRowEditing](this, lRow);

    private static StackPanel PSectionSpanBuild(QSectionRow lRow, TextBlock pBeginLabel, TextBlock pEndLabel)
    {
        pBeginLabel.Margin = new Thickness(8, 0, 0, 0);
        var pTimeLabel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pTimeLabel.Children.Add(pBeginLabel);
        pTimeLabel.Children.Add(PSectionTimeBuild(PSectionArrowText));
        pTimeLabel.Children.Add(pEndLabel);
        pTimeLabel.Children.Add(PSectionTimeBuild(lRow.QSectionRowSpan));
        return pTimeLabel;
    }

    private Border PSectionBorderBuild(QSectionRow lRow, Border pColorDot, UIElement pNameHost, StackPanel pTimeLabel)
    {
        var pRowContent = new Grid { Opacity = PLook.PLookOpacity[!lRow.QSectionRowHidden] };
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pColorDot, 0);
        Grid.SetColumn(pNameHost, 1);
        Grid.SetColumn(pTimeLabel, 2);
        pRowContent.Children.Add(pColorDot);
        pRowContent.Children.Add(pNameHost);
        pRowContent.Children.Add(pTimeLabel);

        return new Border
        {
            Padding = new Thickness(12, 5, 12, 5),
            Background = pSectionBackgrounds[lRow.QSectionRowSelected],
            BorderBrush = pSectionLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Cursor = Cursors.Hand,
            Child = pRowContent,
            Tag = this
        };
    }

    private void PSectionRowAttach(Border pColorDot, TextBlock pBeginLabel, TextBlock pEndLabel)
    {
        pColorDot.MouseLeftButtonDown += PSectionToggleHandle;
        pBeginLabel.MouseLeftButtonDown += PSectionOriginHandle;
        pEndLabel.MouseLeftButtonDown += PSectionEndHandle;
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
        bool pCapture = lSection.QSectionDrag.QSectionPressHandle(
            PSectionIndexRead(), pEvent.ClickCount, pOrigin.X, pOrigin.Y, pGrab.X, pGrab.Y);
        pSectionCaptures[pCapture](pSectionPanel);
    }

    private void PSectionToggleHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        pSectionPanel.PSectionCaptureRelease();
        lSection.QSectionToggleHandle(PSectionIndexRead());
        pEvent.Handled = true;
    }

    private void PSectionOriginHandle(object pSender, MouseButtonEventArgs pEvent) => PSectionSeekHandle(false, pEvent);

    private void PSectionEndHandle(object pSender, MouseButtonEventArgs pEvent) => PSectionSeekHandle(true, pEvent);

    private void PSectionSeekHandle(bool pSeekEnd, MouseButtonEventArgs pEvent)
    {
        bool pSeek = lSection.QSectionSeekHandle(PSectionIndexRead(), pEvent.ClickCount, pSeekEnd);
        pSectionReleases[pSeek](pSectionPanel);
        pEvent.Handled = pSeek;
    }

    private void PSectionRenameHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        bool pRename = lSection.QSectionRenameHandle(PSectionIndexRead(), pEvent.ClickCount);
        pSectionReleases[pRename](pSectionPanel);
        pEvent.Handled = pRename;
    }

    private TextBlock PSectionTextBuild(QSectionRow lRow)
    {
        TextBlock pNameText = PSectionTitleBuild(lRow);
        PSectionRenameAttach(pNameText);
        return pNameText;
    }

    private static TextBlock PSectionTitleBuild(QSectionRow lRow)
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
            new Run(lRow.QSectionRowText) { Foreground = pSectionNameBrushes[lRow.QSectionRowUnnamed] });
        lRow.QSectionRowAffixes.Select(PSectionAffixBuild).ToList().ForEach(pNameText.Inlines.Add);
        return pNameText;
    }

    private void PSectionRenameAttach(TextBlock pNameText) => pNameText.MouseLeftButtonDown += PSectionRenameHandle;

    private static Run PSectionAffixBuild(string lAffix) => new(lAffix) { Foreground = pSectionMutedBrush };

    private UIElement PSectionEditorBuild(QSectionRow lRow)
    {
        int pSerial = lSection.QSectionEditSerial;
        TextBox pNameBox = PSectionFieldBuild(lRow.QSectionRowName, double.NaN, pSerial);
        TextBox pPrefixBox = PSectionFieldBuild(lRow.QSectionRowPrefix, PSectionAffixWidth, pSerial);
        TextBox pSuffixBox = PSectionFieldBuild(lRow.QSectionRowSuffix, PSectionAffixWidth, pSerial);
        TextBlock pPrefixMark = PSectionMarkBuild();
        TextBlock pSuffixMark = PSectionMarkBuild();
        PSectionAffixShow(pPrefixBox, pPrefixMark, lRow.QSectionRowPrefixed);
        PSectionAffixShow(pSuffixBox, pSuffixMark, lRow.QSectionRowSuffixed);
        PSectionEditorAttach(pNameBox, pPrefixBox, pSuffixBox, pPrefixMark, pSuffixMark);
        return PSectionInputBuild(pNameBox, pPrefixMark, pPrefixBox, pSuffixMark, pSuffixBox);
    }

    private void PSectionEditorAttach(
        TextBox pNameBox, TextBox pPrefixBox, TextBox pSuffixBox, TextBlock pPrefixMark, TextBlock pSuffixMark)
    {
        Action pTextSync = () => lSection.QSectionTextSet(pNameBox.Text, pPrefixBox.Text, pSuffixBox.Text);
        pSectionBoxes.ForEach(pBox => pBox.TextChanged += (_, _) => pTextSync());
        PSectionStepAttach(pNameBox, () => PSectionAffixSelect(pPrefixBox, pPrefixMark));
        PSectionStepAttach(pPrefixBox, () => PSectionAffixSelect(pSuffixBox, pSuffixMark));
        PSectionStepAttach(pSuffixBox, () => { });
        pNameBox.Loaded += (_, _) => PSectionNameSelect(pNameBox);
    }

    private static StackPanel PSectionInputBuild(
        TextBox pNameBox, TextBlock pPrefixMark, TextBox pPrefixBox, TextBlock pSuffixMark, TextBox pSuffixBox)
    {
        var pEditorPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pEditorPanel.Children.Add(pNameBox);
        pEditorPanel.Children.Add(pPrefixMark);
        pEditorPanel.Children.Add(pPrefixBox);
        pEditorPanel.Children.Add(pSuffixMark);
        pEditorPanel.Children.Add(pSuffixBox);
        return pEditorPanel;
    }

    private static void PSectionNameSelect(TextBox pNameBox)
    {
        pNameBox.Focus();
        pNameBox.SelectAll();
    }

    private TextBox PSectionFieldBuild(string pFieldText, double pFieldWidth, int pSerial)
    {
        TextBox pFieldBox = PSectionBoxBuild(pFieldText, pFieldWidth);
        PSectionBoxAttach(pFieldBox, pSerial);
        return pFieldBox;
    }

    private TextBox PSectionBoxBuild(string pFieldText, double pFieldWidth)
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
        pSectionBoxes.Add(pFieldBox);
        return pFieldBox;
    }

    private void PSectionBoxAttach(TextBox pFieldBox, int pSerial)
    {
        pFieldBox.LostFocus += (_, _) => PSectionEditClose(pSerial);
        pFieldBox.KeyDown += PSectionKeyHandle;
    }

    private void PSectionKeyHandle(object pSender, KeyEventArgs pEvent) =>
        pEvent.Handled = lSection.QSectionKeyRun(pEvent.Key.ToString());

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
        PSectionBoxSelect(pNextBox);
    }

    private static void PSectionBoxSelect(TextBox pNextBox)
    {
        pNextBox.Focus();
        Keyboard.Focus(pNextBox);
        pNextBox.SelectAll();
    }

    private static void PSectionStepAttach(TextBox pFieldBox, Action pNext) =>
        pFieldBox.PreviewTextInput += (_, pFieldEvent) => PSectionStepHandle(pFieldEvent, pNext);

    private static void PSectionStepHandle(TextCompositionEventArgs pFieldEvent, Action pNext)
    {
        pFieldEvent.Handled = QSection.QSectionStepCheck(pFieldEvent.Text);
        pSectionSteps[pFieldEvent.Handled](pNext);
    }

    private void PSectionEditClose(int pSerial) =>
        pSectionPanel.Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() => lSection.QSectionBlurHandle(
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
