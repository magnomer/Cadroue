using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIVeneer.PBench;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PSection
{
    private const double PSectionAffixWidth = 62;

    private TextBox? pSectionNameBox;
    private TextBox? pSectionPrefixBox;
    private TextBox? pSectionSuffixBox;

    private void PSectionEditCommit()
    {
        if (LSection.LSectionEditIndex is not int pEditingIndex || pSectionNameBox is not { } pEditingBox)
        {
            return;
        }

        string pEditingName = pEditingBox.Text.Trim();
        string pEditingPrefix = pSectionPrefixBox?.Text.Trim() ?? string.Empty;
        string pEditingSuffix = pSectionSuffixBox?.Text.Trim() ?? string.Empty;

        LSection.LSectionEditSet(null);
        pSectionNameBox = null;
        pSectionPrefixBox = null;
        pSectionSuffixBox = null;
        pFlowAttached?.LFlow.LFlowSection.LFlowNameSet(pEditingIndex, pEditingName, pEditingPrefix, pEditingSuffix);
        PSectionRebuild();
    }

    private UIElement PSectionEditorBuild(LPiece pSectionEntry)
    {
        TextBox pFlowNameBox = PSectionFieldBuild(pSectionEntry.LPieceName, 0);
        TextBox pFlowPrefixBox = PSectionFieldBuild(pSectionEntry.LPiecePrefix, PSectionAffixWidth);
        TextBox pFlowSuffixBox = PSectionFieldBuild(pSectionEntry.LPieceSuffix, PSectionAffixWidth);
        pSectionNameBox = pFlowNameBox;
        pSectionPrefixBox = pFlowPrefixBox;
        pSectionSuffixBox = pFlowSuffixBox;

        UIElement pPrefixMark = PSectionMarkBuild(pFlowPrefixBox);
        UIElement pSuffixMark = PSectionMarkBuild(pFlowSuffixBox);
        PSectionAffixShow(pFlowPrefixBox, !string.IsNullOrEmpty(pSectionEntry.LPiecePrefix));
        PSectionAffixShow(pFlowSuffixBox, !string.IsNullOrEmpty(pSectionEntry.LPieceSuffix));

        PSectionStepAttach(pFlowNameBox, pFlowPrefixBox);
        PSectionStepAttach(pFlowPrefixBox, pFlowSuffixBox);
        PSectionStepAttach(pFlowSuffixBox, null);

        var pEditorPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pEditorPanel.Children.Add(pFlowNameBox);
        pEditorPanel.Children.Add(pPrefixMark);
        pEditorPanel.Children.Add(pFlowPrefixBox);
        pEditorPanel.Children.Add(pSuffixMark);
        pEditorPanel.Children.Add(pFlowSuffixBox);

        pFlowNameBox.Loaded += (_, _) =>
        {
            pFlowNameBox.Focus();
            pFlowNameBox.SelectAll();
        };
        return pEditorPanel;
    }

    private TextBox PSectionFieldBuild(string pFieldText, double pFieldWidth)
    {
        var pFieldBox = new TextBox
        {
            Text = pFieldText,
            MinWidth = 24,
            FontSize = PSectionNameSize,
            FontFamily = pSectionFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9)),
            Padding = new Thickness(2, 0, 2, 1),
            VerticalAlignment = VerticalAlignment.Center,
            FocusVisualStyle = null
        };

        if (pFieldWidth > 0)
        {
            pFieldBox.Width = pFieldWidth;
        }

        pFieldBox.LostFocus += (_, _) => PSectionEditClose(pFieldBox);
        pFieldBox.KeyDown += (_, pEvent) =>
        {
            if (pEvent.Key == Key.Return)
            {
                PSectionEditCommit();
                pEvent.Handled = true;
            }
            else if (pEvent.Key == Key.Escape)
            {
                PSectionEditCancel();
                pEvent.Handled = true;
            }
        };
        return pFieldBox;
    }

    private static UIElement PSectionMarkBuild(TextBox pAffixBox)
    {
        var pMark = new TextBlock
        {
            Text = "/",
            Margin = new Thickness(5, 0, 5, 0),
            FontSize = PSectionNameSize,
            FontFamily = pSectionFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E)),
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed
        };
        pAffixBox.Tag = pMark;
        return pMark;
    }

    private static void PSectionAffixShow(TextBox pAffixBox, bool pAffixVisible)
    {
        pAffixBox.Visibility = pAffixVisible ? Visibility.Visible : Visibility.Collapsed;
        if (pAffixBox.Tag is UIElement pMark)
        {
            pMark.Visibility = pAffixBox.Visibility;
        }
    }

    private static void PSectionStepAttach(TextBox pFieldBox, TextBox? pNextBox)
    {
        pFieldBox.PreviewTextInput += (_, pFieldEvent) =>
        {
            if (pFieldEvent.Text != ",")
            {
                return;
            }

            pFieldEvent.Handled = true;
            if (pNextBox is null)
            {
                return;
            }

            PSectionAffixShow(pNextBox, true);
            pNextBox.Focus();
            Keyboard.Focus(pNextBox);
            pNextBox.SelectAll();
        };
    }

    private void PSectionEditClose(TextBox pFieldBox)
    {
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
        {
            if (!PSectionFieldCheck(pFieldBox) || PSectionFocusCheck())
            {
                return;
            }

            PSectionEditCommit();
        }));
    }

    private bool PSectionFieldCheck(TextBox pFieldBox) =>
        ReferenceEquals(pFieldBox, pSectionNameBox)
        || ReferenceEquals(pFieldBox, pSectionPrefixBox)
        || ReferenceEquals(pFieldBox, pSectionSuffixBox);

    private bool PSectionFocusCheck()
    {
        return ReferenceEquals(Keyboard.FocusedElement, pSectionNameBox)
            || ReferenceEquals(Keyboard.FocusedElement, pSectionPrefixBox)
            || ReferenceEquals(Keyboard.FocusedElement, pSectionSuffixBox);
    }

    private void PSectionEditCancel()
    {
        LSection.LSectionEditSet(null);
        pSectionNameBox = null;
        pSectionPrefixBox = null;
        pSectionSuffixBox = null;
        PSectionRebuild();
    }
}
