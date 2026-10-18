using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSOptionsTimeline
{
    internal static readonly LLocalizationChoice[] PSOptionsOrderItems =
    {
        new("MapFirst", "Options.Timeline.MapTop"),
        new("ViewfinderFirst", "Options.Timeline.ViewfinderTop")
    };

    internal required PSOptionsSpectrum psOptionsSpectrum;
    internal required Border psOptionsOrderMode;
    internal required Slider psKeyframeSlider;
    internal required Slider psKeyframeDelaySlider;
    internal required CheckBox psOptionsOverlapBox;
    internal required CheckBox psWaveformBox;

    internal UIElement PSTimelineBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.Order"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.StripOrder"),
                psOptionsOrderMode)));
        pPanel.Children.Add(
            PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.KeyframeSpacing"),
            PSOptionsField.PSOptionsFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.Minimum"),
                psKeyframeSlider,
                " px")));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Timeline.KeyframeDelay"),
            PSOptionsField.PSOptionsFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.KeyframeDelayLabel"),
                psKeyframeDelaySlider,
                " ms"),
            PSNotice.PSNoticeBuild(QLocalizationCatalog.QLocalizationTextRead(
                "Options.Timeline.KeyframeDelayNotice"))));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Timeline.Overlapping"),
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.OverlappingSections"),
                psOptionsOverlapBox)));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Timeline.Waveform"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.ShowWaveforms"),
                psWaveformBox),
            PSNotice.PSNoticeBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.WaveformNotice"))));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Timeline.SectionPalette"),
            psOptionsSpectrum.PSSpectrumFieldBuild()));
        return pPanel;
    }
}
