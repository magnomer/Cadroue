using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private CheckBox pSkipApplyBox = null!;
    private CheckBox pInspectorSkipPersistent = null!;
    private StackPanel pInspectorSkipBody = null!;

    public event Action? PSkipActiveChange;

    public bool PSkipActiveCheck() => pSkipApplyBox.IsChecked == true;

    public bool PSkipPersistentCheck() => pInspectorSkipPersistent.IsChecked == true;

    public void PSkipApply(bool pSkipActive)
    {
        pSkipApplyBox.IsChecked = pSkipActive;
    }

    public void PSkipPersistentApply(bool pSkipPersistent)
    {
        pInspectorSkipPersistent.IsChecked = pSkipPersistent;
    }

    private StackPanel PSkipBodyBuild()
    {
        pSkipApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Skip.ApplyTooltip"));
        pSkipApplyBox.Checked += (_, _) => PSkipActiveChange?.Invoke();
        pSkipApplyBox.Unchecked += (_, _) => PSkipActiveChange?.Invoke();

        pInspectorSkipPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Skip.PersistentTooltip"));

        var pSkipNote = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Skip.Note"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };

        pInspectorSkipBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pInspectorSkipBody.Children.Add(pSkipApplyBox);
        pInspectorSkipBody.Children.Add(PInspectorSeparatorBuild());
        pInspectorSkipBody.Children.Add(pSkipNote);
        return pInspectorSkipBody;
    }
}
