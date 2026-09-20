using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private CheckBox pSkipApplyBox = null!;
    private CheckBox pSkipPersistentBox = null!;
    private StackPanel pSkipBody = null!;

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
        PInspectorSwitchUpdate(pSkipApplyBox, LSkip.LSkipActive);
        PInspectorSwitchUpdate(pSkipPersistentBox, LSkip.LSkipPersistent);
    }
}
