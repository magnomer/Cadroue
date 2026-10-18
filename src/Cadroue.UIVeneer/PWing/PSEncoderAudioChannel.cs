using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{
    private static readonly string[] psAudioChannelStandard =
        ["mono", "stereo", "2.1", "3.0", "4.0", "5.0", "5.1", "6.1", "7.1"];

    private void PSAudioChannelRebuild()
    {
        string pStored = PSAudioLayoutRead();
        string pEncoder = LCapability.LCapabilityNameRead(lsEncoder.QSEncoderAudio.QSEncoderAudioEncoder);
        IReadOnlyList<string> pLayouts = PSAudioLayoutsResolve(LInventoryHelp.LInventoryLayoutRead(pEncoder));
        PSAudioLayoutsSet(pLayouts);
        PSAudioLayoutRebuild(PSAudioCaptionsResolve(pLayouts), PSAudioPositionResolve(pLayouts, pStored));
    }

    private string PSAudioLayoutRead() => psAudioChannelReadout is null
        ? lsExportSpecificEdit.LPresetAudio.LPresetChannels
        : PSAudioChannelRead();

    private static IReadOnlyList<string> PSAudioLayoutsResolve(IReadOnlyList<string> pLayouts) =>
        pLayouts.Count == 0 ? psAudioChannelStandard : pLayouts;

    private void PSAudioLayoutsSet(IReadOnlyList<string> pLayouts) => psAudioChannelLayouts = pLayouts;

    private static List<string> PSAudioCaptionsResolve(IReadOnlyList<string> pLayouts)
    {
        var pLabels = new List<string>(pLayouts.Count + 1)
        {
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Sample.Source")
        };
        pLabels.AddRange(pLayouts);
        return pLabels;
    }

    private static int PSAudioPositionResolve(IReadOnlyList<string> pLayouts, string pStored)
    {
        int pIndex = 0;
        for (int pAt = 0; pAt < pLayouts.Count; pAt++)
        {
            if (string.Equals(pLayouts[pAt], pStored, StringComparison.OrdinalIgnoreCase))
            {
                pIndex = pAt + 1;
                break;
            }
        }

        return pIndex;
    }

    private void PSAudioLayoutRebuild(List<string> pLabels, int pIndex)
    {
        psAudioChannelSlider = new Slider();
        psAudioChannelReadout = PSEntry.PSEntryBuild(string.Empty, 96);
        UIElement pNotice = PSNotice.PSNoticeBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Notice.ChannelSource"));
        UIElement pRow = PSFader.PSFaderLayoutBuild(psAudioChannelSlider, pLabels, pIndex, psAudioChannelReadout,
            pNotice);

        psAudioChannelPanel.Children.Clear();
        psAudioChannelPanel.Children.Add(PSField.PSFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Field.Channels"), pRow));
        psAudioChannelPanel.Children.Add(pNotice);
    }

    private string PSAudioChannelRead()
    {
        if (psAudioChannelSlider is null)
        {
            return "Same as source";
        }

        int pIndex = Math.Clamp((int)Math.Round(psAudioChannelSlider.Value), 0, psAudioChannelLayouts.Count);
        return pIndex <= 0 ? "Same as source" : psAudioChannelLayouts[pIndex - 1];
    }
}
