using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PFlow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PSection
{
    private const double PSectionAffixWidth = 62;

    private int? pSectionIndexEditing;
    private TextBox? pSectionNameBox;
    private TextBox? pSectionPrefixBox;
    private TextBox? pSectionSuffixBox;

    private void PSectionEditCommit()
    {
        if (pSectionIndexEditing is not int pEditingIndex || pSectionNameBox is not { } pEditingBox)
        {
            return;
        }

        string pEditingName = pEditingBox.Text.Trim();
        string pEditingPrefix = pSectionPrefixBox?.Text.Trim() ?? string.Empty;
        string pEditingSuffix = pSectionSuffixBox?.Text.Trim() ?? string.Empty;

        pSectionIndexEditing = null;
        pSectionNameBox = null;
        pSectionPrefixBox = null;
        pSectionSuffixBox = null;
        pFlowAttached?.PFlowNameSet(pEditingIndex, pEditingName, pEditingPrefix, pEditingSuffix);
        PSectionRebuild();
    }

    private UIElement PSectionEditorBuild(LPiece pSectionEntry)
    {
        TextBox pNameBox = PSectionFieldBuild(pSectionEntry.LPieceName, 0);
        TextBox pPrefixBox = PSectionFieldBuild(pSectionEntry.LPiecePrefix, PSectionAffixWidth);
        TextBox pSuffixBox = PSectionFieldBuild(pSectionEntry.LPieceSuffix, PSectionAffixWidth);
        pSectionNameBox = pNameBox;
        pSectionPrefixBox = pPrefixBox;
        pSectionSuffixBox = pSuffixBox;

        UIElement pPrefixMark = PSectionMarkBuild(pPrefixBox);
        UIElement pSuffixMark = PSectionMarkBuild(pSuffixBox);
        PSectionAffixShow(pPrefixBox, !string.IsNullOrEmpty(pSectionEntry.LPiecePrefix));
        PSectionAffixShow(pSuffixBox, !string.IsNullOrEmpty(pSectionEntry.LPieceSuffix));

        PSectionStepAttach(pNameBox, pPrefixBox);
        PSectionStepAttach(pPrefixBox, pSuffixBox);
        PSectionStepAttach(pSuffixBox, null);

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

        pFieldBox.LostFocus += (_, _) => PSectionEditClose();
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

    private void PSectionEditClose()
    {
        if (pSectionRebuilding)
        {
            return;
        }

        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
        {
            if (pSectionRebuilding || PSectionFocusCheck())
            {
                return;
            }

            PSectionEditCommit();
        }));
    }

    private bool PSectionFocusCheck()
    {
        return ReferenceEquals(Keyboard.FocusedElement, pSectionNameBox)
            || ReferenceEquals(Keyboard.FocusedElement, pSectionPrefixBox)
            || ReferenceEquals(Keyboard.FocusedElement, pSectionSuffixBox);
    }

    private void PSectionEditCancel()
    {
        pSectionIndexEditing = null;
        pSectionNameBox = null;
        pSectionPrefixBox = null;
        pSectionSuffixBox = null;
        PSectionRebuild();
    }
}
