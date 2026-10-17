using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private CheckBox pToneBrightnessBox = null!;
    private CheckBox pInspectorBrightnessPersistent = null!;
    private StackPanel pInspectorBrightnessStack = null!;
    private StackPanel pInspectorBrightnessBody = null!;

    private CheckBox pToneContrastBox = null!;
    private CheckBox pInspectorContrastPersistent = null!;
    private StackPanel pInspectorContrastStack = null!;
    private StackPanel pInspectorContrastBody = null!;

    private CheckBox pToneSaturationBox = null!;
    private CheckBox pInspectorSaturationPersistent = null!;
    private StackPanel pInspectorSaturationStack = null!;
    private StackPanel pInspectorSaturationBody = null!;

    private StackPanel PInspectorBrightnessBuild()
    {
        pToneBrightnessBox = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplyBrightness"));
        pInspectorBrightnessPersistent = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistBrightness"));
        pInspectorBrightnessStack = new StackPanel();
        pInspectorBrightnessBody = PToneBodyBuild(pToneBrightnessBox, pInspectorBrightnessStack);
        return pInspectorBrightnessBody;
    }

    private StackPanel PInspectorContrastBuild()
    {
        pToneContrastBox = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplyContrast"));
        pInspectorContrastPersistent = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistContrast"));
        pInspectorContrastStack = new StackPanel();
        pInspectorContrastBody = PToneBodyBuild(pToneContrastBox, pInspectorContrastStack);
        return pInspectorContrastBody;
    }

    private StackPanel PInspectorSaturationBuild()
    {
        pToneSaturationBox = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplySaturation"));
        pInspectorSaturationPersistent = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistSaturation"));
        pInspectorSaturationStack = new StackPanel();
        pInspectorSaturationBody = PToneBodyBuild(pToneSaturationBox, pInspectorSaturationStack);
        return pInspectorSaturationBody;
    }

    // The tone driver takes every tone section's parts in slot order: brightness, contrast, saturation.
    private void PToneBuild() =>
        _ = new QTone(
            QInspector,
            [pToneBrightnessBox, pToneContrastBox, pToneSaturationBox],
            [pInspectorBrightnessPersistent, pInspectorContrastPersistent, pInspectorSaturationPersistent],
            [pInspectorBrightnessStack, pInspectorContrastStack, pInspectorSaturationStack],
            [pInspectorBrightnessBody, pInspectorContrastBody, pInspectorSaturationBody]);
}
