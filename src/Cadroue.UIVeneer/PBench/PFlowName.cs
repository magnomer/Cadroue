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
    private readonly QFlow lFlow;
    private readonly PViewfinder pViewfinder;
    private readonly TextBox pFlowNameBox = PFlowNameBuild(PFlowNameWidth);
    private readonly TextBox pFlowPrefixBox = PFlowNameBuild(PFlowAffixWidth);
    private readonly TextBox pFlowSuffixBox = PFlowNameBuild(PFlowAffixWidth);
    private readonly TextBlock pFlowPrefixSeparator = PFlowAffixBuild();
    private readonly TextBlock pFlowSuffixSeparator = PFlowAffixBuild();
    private readonly Popup pFlowNamePopup;

    public PFlowName(QFlow lOwner, PViewfinder pHost)
    {
        lFlow = lOwner;
        pViewfinder = pHost;
        pFlowNameKeys = PFlowKeysCreate();
        pFlowStepKeys = PFlowInputsCreate();
        pFlowSteps = PFlowStepsCreate();
        pFlowNamePopup = PFlowPopupBuild();
        PFlowNameAttach();
    }

    private Dictionary<Key, Action> PFlowKeysCreate() => new()
    {
        [Key.Enter] = PFlowNameCommit,
        [Key.Escape] = lFlow.QFlowName.QFlowNameHide,
    };

    private Dictionary<string, Action<object>> PFlowInputsCreate() => new() { [","] = PFlowStepRun };

    private Dictionary<object, Action> PFlowStepsCreate() => new()
    {
        [pFlowNameBox] = PFlowPrefixShow,
        [pFlowPrefixBox] = PFlowSuffixShow,
    };

    private Popup PFlowPopupBuild()
    {
        var pFieldPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pFieldPanel.Children.Add(pFlowNameBox);
        pFieldPanel.Children.Add(pFlowPrefixSeparator);
        pFieldPanel.Children.Add(pFlowPrefixBox);
        pFieldPanel.Children.Add(pFlowSuffixSeparator);
        pFieldPanel.Children.Add(pFlowSuffixBox);
        return new Popup
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
    }

    private void PFlowNameAttach()
    {
        pFlowNameBox.KeyDown += PFlowNameHandle;
        pFlowPrefixBox.KeyDown += PFlowNameHandle;
        pFlowSuffixBox.KeyDown += PFlowNameHandle;
        pFlowNameBox.PreviewTextInput += PFlowStepHandle;
        pFlowPrefixBox.PreviewTextInput += PFlowStepHandle;
        pFlowSuffixBox.PreviewTextInput += PFlowStepHandle;
        lFlow.QFlowName.QFlowNameShown += PFlowNameShow;
        lFlow.QFlowName.QFlowNameClosed += PFlowNameHide;
    }

    public void PFlowNameDetach()
    {
        lFlow.QFlowName.QFlowNameShown -= PFlowNameShow;
        lFlow.QFlowName.QFlowNameClosed -= PFlowNameHide;
    }

    private void PFlowNameShow(QFlowNamePrompt lPrompt)
    {
        PFlowFieldsShow(lPrompt);
        QViewfinderPoint lOffset = pViewfinder.QViewfinder.QViewfinderPopupResolve(
            lPrompt.QFlowNameIndex, pViewfinder.ActualWidth, pViewfinder.ActualHeight);
        PFlowPopupOpen(lOffset);
        PFlowFieldSelect(pFlowNameBox);
    }

    private void PFlowFieldsShow(QFlowNamePrompt lPrompt)
    {
        pFlowNameBox.Text = lPrompt.QFlowNameText;
        pFlowPrefixBox.Text = lPrompt.QFlowNamePrefix;
        pFlowSuffixBox.Text = lPrompt.QFlowNameSuffix;
        PFlowAffixShow(pFlowPrefixBox, pFlowPrefixSeparator, lPrompt.QFlowPrefixShown);
        PFlowAffixShow(pFlowSuffixBox, pFlowSuffixSeparator, lPrompt.QFlowSuffixShown);
    }

    private void PFlowPopupOpen(QViewfinderPoint lOffset)
    {
        pFlowNamePopup.HorizontalOffset = lOffset.QViewfinderPointX;
        pFlowNamePopup.VerticalOffset = lOffset.QViewfinderPointY;
        pFlowNamePopup.IsOpen = true;
    }

    private void PFlowNameHide() => pFlowNamePopup.IsOpen = false;

    private void PFlowNameCommit() =>
        lFlow.QFlowName.QFlowNameCommit(pFlowNameBox.Text, pFlowPrefixBox.Text, pFlowSuffixBox.Text);

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
        QTextbox.QTextboxAttach(pFieldBox);
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
