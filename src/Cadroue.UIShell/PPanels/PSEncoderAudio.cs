using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;

using static Cadroue.UIShell.PSShared.PSField;
using static Cadroue.UIShell.PSShared.PSCombo;
using static Cadroue.UIShell.PSShared.PSInline;
using static Cadroue.UIShell.PSShared.PSPlate;
using static Cadroue.UIShell.PSShared.PSEntry;
using static Cadroue.UIShell.PSShared.PSNotice;
using static Cadroue.UIShell.PSShared.PSFader;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private static readonly (string PSAudioText, string PSAudioName)[] PSAudioCandidates =
    [
        ("AAC, native / aac", "aac"),
        ("AAC, Fraunhofer FDK / libfdk_aac", "libfdk_aac"),
        ("AAC, Media Foundation / aac_mf", "aac_mf"),
        ("AAC, AudioToolbox / aac_at", "aac_at"),
        ("MP3, LAME / libmp3lame", "libmp3lame"),
        ("MP3, Shine / libshine", "libshine"),
        ("MP3, Media Foundation / mp3_mf", "mp3_mf"),
        ("MP2, native / mp2", "mp2"),
        ("MP2, fixed-point / mp2fixed", "mp2fixed"),
        ("MP2, TwoLAME / libtwolame", "libtwolame"),
        ("Opus, libopus / libopus", "libopus"),
        ("Opus, native / opus", "opus"),
        ("Vorbis, libvorbis / libvorbis", "libvorbis"),
        ("Vorbis, native / vorbis", "vorbis"),
        ("AC-3, native / ac3", "ac3"),
        ("AC-3, fixed-point / ac3_fixed", "ac3_fixed"),
        ("AC-3, Media Foundation / ac3_mf", "ac3_mf"),
        ("E-AC-3, native / eac3", "eac3"),
        ("DTS, native / dca", "dca"),
        ("TrueHD, native / truehd", "truehd"),
        ("MLP, native / mlp", "mlp"),
        ("WMA v1, native / wmav1", "wmav1"),
        ("WMA v2, native / wmav2", "wmav2"),
        ("FLAC, native / flac", "flac"),
        ("ALAC, native / alac", "alac"),
        ("ALAC, AudioToolbox / alac_at", "alac_at"),
        ("WavPack, native / wavpack", "wavpack"),
        ("TTA, native / tta", "tta"),
        ("Sonic, native / sonic", "sonic"),
        ("Sonic lossless, native / sonic_ls", "sonic_ls"),
        ("PCM signed 8-bit / pcm_s8", "pcm_s8"),
        ("PCM signed 8-bit planar / pcm_s8_planar", "pcm_s8_planar"),
        ("PCM signed 16-bit LE / pcm_s16le", "pcm_s16le"),
        ("PCM signed 16-bit BE / pcm_s16be", "pcm_s16be"),
        ("PCM signed 16-bit LE planar / pcm_s16le_planar", "pcm_s16le_planar"),
        ("PCM signed 16-bit BE planar / pcm_s16be_planar", "pcm_s16be_planar"),
        ("PCM signed 24-bit LE / pcm_s24le", "pcm_s24le"),
        ("PCM signed 24-bit BE / pcm_s24be", "pcm_s24be"),
        ("PCM signed 24-bit LE planar / pcm_s24le_planar", "pcm_s24le_planar"),
        ("PCM signed 24-bit D-Cinema / pcm_s24daud", "pcm_s24daud"),
        ("PCM signed 32-bit LE / pcm_s32le", "pcm_s32le"),
        ("PCM signed 32-bit BE / pcm_s32be", "pcm_s32be"),
        ("PCM signed 32-bit LE planar / pcm_s32le_planar", "pcm_s32le_planar"),
        ("PCM signed 64-bit LE / pcm_s64le", "pcm_s64le"),
        ("PCM signed 64-bit BE / pcm_s64be", "pcm_s64be"),
        ("PCM unsigned 8-bit / pcm_u8", "pcm_u8"),
        ("PCM unsigned 16-bit LE / pcm_u16le", "pcm_u16le"),
        ("PCM unsigned 16-bit BE / pcm_u16be", "pcm_u16be"),
        ("PCM unsigned 24-bit LE / pcm_u24le", "pcm_u24le"),
        ("PCM unsigned 24-bit BE / pcm_u24be", "pcm_u24be"),
        ("PCM unsigned 32-bit LE / pcm_u32le", "pcm_u32le"),
        ("PCM unsigned 32-bit BE / pcm_u32be", "pcm_u32be"),
        ("PCM float 32-bit LE / pcm_f32le", "pcm_f32le"),
        ("PCM float 32-bit BE / pcm_f32be", "pcm_f32be"),
        ("PCM float 64-bit LE / pcm_f64le", "pcm_f64le"),
        ("PCM float 64-bit BE / pcm_f64be", "pcm_f64be"),
        ("PCM A-law / pcm_alaw", "pcm_alaw"),
        ("PCM A-law, AudioToolbox / pcm_alaw_at", "pcm_alaw_at"),
        ("PCM mu-law / pcm_mulaw", "pcm_mulaw"),
        ("PCM mu-law, AudioToolbox / pcm_mulaw_at", "pcm_mulaw_at"),
        ("PCM VIDC / pcm_vidc", "pcm_vidc"),
        ("PCM Blu-ray / pcm_bluray", "pcm_bluray"),
        ("PCM DVD / pcm_dvd", "pcm_dvd"),
        ("PCM SMPTE 302M / s302m", "s302m"),
        ("ADPCM ADX / adpcm_adx", "adpcm_adx"),
        ("ADPCM Argonaut Games / adpcm_argo", "adpcm_argo"),
        ("ADPCM G.722 / adpcm_g722", "adpcm_g722"),
        ("ADPCM G.726 / adpcm_g726", "adpcm_g726"),
        ("ADPCM G.726 LE / adpcm_g726le", "adpcm_g726le"),
        ("ADPCM IMA ALP / adpcm_ima_alp", "adpcm_ima_alp"),
        ("ADPCM IMA AMV / adpcm_ima_amv", "adpcm_ima_amv"),
        ("ADPCM IMA APM / adpcm_ima_apm", "adpcm_ima_apm"),
        ("ADPCM IMA QuickTime / adpcm_ima_qt", "adpcm_ima_qt"),
        ("ADPCM IMA Simon & Schuster / adpcm_ima_ssi", "adpcm_ima_ssi"),
        ("ADPCM IMA WAV / adpcm_ima_wav", "adpcm_ima_wav"),
        ("ADPCM IMA Westwood / adpcm_ima_ws", "adpcm_ima_ws"),
        ("ADPCM Microsoft / adpcm_ms", "adpcm_ms"),
        ("ADPCM Shockwave Flash / adpcm_swf", "adpcm_swf"),
        ("ADPCM Yamaha / adpcm_yamaha", "adpcm_yamaha"),
        ("AMR-NB, OpenCORE / libopencore_amrnb", "libopencore_amrnb"),
        ("AMR-WB, VisualOn / libvo_amrwbenc", "libvo_amrwbenc"),
        ("Speex, libspeex / libspeex", "libspeex"),
        ("GSM, libgsm / libgsm", "libgsm"),
        ("GSM Microsoft, libgsm / libgsm_ms", "libgsm_ms"),
        ("iLBC, libilbc / libilbc", "libilbc"),
        ("iLBC, AudioToolbox / ilbc_at", "ilbc_at"),
        ("Codec2, libcodec2 / libcodec2", "libcodec2"),
        ("LC3, liblc3 / liblc3", "liblc3"),
        ("G.723.1, native / g723_1", "g723_1"),
        ("Comfort noise, native / comfortnoise", "comfortnoise"),
        ("Nellymoser, native / nellymoser", "nellymoser"),
        ("aptX, native / aptx", "aptx"),
        ("aptX HD, native / aptx_hd", "aptx_hd"),
        ("SBC, native / sbc", "sbc"),
        ("DFPWM, native / dfpwm", "dfpwm"),
        ("RealAudio 1.0, native / ra_144", "ra_144"),
        ("RoQ DPCM, native / roq_dpcm", "roq_dpcm")
    ];

    private static readonly Dictionary<string, string[]> PSAudioContainerTable = new(StringComparer.Ordinal)
    {
        ["AAC"] = ["MP4", "Matroska", "MOV", "MPEG-TS", "FLV", "AVI"],
        ["MP3"] = ["MP4", "Matroska", "MOV", "AVI", "MPEG-TS", "FLV"],
        ["MP2"] = ["Matroska", "MPEG-TS", "AVI"],
        ["AC-3"] = ["MP4", "Matroska", "MOV", "MPEG-TS", "AVI"],
        ["E-AC-3"] = ["MP4", "Matroska", "MOV", "MPEG-TS"],
        ["Opus"] = ["MP4", "Matroska", "WebM", "Ogg"],
        ["Vorbis"] = ["Matroska", "WebM", "Ogg"],
        ["FLAC"] = ["MP4", "Matroska", "Ogg"],
        ["ALAC"] = ["MP4", "Matroska", "MOV"],
        ["WavPack"] = ["Matroska"],
        ["TTA"] = ["Matroska"],
        ["TrueHD"] = ["Matroska", "MPEG-TS"],
        ["MLP"] = ["Matroska", "MPEG-TS"],
        ["PCM"] = ["Matroska", "MOV", "AVI"]
    };

    private static string PSAudioFamilyRead(string pName)
    {
        if (pName.StartsWith("pcm_", StringComparison.OrdinalIgnoreCase))
        {
            return "PCM";
        }

        return pName switch
        {
            "aac" or "libfdk_aac" or "aac_mf" or "aac_at" => "AAC",
            "libmp3lame" or "libshine" or "mp3_mf" => "MP3",
            "mp2" or "mp2fixed" or "libtwolame" => "MP2",
            "ac3" or "ac3_fixed" or "ac3_mf" => "AC-3",
            "eac3" => "E-AC-3",
            "libopus" or "opus" => "Opus",
            "libvorbis" or "vorbis" => "Vorbis",
            "flac" => "FLAC",
            "alac" or "alac_at" => "ALAC",
            "wavpack" => "WavPack",
            "tta" => "TTA",
            "truehd" => "TrueHD",
            "mlp" => "MLP",
            _ => string.Empty
        };
    }

    private static bool PSAudioContainerCheck(string pName, string pContainer)
    {
        string pFamily = PSAudioFamilyRead(pName);
        return pFamily.Length == 0
            || (PSAudioContainerTable.TryGetValue(pFamily, out string[]? pContainers) && pContainers.Contains(pContainer));
    }

    private static string[] PSAudioItemsRead() =>
        PSAudioCandidates
            .Where(pCandidate => LInventory.LInventoryInstalledCheck(pCandidate.PSAudioName))
            .Select(pCandidate => pCandidate.PSAudioText)
            .ToArray();

    private static string[] PSAudioItemsRead(string pContainer)
    {
        if (!LRepertoireCatalog.LRepertoireContainerNames.Contains(pContainer))
        {
            return PSAudioItemsRead();
        }

        return PSAudioCandidates
            .Where(pCandidate => LInventory.LInventoryInstalledCheck(pCandidate.PSAudioName)
                              && PSAudioContainerCheck(pCandidate.PSAudioName, pContainer))
            .Select(pCandidate => pCandidate.PSAudioText)
            .ToArray();
    }

    private static string[] PSAudioItemsRead(string pContainer, string pKeep)
    {
        string[] pItems = PSAudioItemsRead(pContainer);
        if (string.IsNullOrEmpty(pKeep) || pItems.Contains(pKeep))
        {
            return pItems;
        }

        var pCandidate = PSAudioCandidates.FirstOrDefault(pEntry => string.Equals(pEntry.PSAudioText, pKeep, StringComparison.Ordinal));
        bool pFits = pCandidate.PSAudioName is not null
                     && (!LRepertoireCatalog.LRepertoireContainerNames.Contains(pContainer) || PSAudioContainerCheck(pCandidate.PSAudioName, pContainer));
        return pFits ? [pKeep, .. pItems] : pItems;
    }

    private static bool PSAudioAvailableCheck(string pText)
    {
        foreach (var pCandidate in PSAudioCandidates)
        {
            if (string.Equals(pCandidate.PSAudioText, pText, StringComparison.Ordinal))
            {
                return LInventory.LInventoryInstalledCheck(pCandidate.PSAudioName);
            }
        }

        return true;
    }

    private void PSAudioEncoderUpdate()
    {
        bool pAvailable = PSAudioAvailableCheck(PSComboTextRead(psAudioEncoderCombo));
        psAudioEncoderNotice.Visibility = pAvailable ? Visibility.Collapsed : Visibility.Visible;
        if (!pAvailable)
        {
            psAudioEncoderNotice.Text = LLocalization.LLocalizationTextRead("Encoder.Audio.Notice.Unavailable");
        }
    }

    private void PSAudioContainerHandle()
    {
        string pContainer = PSComboTextRead(psOutputContainerCombo);
        string pCurrent = psAudioEncoderCombo.SelectedItem as string ?? string.Empty;
        string[] pItems = PSAudioItemsRead(pContainer, pCurrent);
        psAudioEncoderCombo.ItemsSource = pItems;
        psAudioEncoderCombo.SelectedItem = pItems.Contains(pCurrent) ? pCurrent : pItems.FirstOrDefault();
        PSAudioEncoderUpdate();
    }

    private async Task PSAudioVerifyHandle(ComboBox pCombo, Button pButton, IProgress<double> pFeed)
    {
        string pSelected = pCombo.SelectedItem as string ?? string.Empty;
        pButton.IsEnabled = false;
        pButton.Content = LLocalization.LLocalizationTextRead("Encoder.Verification.Checking");
        LInventory.LInventoryReset();
        var pAvailable = new List<string>();
        var pRows = new List<PSVerdictRow>();
        int pTotal = PSAudioCandidates.Length;
        int pDone = 0;
        foreach (var pCandidate in PSAudioCandidates)
        {
            LTrialResult pResult = await LTrial.LTrialRun(pCandidate.PSAudioName, LTrialKind.LTrialKindAudio);
            pRows.Add(new PSVerdictRow(pCandidate.PSAudioText, pCandidate.PSAudioName, pResult.LTrialSuccess, pResult.LTrialMessage));
            if (pResult.LTrialSuccess)
            {
                pAvailable.Add(pCandidate.PSAudioText);
            }

            pDone++;
            pFeed.Report(pTotal == 0 ? 1 : (double)pDone / pTotal);
        }

        if (!pAvailable.Contains(pSelected)
            && PSAudioCandidates.Any(pCandidate => string.Equals(pCandidate.PSAudioText, pSelected, StringComparison.Ordinal)))
        {
            pAvailable.Insert(0, pSelected);
        }

        pCombo.ItemsSource = pAvailable;
        pCombo.SelectedItem = pAvailable.Contains(pSelected) ? pSelected : pAvailable.FirstOrDefault();
        PSAudioEncoderUpdate();
        psAudioResults = pRows;
        PSVerdictLogRecord("audio", pRows);
        pButton.Content = LLocalization.LLocalizationTextRead("Encoder.Button.Verify");
        pButton.IsEnabled = true;
    }

    private UIElement PSAudioPlateBuild()
    {
        var pPanel = new StackPanel();

        var pVerify = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Encoder.Button.Verify"), 84, new Thickness(8, 0, 0, 0));
        var pLog = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Encoder.Button.Result"), 64, new Thickness(6, 0, 0, 0));
        pLog.IsEnabled = psAudioResults.Count > 0;
        ProgressBar pProgress = PSFieldProgressBuild();
        var pFeed = new Progress<double>(pValue => pProgress.Value = pValue);
        pVerify.Click += async (_, _) =>
        {
            pProgress.Value = 0;
            pProgress.Visibility = Visibility.Visible;
            try
            {
                await PSAudioVerifyHandle(psAudioEncoderCombo, pVerify, pFeed);
            }
            finally
            {
                pProgress.Visibility = Visibility.Collapsed;
            }

            pLog.IsEnabled = psAudioResults.Count > 0;
        };
        pLog.Click += (_, _) => PSVerdict.PSVerdictShow(this, LLocalization.LLocalizationTextRead("Encoder.Verification.AudioTitle"), psAudioResults);
        psAudioEncoderCombo.SelectionChanged += (_, _) => PSAudioChangeHandle();
        psAudioRateCombo.SelectionChanged += (_, _) => PSAudioRowsRebuild();

        psAudioEncodePanel.Children.Add(PSFieldButtonBuild(LLocalization.LLocalizationTextRead("Encoder.Audio.Field.Encoder"), psAudioEncoderCombo, pVerify, pLog, pProgress));
        psAudioEncodePanel.Children.Add(psAudioEncoderNotice);
        psAudioEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Audio.Field.RateControl"), psAudioRateCombo));
        psAudioEncodePanel.Children.Add(psAudioRowsPanel);
        psAudioEncodePanel.Children.Add(psAudioSamplePanel);
        psAudioEncodePanel.Children.Add(psAudioChannelPanel);
        PSAudioSampleRebuild();
        PSAudioChannelRebuild();

        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Audio.Field.Stream"), psAudioStreamCombo));
        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Audio.Field.Mode"), psAudioMode));
        pPanel.Children.Add(psAudioEncodePanel);
        pPanel.Children.Add(psAudioNotice);

        psAudioStreamCombo.SelectionChanged += (_, _) => PSAudioScopeUpdate();

        PSAudioRowsRebuild();
        PSAudioScopeUpdate();
        PSAudioEncoderUpdate();
        return PSPlateBuild(pPanel);
    }

    private LCapabilityCodec PSAudioCapabilityRead() =>
        LCapability.LCapabilityAudioRead(LCapability.LCapabilityNameRead(PSComboTextRead(psAudioEncoderCombo)));

    private void PSAudioChangeHandle()
    {
        LCapabilityCodec pCodec = PSAudioCapabilityRead();
        string[] pModeLabels = pCodec.LCapabilityModeLabels;

        string pPreviousMode = PSComboTextRead(psAudioRateCombo);

        psAudioRowsBusy = true;
        psAudioRateCombo.ItemsSource = pModeLabels;
        psAudioRateCombo.SelectedItem = pModeLabels.Contains(pPreviousMode) ? pPreviousMode : pModeLabels[0];
        psAudioRowsBusy = false;

        PSAudioRowsRebuild();
        PSAudioSampleRebuild();
        PSAudioChannelRebuild();
        PSAudioEncoderUpdate();
    }

    private static readonly int[] psAudioSampleStandard =
        [8000, 11025, 16000, 22050, 24000, 32000, 44100, 48000, 88200, 96000, 176400, 192000];

    private void PSAudioSampleRebuild()
    {
        string pStored = psAudioSampleReadout is null
            ? lsExportSpecificEdit.LPresetAudio.LPresetSampleRate
            : PSAudioSampleRead();
        string pEncoder = LCapability.LCapabilityNameRead(PSComboTextRead(psAudioEncoderCombo));
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
        psAudioSamplePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Audio.Field.SampleRate"), pRow));
        psAudioSamplePanel.Children.Add(pNotice);
    }

    private string PSAudioSampleRead()
    {
        string pText = psAudioSampleReadout?.Text.Trim() ?? string.Empty;
        return int.TryParse(pText, out int pHz) && pHz > 0
            ? pHz.ToString()
            : "Same as source";
    }

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

    private void PSAudioRowsRebuild()
    {
        if (psAudioRowsBusy)
        {
            return;
        }

        psAudioRowsPanel.Children.Clear();
        psAudioQualityBox = null;
        psAudioSpeedCombo = null;
        psAudioExtraCombos.Clear();

        LCapabilityCodec pCodec = PSAudioCapabilityRead();
        LCapabilityMode pMode = pCodec.LCapabilityModeFind(PSComboTextRead(psAudioRateCombo));
        bool pModeStored = string.Equals(pMode.CapabilityModeLabel, lsExportSpecificEdit.LPresetAudio.LPresetRateControl, StringComparison.Ordinal);

        PSAudioQualityBuild(pMode, pModeStored);
        PSAudioSpeedBuild(pCodec, pModeStored);
        PSAudioExtraBuild(pCodec);

        if (!string.IsNullOrWhiteSpace(pCodec.CapabilityNotice))
        {
            psAudioRowsPanel.Children.Add(PSNoticeBuild(pCodec.CapabilityNotice));
        }
    }

    private void PSAudioQualityBuild(LCapabilityMode pMode, bool pModeStored)
    {
        if (pMode.CapabilityModeQuality is not LCapabilityQuality pQuality)
        {
            return;
        }

        string pStored = lsExportSpecificEdit.LPresetAudio.LPresetQuality;
        string pText = pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pQuality.CapabilityQualityDefault;

        psAudioQualityBox = PSEntryBuild(pText, 120);
        if (pQuality.CapabilityQualityMinimum is double pMinimum && pQuality.CapabilityQualityMaximum is double pMaximum)
        {
            UIElement pSliderRow = pQuality.LCapabilityQualityBitrate
                ? PSFaderBitrateBuild(pMinimum, pMaximum, pText, psAudioQualityBox)
                : PSFaderQualityBuild(pMinimum, pMaximum, pQuality.LCapabilityQualityStep, pText, psAudioQualityBox, pQuality.CapabilityQualityHigherBetter);
            psAudioRowsPanel.Children.Add(PSFieldBuild(pQuality.CapabilityQualityLabel, pSliderRow));
        }
        else
        {
            psAudioRowsPanel.Children.Add(PSFieldBuild(pQuality.CapabilityQualityLabel, psAudioQualityBox));
        }

        string pRange = pQuality.LCapabilityQualityRange;
        psAudioRowsPanel.Children.Add(PSNoticeBuild(string.IsNullOrEmpty(pRange)
            ? LLocalization.LLocalizationFormat("Encoder.Audio.FFmpegOption", pQuality.CapabilityQualityOption)
            : LLocalization.LLocalizationFormat("Encoder.Audio.FFmpegOptionRange", pQuality.CapabilityQualityOption, pRange)));
    }

    private void PSAudioSpeedBuild(LCapabilityCodec pCodec, bool pModeStored)
    {
        if (pCodec.CapabilitySpeed is not LCapabilitySpeed pSpeed)
        {
            return;
        }

        string pStored = lsExportSpecificEdit.LPresetAudio.LPresetSpeed;
        string pSelected = pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pSpeed.CapabilitySpeedDefault;

        psAudioSpeedCombo = PSComboBuild(pSelected, PSEncoderChoicesRead(pSpeed.CapabilitySpeedValues));
        psAudioRowsPanel.Children.Add(PSFieldBuild(pSpeed.CapabilitySpeedLabel, psAudioSpeedCombo));
    }

    private void PSAudioExtraBuild(LCapabilityCodec pCodec)
    {
        foreach (LCapabilityExtra pExtra in pCodec.LCapabilityExtraList)
        {
            string pSelected = lsExportSpecificEdit.LPresetAudio.LPresetExtras.TryGetValue(pExtra.CapabilityExtraOption, out string? pStored)
                               && pExtra.CapabilityExtraValues.Any(pChoice => string.Equals(pChoice.CapabilityChoiceValue, pStored, StringComparison.Ordinal))
                ? pStored
                : pExtra.CapabilityExtraDefault;

            ComboBox pCombo = PSComboBuild(pSelected, PSEncoderChoicesRead(pExtra.CapabilityExtraValues));
            psAudioExtraCombos[pExtra.CapabilityExtraOption] = pCombo;
            psAudioRowsPanel.Children.Add(PSFieldBuild(pExtra.CapabilityExtraLabel, pCombo));
        }
    }

    private void PSAudioScopeUpdate()
    {
        string pStream = PSComboTextRead(psAudioStreamCombo);
        string pMode = PSModeTextRead(psAudioMode);

        bool pExcluded = pStream == "Exclude" || pMode == "Exclude";
        bool pCopied = pMode == "Copy";
        bool pEncoded = !pExcluded && !pCopied;

        psAudioEncodePanel.Visibility = pEncoded ? Visibility.Visible : Visibility.Collapsed;
        psAudioNotice.Visibility = pEncoded ? Visibility.Collapsed : Visibility.Visible;
        psAudioNotice.Text = pExcluded
            ? LLocalization.LLocalizationTextRead("Encoder.Audio.Notice.Excluded")
            : LLocalization.LLocalizationTextRead("Encoder.Audio.Notice.Copied");
    }

    private static TextBlock PSAudioNoticeBuild() => new()
    {
        Foreground = PSEncoderMutedBrush,
        TextWrapping = TextWrapping.Wrap,
        Margin = PSNoticeMargin,
        Visibility = Visibility.Collapsed
    };
}
