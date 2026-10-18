using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PInspectorSkip
{
    private readonly CheckBox pSkipApplyBox;

    internal PInspectorSkip(QSkip lSkip)
    {
        QSkip = lSkip;
        pSkipApplyBox = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Skip.ApplyTooltip"));

        PSkipPersistentBox = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Skip.PersistentTooltip"));

        var pSkipNote = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Skip.Note"),
            FontSize = 12,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            Foreground = PInspectorLook.PInspectorMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };

        PSkipBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        PSkipBody.Children.Add(pSkipApplyBox);
        PSkipBody.Children.Add(PInspectorField.PInspectorSeparatorBuild());
        PSkipBody.Children.Add(pSkipNote);

        PInspectorSwitch.PInspectorSwitchAttach(pSkipApplyBox, QSkip.QSkipActiveSet);
        PInspectorSwitch.PInspectorSwitchAttach(PSkipPersistentBox, QSkip.QSkipPersistentSet);
    }

    private QSkip QSkip { get; }

    internal CheckBox PSkipPersistentBox { get; }

    internal StackPanel PSkipBody { get; }

    internal void PSkipChangeAttach() => QSkip.QSkipChanged += PSkipUpdate;

    internal void PSkipUpdate()
    {
        PInspectorSwitch.PInspectorSwitchUpdate(pSkipApplyBox, QSkip.QSkipActive);
        PInspectorSwitch.PInspectorSwitchUpdate(PSkipPersistentBox, QSkip.QSkipPersistent);
    }
}
