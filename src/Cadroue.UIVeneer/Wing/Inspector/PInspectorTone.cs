using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PInspectorTone
{
    // The tone driver takes every tone section's parts in slot order: brightness, contrast, saturation.
    internal PInspectorTone(QInspector lInspector)
    {
        CheckBox pToneBrightnessBox = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplyBrightness"));
        PInspectorBrightnessPersistent = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistBrightness"));
        var pInspectorBrightnessStack = new StackPanel();
        PInspectorBrightnessBody = PInspectorField.PToneBodyBuild(pToneBrightnessBox, pInspectorBrightnessStack);

        CheckBox pToneContrastBox = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplyContrast"));
        PInspectorContrastPersistent = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistContrast"));
        var pInspectorContrastStack = new StackPanel();
        PInspectorContrastBody = PInspectorField.PToneBodyBuild(pToneContrastBox, pInspectorContrastStack);

        CheckBox pToneSaturationBox = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplySaturation"));
        PInspectorSaturationPersistent = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistSaturation"));
        var pInspectorSaturationStack = new StackPanel();
        PInspectorSaturationBody = PInspectorField.PToneBodyBuild(pToneSaturationBox, pInspectorSaturationStack);

        _ = new QTone(
            lInspector,
            [pToneBrightnessBox, pToneContrastBox, pToneSaturationBox],
            [PInspectorBrightnessPersistent, PInspectorContrastPersistent, PInspectorSaturationPersistent],
            [pInspectorBrightnessStack, pInspectorContrastStack, pInspectorSaturationStack],
            [PInspectorBrightnessBody, PInspectorContrastBody, PInspectorSaturationBody]);
    }

    internal CheckBox PInspectorBrightnessPersistent { get; }

    internal StackPanel PInspectorBrightnessBody { get; }

    internal CheckBox PInspectorContrastPersistent { get; }

    internal StackPanel PInspectorContrastBody { get; }

    internal CheckBox PInspectorSaturationPersistent { get; }

    internal StackPanel PInspectorSaturationBody { get; }
}
