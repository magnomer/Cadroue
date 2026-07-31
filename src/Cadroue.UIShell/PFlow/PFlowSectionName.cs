using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    private bool PFlowNameShow()
    {
        if (lSectionIndexActive is not int pSectionIndex || pSectionIndex >= lSectionList.Count)
        {
            return false;
        }

        PFlowNameClose();

        LSegment pNameSection = lSectionList[pSectionIndex];
        TextBox pNameBox = PFlowNameBuild(pNameSection.LSegmentName, PFlowNameWidth);
        TextBox pPrefixBox = PFlowNameBuild(pNameSection.LSegmentPrefix, PFlowAffixWidth);
        TextBox pSuffixBox = PFlowNameBuild(pNameSection.LSegmentSuffix, PFlowAffixWidth);

        var pFieldPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pFieldPanel.Children.Add(pNameBox);
        pFieldPanel.Children.Add(PFlowAffixBuild(pPrefixBox));
        pFieldPanel.Children.Add(pPrefixBox);
        pFieldPanel.Children.Add(PFlowAffixBuild(pSuffixBox));
        pFieldPanel.Children.Add(pSuffixBox);

        PFlowAffixShow(pPrefixBox, !string.IsNullOrEmpty(pNameSection.LSegmentPrefix));
        PFlowAffixShow(pSuffixBox, !string.IsNullOrEmpty(pNameSection.LSegmentSuffix));

        PFlowStepAttach(pNameBox, pPrefixBox);
        PFlowStepAttach(pPrefixBox, pSuffixBox);
        PFlowStepAttach(pSuffixBox, null);

        Rect pSectionRect = pViewfinder.PViewfinderSectionRead(pSectionIndex);
        var pNamePopup = new Popup
        {
            PlacementTarget = pViewfinder,
            Placement = PlacementMode.Center,
            HorizontalOffset = pSectionRect.IsEmpty ? 0 : pSectionRect.Left + pSectionRect.Width / 2 - pViewfinder.ActualWidth / 2,
            VerticalOffset = pSectionRect.IsEmpty ? 0 : pSectionRect.Top + pSectionRect.Height / 2 - pViewfinder.ActualHeight / 2,
            StaysOpen = false,
            AllowsTransparency = true,
            Child = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xD7, 0xDF, 0xEA)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Child = pFieldPanel
            }
        };

        void PFlowNameKeyHandle(object pSender, KeyEventArgs pNameKeyEvent)
        {
            switch (pNameKeyEvent.Key)
            {
                case Key.Enter:
                    PFlowNameApply(pSectionIndex, pNameBox.Text, pPrefixBox.Text, pSuffixBox.Text);
                    PFlowNameClose();
                    pNameKeyEvent.Handled = true;
                    break;
                case Key.Escape:
                    PFlowNameClose();
                    pNameKeyEvent.Handled = true;
                    break;
            }
        }

        pNameBox.KeyDown += PFlowNameKeyHandle;
        pPrefixBox.KeyDown += PFlowNameKeyHandle;
        pSuffixBox.KeyDown += PFlowNameKeyHandle;

        pFlowNamePopup = pNamePopup;
        pNamePopup.IsOpen = true;
        pNameBox.Focus();
        Keyboard.Focus(pNameBox);
        pNameBox.SelectAll();
        return true;
    }

    private static TextBox PFlowNameBuild(string pFieldText, double pFieldWidth)
    {
        var pFieldBox = new TextBox
        {
            Width = pFieldWidth,
            Height = PFlowNameHeight,
            Text = pFieldText,
            FontSize = PSection.PSectionNameSize,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        PTextbox.PTextboxApply(pFieldBox);
        return pFieldBox;
    }

    private static UIElement PFlowAffixBuild(TextBox pAffixBox)
    {
        var pSeparator = new TextBlock
        {
            Text = "/",
            Margin = new Thickness(6, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E)),
            Visibility = Visibility.Collapsed
        };
        pAffixBox.Tag = pSeparator;
        return pSeparator;
    }

    private static void PFlowAffixShow(TextBox pAffixBox, bool pAffixVisible)
    {
        pAffixBox.Visibility = pAffixVisible ? Visibility.Visible : Visibility.Collapsed;
        if (pAffixBox.Tag is UIElement pSeparator)
        {
            pSeparator.Visibility = pAffixBox.Visibility;
        }
    }

    private static void PFlowStepAttach(TextBox pFieldBox, TextBox? pNextBox)
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

            PFlowAffixShow(pNextBox, true);
            pNextBox.Focus();
            Keyboard.Focus(pNextBox);
            pNextBox.SelectAll();
        };
    }

    private void PFlowNameClose()
    {
        if (pFlowNamePopup is null)
        {
            return;
        }

        pFlowNamePopup.IsOpen = false;
        pFlowNamePopup = null;
    }

    private void PFlowNameApply(int pSectionIndex, string pSectionName, string pSectionPrefix, string pSectionSuffix)
    {
        PFlowNameSet(pSectionIndex, pSectionName.Trim(), pSectionPrefix.Trim(), pSectionSuffix.Trim());
    }
}
