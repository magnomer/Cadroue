using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;

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
        pPanel.Children.Add(PSPlate.PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Playback.Autoplay"),
            PSField.PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.Autoplay"),
                psOptionsAutoplayBox)));
        pPanel.Children.Add(PSPlate.PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Playback.VolumePlate"),
            PSField.PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.VolumeMode"),
                psOptionsVolumeMode),
            PSOptionsFieldBuild(
                LLocalization.LLocalizationTextRead("Options.Playback.DefaultVolume"),
                psOptionsVolumeSlider,
                "%")));
        pPanel.Children.Add(PSPlate.PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Playback.Mousewheel"),
            PSField.PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.OverTimeline"),
                psOptionsWheelMode)));
        pPanel.Children.Add(PSPlate.PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Playback.Dragging"),
            PSField.PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.WhileDragging"),
                psOptionsDragBox)));
        return pPanel;
    }

    private UIElement PSPlaybackEngineBuild()
    {
        return PSPlate.PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Playback.Preview"),
            PSField.PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.Engine"), psOptionsEngineMode),
            PSSystemFlyleafBuild(),
            PSSystemMpvBuild(),
            PSNotice.PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.Playback.MpvEditNotice")));
    }
}
