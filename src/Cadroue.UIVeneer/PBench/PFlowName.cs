using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PBench;

internal sealed class PFlowName
{
    private const double PFlowNameHeight = 32;
    private const double PFlowNameWidth = 220;
    private const double PFlowAffixWidth = 96;

    private static readonly IReadOnlyDictionary<Key, bool> pFlowNameHandled = new Dictionary<Key, bool>
    {
        [Key.Enter] = true,
        [Key.Escape] = true,
    };

    private static readonly IReadOnlyDictionary<string, bool> pFlowStepHandled = new Dictionary<string, bool>
    {
        [","] = true,
    };

    private readonly IReadOnlyDictionary<Key, Action> pFlowNameKeys;
    private readonly IReadOnlyDictionary<string, Action<object>> pFlowStepKeys;
    private readonly IReadOnlyDictionary<object, Action> pFlowSteps;
    private readonly LFlow lFlow;
    private readonly PViewfinder pViewfinder;
    private readonly TextBox pFlowNameBox = PFlowNameBuild(PFlowNameWidth);
    private readonly TextBox pFlowPrefixBox = PFlowNameBuild(PFlowAffixWidth);
    private readonly TextBox pFlowSuffixBox = PFlowNameBuild(PFlowAffixWidth);
    private readonly TextBlock pFlowPrefixSeparator = PFlowAffixBuild();
    private readonly TextBlock pFlowSuffixSeparator = PFlowAffixBuild();
    private readonly Popup pFlowNamePopup;

    public PFlowName(LFlow lOwner, PViewfinder pHost)
    {
        lFlow = lOwner;
        pViewfinder = pHost;
        pFlowNameKeys = new Dictionary<Key, Action>
        {
            [Key.Enter] = PFlowNameCommit,
            [Key.Escape] = lFlow.LFlowName.LFlowNameHide,
        };
        pFlowStepKeys = new Dictionary<string, Action<object>> { [","] = PFlowStepRun };
        pFlowSteps = new Dictionary<object, Action>
        {
            [pFlowNameBox] = PFlowPrefixShow,
            [pFlowPrefixBox] = PFlowSuffixShow,
        };
        var pFieldPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pFieldPanel.Children.Add(pFlowNameBox);
        pFieldPanel.Children.Add(pFlowPrefixSeparator);
        pFieldPanel.Children.Add(pFlowPrefixBox);
        pFieldPanel.Children.Add(pFlowSuffixSeparator);
        pFieldPanel.Children.Add(pFlowSuffixBox);
        pFlowNamePopup = new Popup
        {
            PlacementTarget = pViewfinder,
            Placement = PlacementMode.Center,
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
        pFlowNameBox.KeyDown += PFlowNameHandle;
        pFlowPrefixBox.KeyDown += PFlowNameHandle;
        pFlowSuffixBox.KeyDown += PFlowNameHandle;
        pFlowNameBox.PreviewTextInput += PFlowStepHandle;
        pFlowPrefixBox.PreviewTextInput += PFlowStepHandle;
        pFlowSuffixBox.PreviewTextInput += PFlowStepHandle;
        lFlow.LFlowName.LFlowNameShow += PFlowNameShow;
        lFlow.LFlowName.LFlowNameClose += PFlowNameHide;
    }

    public void PFlowNameDetach()
    {
        lFlow.LFlowName.LFlowNameShow -= PFlowNameShow;
        lFlow.LFlowName.LFlowNameClose -= PFlowNameHide;
    }

    private void PFlowNameShow(LFlowNamePrompt lPrompt)
    {
        pFlowNameBox.Text = lPrompt.LFlowNameText;
        pFlowPrefixBox.Text = lPrompt.LFlowNamePrefix;
        pFlowSuffixBox.Text = lPrompt.LFlowNameSuffix;
        PFlowAffixShow(pFlowPrefixBox, pFlowPrefixSeparator, lPrompt.LFlowPrefixShown);
        PFlowAffixShow(pFlowSuffixBox, pFlowSuffixSeparator, lPrompt.LFlowSuffixShown);
        Rect pSectionRect = pViewfinder.PViewfinderSectionRead(lPrompt.LFlowNameIndex);
        pFlowNamePopup.HorizontalOffset = LFlowName.LFlowOffsetResolve(
            pSectionRect.IsEmpty, pSectionRect.Left, pSectionRect.Width, pViewfinder.ActualWidth);
        pFlowNamePopup.VerticalOffset = LFlowName.LFlowOffsetResolve(
            pSectionRect.IsEmpty, pSectionRect.Top, pSectionRect.Height, pViewfinder.ActualHeight);
        pFlowNamePopup.IsOpen = true;
        PFlowFieldSelect(pFlowNameBox);
    }

    private void PFlowNameHide() => pFlowNamePopup.IsOpen = false;

    private void PFlowNameCommit() =>
        lFlow.LFlowName.LFlowNameCommit(pFlowNameBox.Text, pFlowPrefixBox.Text, pFlowSuffixBox.Text);

    private void PFlowNameHandle(object pSender, KeyEventArgs pNameKeyEvent)
    {
        pFlowNameKeys.GetValueOrDefault(pNameKeyEvent.Key)?.Invoke();
        pNameKeyEvent.Handled = pFlowNameHandled.GetValueOrDefault(pNameKeyEvent.Key);
    }

    private void PFlowStepHandle(object pSender, TextCompositionEventArgs pFieldEvent)
    {
        pFlowStepKeys.GetValueOrDefault(pFieldEvent.Text)?.Invoke(pSender);
        pFieldEvent.Handled = pFlowStepHandled.GetValueOrDefault(pFieldEvent.Text);
    }

    private void PFlowStepRun(object pSender) => pFlowSteps.GetValueOrDefault(pSender)?.Invoke();

    private void PFlowPrefixShow()
    {
        PFlowAffixShow(pFlowPrefixBox, pFlowPrefixSeparator, true);
        PFlowFieldSelect(pFlowPrefixBox);
    }

    private void PFlowSuffixShow()
    {
        PFlowAffixShow(pFlowSuffixBox, pFlowSuffixSeparator, true);
        PFlowFieldSelect(pFlowSuffixBox);
    }

    private static void PFlowFieldSelect(TextBox pFieldBox)
    {
        pFieldBox.Focus();
        Keyboard.Focus(pFieldBox);
        pFieldBox.SelectAll();
    }

    private static void PFlowAffixShow(TextBox pAffixBox, TextBlock pSeparator, bool pAffixVisible)
    {
        pAffixBox.Visibility = PLook.PLookVisible[pAffixVisible];
        pSeparator.Visibility = PLook.PLookVisible[pAffixVisible];
    }

    private static TextBox PFlowNameBuild(double pFieldWidth)
    {
        var pFieldBox = new TextBox
        {
            Width = pFieldWidth,
            Height = PFlowNameHeight,
            FontSize = PSection.PSectionNameSize,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        PTextbox.PTextboxApply(pFieldBox);
        return pFieldBox;
    }

    private static TextBlock PFlowAffixBuild() => new()
    {
        Text = "/",
        Margin = new Thickness(6, 0, 6, 0),
        VerticalAlignment = VerticalAlignment.Center,
        Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E)),
        Visibility = Visibility.Collapsed
    };
}
