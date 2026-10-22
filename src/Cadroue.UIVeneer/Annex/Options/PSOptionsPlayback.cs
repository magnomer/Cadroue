using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSOptionsPlayback
{
    internal static readonly LLocalizationChoice[] PSOptionsVolumeItems =
    {
        new("Unified", "Options.Playback.VolumeUnified"),
        new("PerTab", "Options.Playback.VolumePerTab")
    };

    internal static readonly LLocalizationChoice[] PSOptionsWheelItems =
    {
        new("Seek", "Options.Playback.Seek"),
        new("Zoom", "Options.Playback.Zoom"),
        new("Volume", "Options.Playback.Volume")
    };

    internal static readonly LLocalizationChoice[] PSOptionsEngineItems =
    {
        new("Flyleaf", "Options.Playback.EngineFlyleaf"),
        new("Mpv", "Options.Playback.EngineMpv")
    };

    internal required Border psOptionsEngineMode;
    internal required CheckBox psOptionsAutoplayBox;
    internal required Border psOptionsVolumeMode;
    internal required Slider psOptionsVolumeSlider;
    internal required Border psOptionsWheelMode;
    internal required CheckBox psOptionsDragBox;

    internal UIElement PSPlaybackBuild(UIElement psFlyleaf, UIElement psMpv)
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlaybackEngineBuild(psFlyleaf, psMpv));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Playback.Autoplay"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Playback.Autoplay"),
                psOptionsAutoplayBox)));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Playback.VolumePlate"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Playback.VolumeMode"),
                psOptionsVolumeMode),
            PSOptionsField.PSOptionsFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Playback.DefaultVolume"),
                psOptionsVolumeSlider,
                "%")));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Playback.Mousewheel"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Playback.OverTimeline"),
                psOptionsWheelMode)));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Playback.Dragging"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Playback.WhileDragging"),
                psOptionsDragBox)));
        return pPanel;
    }

    private UIElement PSPlaybackEngineBuild(UIElement psFlyleaf, UIElement psMpv)
    {
        return PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Playback.Preview"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Playback.Engine"),
                psOptionsEngineMode),
            psFlyleaf,
            psMpv,
            PSNotice.PSNoticeBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Playback.MpvEditNotice")));
    }
}
