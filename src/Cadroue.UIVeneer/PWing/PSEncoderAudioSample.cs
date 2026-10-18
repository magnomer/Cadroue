using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PSEncoderAudioSample
{
    private static readonly int[] psAudioSampleStandard =
        [8000, 11025, 16000, 22050, 24000, 32000, 44100, 48000, 88200, 96000, 176400, 192000];

    private readonly QSEncoderAudio lsEncoderAudio;
    private readonly LPreset lsEncoderDraft;
    private TextBox? psAudioSampleReadout;

    internal PSEncoderAudioSample(QSEncoderAudio lsAudio, LPreset lsDraft)
    {
        lsEncoderAudio = lsAudio;
        lsEncoderDraft = lsDraft;
    }

    internal StackPanel PSAudioSamplePanel { get; } = new();

    internal void PSAudioSampleRebuild()
    {
        string pStored = PSAudioHertzRead();
        string pEncoder = LCapability.LCapabilityNameRead(lsEncoderAudio.QSEncoderAudioEncoder);
        IReadOnlyList<int> pRates = LInventoryHelp.LInventorySampleRead(pEncoder);
        (IReadOnlyList<int> pTicks, bool pDiscrete, double pMaximum) = PSAudioScaleResolve(pRates);
        PSAudioDetentRebuild(pTicks, pDiscrete, pMaximum, pStored);
    }

    private string PSAudioHertzRead() => psAudioSampleReadout is null
        ? lsEncoderDraft.LPresetAudio.LPresetSampleRate
        : PSAudioSampleRead();

    private static (IReadOnlyList<int> PSAudioTicks, bool PSAudioDiscrete, double PSAudioMaximum) PSAudioScaleResolve(
        IReadOnlyList<int> pRates)
    {
        bool pDiscrete = pRates.Count > 0;
        IReadOnlyList<int> pTicks = pDiscrete ? pRates : psAudioSampleStandard;
        double pMaximum = pTicks.Count > 0 ? pTicks[^1] : 48000;
        return (pTicks, pDiscrete, pMaximum);
    }

    private void PSAudioDetentRebuild(IReadOnlyList<int> pTicks, bool pDiscrete, double pMaximum, string pStored)
    {
        psAudioSampleReadout = PSEntry.PSEntryBuild(string.Empty, 96);
        UIElement pNotice = PSNotice.PSNoticeBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Notice.SampleSource"));
        UIElement pRow = PSFader.PSFaderDetentBuild(
            pTicks, pDiscrete, pMaximum,
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Sample.Source"),
            pStored, psAudioSampleReadout, pNotice);

        PSAudioSamplePanel.Children.Clear();
        PSAudioSamplePanel.Children.Add(
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Field.SampleRate"),
                pRow));
        PSAudioSamplePanel.Children.Add(pNotice);
    }

    internal string PSAudioSampleRead()
    {
        string pText = psAudioSampleReadout?.Text.Trim() ?? string.Empty;
        return int.TryParse(pText, out int pHz) && pHz > 0
            ? pHz.ToString()
            : "Same as source";
    }
}
