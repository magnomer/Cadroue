using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed partial class PSOptions
{
    private static readonly LLocalizationChoice[] PSOptionsVolumeItems =
    {
        new("Unified", "Options.Playback.VolumeUnified"),
        new("PerTab", "Options.Playback.VolumePerTab")
    };

    private static readonly LLocalizationChoice[] PSOptionsWheelItems =
    {
        new("Seek", "Options.Playback.Seek"),
        new("Zoom", "Options.Playback.Zoom"),
        new("Volume", "Options.Playback.Volume")
    };

    private static readonly LLocalizationChoice[] PSOptionsEngineItems =
    {
        new("Flyleaf", "Options.Playback.EngineFlyleaf"),
        new("Mpv", "Options.Playback.EngineMpv")
    };

    private readonly Border psOptionsEngineMode;
    private readonly Action<string, bool> psOptionsEngineEnable;
    private readonly CheckBox psOptionsAutoplayBox;
    private readonly Border psOptionsVolumeMode;
    private readonly Slider psOptionsVolumeSlider;
    private readonly Border psOptionsWheelMode;
    private readonly CheckBox psOptionsDragBox;

    private UIElement PSPlaybackBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlaybackEngineBuild());
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Playback.Autoplay"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Playback.Autoplay"),
                psOptionsAutoplayBox)));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Playback.VolumePlate"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Playback.VolumeMode"),
                psOptionsVolumeMode),
            PSOptionsFieldBuild(
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

    private UIElement PSPlaybackEngineBuild()
    {
        return PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Playback.Preview"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Playback.Engine"),
                psOptionsEngineMode),
            PSSystemFlyleafBuild(),
            PSSystemMpvBuild(),
            PSNotice.PSNoticeBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Playback.MpvEditNotice")));
    }
}
