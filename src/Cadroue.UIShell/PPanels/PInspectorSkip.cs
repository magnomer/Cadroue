using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private CheckBox pSkipApplyBox = null!;
    private CheckBox pSkipPersistentBox = null!;
    private StackPanel pSkipBody = null!;

    public event Action? PSkipActiveChange;

    public bool PSkipActiveCheck() => pSkipApplyBox.IsChecked == true;

    public bool PSkipPersistentCheck() => pSkipPersistentBox.IsChecked == true;

    public void PSkipApply(bool pSkipActive)
    {
        pSkipApplyBox.IsChecked = pSkipActive;
    }

    public void PSkipPersistentApply(bool pSkipPersistent)
    {
        pSkipPersistentBox.IsChecked = pSkipPersistent;
    }

    private StackPanel PSkipBodyBuild()
    {
        pSkipApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Skip.ApplyTooltip"));
        pSkipApplyBox.Checked += (_, _) => PSkipActiveChange?.Invoke();
        pSkipApplyBox.Unchecked += (_, _) => PSkipActiveChange?.Invoke();

        pSkipPersistentBox = PInspectorSwitchBuild(
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

        pSkipBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pSkipBody.Children.Add(pSkipApplyBox);
        pSkipBody.Children.Add(PInspectorSeparatorBuild());
        pSkipBody.Children.Add(pSkipNote);
        return pSkipBody;
    }
}
