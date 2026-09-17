using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private CheckBox pSkipApplyBox = null!;
    private CheckBox pSkipPersistentBox = null!;
    private StackPanel pSkipBody = null!;

    public event Action? PSkipActiveChange;

    public bool PSkipActiveCheck() => LSkip.LSkipActive;

    public bool PSkipPersistentCheck() => LSkip.LSkipPersistent;

    public void PSkipApply(bool pSkipActive) => LSkip.LSkipActiveSet(pSkipActive);

    public void PSkipPersistentApply(bool pSkipPersistent) => LSkip.LSkipPersistentSet(pSkipPersistent);

    private StackPanel PSkipBodyBuild()
    {
        pSkipApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Skip.ApplyTooltip"));
        PInspectorSwitchAttach(pSkipApplyBox, LSkip.LSkipActiveSet);

        pSkipPersistentBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Skip.PersistentTooltip"));
        PInspectorSwitchAttach(pSkipPersistentBox, LSkip.LSkipPersistentSet);

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

    private void PSkipUpdate()
    {
        bool pFlipped = (pSkipApplyBox.IsChecked == true) != LSkip.LSkipActive;
        PInspectorSwitchUpdate(pSkipApplyBox, LSkip.LSkipActive, false);
        PInspectorSwitchUpdate(pSkipPersistentBox, LSkip.LSkipPersistent, true);
        if (pFlipped)
        {
            PSkipActiveChange?.Invoke();
        }
    }
}
