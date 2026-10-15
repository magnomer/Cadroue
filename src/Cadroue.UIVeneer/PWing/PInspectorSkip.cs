using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private CheckBox pSkipApplyBox = null!;
    private CheckBox pSkipPersistentBox = null!;
    private StackPanel pSkipBody = null!;

    private StackPanel PSkipBodyBuild()
    {
        pSkipApplyBox = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Skip.ApplyTooltip"));
        PInspectorSwitchAttach(pSkipApplyBox, QSkip.QSkipActiveSet);

        pSkipPersistentBox = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Skip.PersistentTooltip"));
        PInspectorSwitchAttach(pSkipPersistentBox, QSkip.QSkipPersistentSet);

        var pSkipNote = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Skip.Note"),
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
        PInspectorSwitchUpdate(pSkipApplyBox, QSkip.QSkipActive);
        PInspectorSwitchUpdate(pSkipPersistentBox, QSkip.QSkipPersistent);
    }
}
