using System.Windows;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Application;

using static Cadroue.UIVeneer.PSField;
using static Cadroue.UIVeneer.PSCombo;
using static Cadroue.UIVeneer.PSEntry;
using static Cadroue.UIVeneer.PSNotice;
using static Cadroue.UIVeneer.PSFader;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{
    private static readonly int[] psAudioSampleStandard =
        [8000, 11025, 16000, 22050, 24000, 32000, 44100, 48000, 88200, 96000, 176400, 192000];

    private void PSAudioSampleRebuild()
    {
        string pStored = psAudioSampleReadout is null
            ? lsExportSpecificEdit.LPresetAudio.LPresetSampleRate
            : PSAudioSampleRead();
        string pEncoder = LCapability.LCapabilityNameRead(lsEncoder.LSEncoderAudioEncoder);
        IReadOnlyList<int> pRates = LInventory.LInventorySampleRead(pEncoder);
        bool pDiscrete = pRates.Count > 0;
        IReadOnlyList<int> pTicks = pDiscrete ? pRates : psAudioSampleStandard;
        double pMaximum = pTicks.Count > 0 ? pTicks[^1] : 48000;

        psAudioSampleReadout = PSEntryBuild(string.Empty, 96);
        UIElement pNotice = PSNoticeBuild(LLocalization.LLocalizationTextRead("Encoder.Audio.Notice.SampleSource"));
        UIElement pRow = PSFaderDetentBuild(
            pTicks, pDiscrete, pMaximum,
            LLocalization.LLocalizationTextRead("Encoder.Sample.Source"),
            pStored, psAudioSampleReadout, pNotice);

        psAudioSamplePanel.Children.Clear();
        psAudioSamplePanel.Children.Add(
            PSFieldBuild(
                LLocalization.LLocalizationTextRead("Encoder.Audio.Field.SampleRate"),
                pRow));
        psAudioSamplePanel.Children.Add(pNotice);
    }

    private string PSAudioSampleRead()
    {
        string pText = psAudioSampleReadout?.Text.Trim() ?? string.Empty;
        return int.TryParse(pText, out int pHz) && pHz > 0
            ? pHz.ToString()
            : "Same as source";
    }
}
