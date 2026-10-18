using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PSectionEditor
{
    private const double PSectionAffixWidth = 62;

    private static readonly FontFamily pSectionFontFamily = new("Segoe UI");
    private static readonly Brush pSectionTextBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly Brush pSectionMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pSectionFieldBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9));

    private static readonly IReadOnlyDictionary<bool, Action<Action>> pSectionSteps =
        new Dictionary<bool, Action<Action>>
        {
            [true] = pNext => pNext(),
            [false] = pNext => { },
        };

    private readonly QSection lSection;
    private readonly Dispatcher pSectionDispatcher;
    private readonly List<TextBox> pSectionBoxes = [];

    public PSectionEditor(QSection lSectionOwner, Dispatcher pDispatcher)
    {
        lSection = lSectionOwner;
        pSectionDispatcher = pDispatcher;
    }

    public UIElement PSectionEditorBuild(QSectionRow lRow)
    {
        int pSerial = lSection.QSectionEdit.QSectionEditSerial;
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
        Action pTextSync = () =>
            lSection.QSectionEdit.QSectionTextSet(pNameBox.Text, pPrefixBox.Text, pSuffixBox.Text);
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
        pEvent.Handled = lSection.QSectionEdit.QSectionKeyRun(pEvent.Key.ToString());

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
        pSectionDispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() => lSection.QSectionEdit.QSectionBlurHandle(
                pSerial, pSectionBoxes.Contains((Keyboard.FocusedElement as TextBox)!))));
}
