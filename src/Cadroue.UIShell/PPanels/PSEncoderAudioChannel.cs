using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;

using static Cadroue.UIShell.PSShared.PSField;
using static Cadroue.UIShell.PSShared.PSCombo;
using static Cadroue.UIShell.PSShared.PSEntry;
using static Cadroue.UIShell.PSShared.PSNotice;
using static Cadroue.UIShell.PSShared.PSFader;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private static readonly string[] psAudioChannelStandard =
        ["mono", "stereo", "2.1", "3.0", "4.0", "5.0", "5.1", "6.1", "7.1"];

    private void PSAudioChannelRebuild()
    {
        string pStored = psAudioChannelReadout is null
            ? lsExportSpecificEdit.LPresetAudio.LPresetChannels
            : PSAudioChannelRead();
        string pEncoder = LCapability.LCapabilityNameRead(PSComboTextRead(psAudioEncoderCombo));
        IReadOnlyList<string> pLayouts = LInventory.LInventoryLayoutRead(pEncoder);
        if (pLayouts.Count == 0)
        {
            pLayouts = psAudioChannelStandard;
        }

        psAudioChannelLayouts = pLayouts;

        var pLabels = new List<string>(pLayouts.Count + 1)
        {
            LLocalization.LLocalizationTextRead("Encoder.Sample.Source")
        };
        pLabels.AddRange(pLayouts);

        int pIndex = 0;
        for (int pAt = 0; pAt < pLayouts.Count; pAt++)
        {
            if (string.Equals(pLayouts[pAt], pStored, StringComparison.OrdinalIgnoreCase))
            {
                pIndex = pAt + 1;
                break;
            }
        }

        psAudioChannelSlider = new Slider();
        psAudioChannelReadout = PSEntryBuild(string.Empty, 96);
        UIElement pNotice = PSNoticeBuild(LLocalization.LLocalizationTextRead("Encoder.Audio.Notice.ChannelSource"));
        UIElement pRow = PSFaderLayoutBuild(psAudioChannelSlider, pLabels, pIndex, psAudioChannelReadout, pNotice);

        psAudioChannelPanel.Children.Clear();
        psAudioChannelPanel.Children.Add(PSFieldBuild(
            LLocalization.LLocalizationTextRead("Encoder.Audio.Field.Channels"), pRow));
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
