using System.Text;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Infrastructure;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private static readonly (string PSCodecText, string[] PSCodecValues)[] PSCodecCandidates =
    [
        ("H.264, x264 / libx264", ["libx264"]), ("H.264, Media Foundation / h264_mf", ["h264_mf"]),
        ("H.264, OpenH264 / libopenh264", ["libopenh264"]), ("H.264, Intel QSV / h264_qsv", ["h264_qsv"]),
        ("H.264, AMD AMF / h264_amf", ["h264_amf"]), ("H.264, NVIDIA NVENC / h264_nvenc", ["h264_nvenc"]),
        ("H.265, x265 / libx265", ["libx265"]), ("H.265, Intel QSV / hevc_qsv", ["hevc_qsv"]),
        ("H.265, AMD AMF / hevc_amf", ["hevc_amf"]), ("H.265, Media Foundation / hevc_mf", ["hevc_mf"]),
        ("H.265, NVIDIA NVENC / hevc_nvenc", ["hevc_nvenc"]), ("H.266/VVC, vvenc / libvvenc", ["libvvenc"]),
        ("AV1, AOM / libaom-av1", ["libaom-av1"]), ("AV1, SVT-AV1 / libsvtav1", ["libsvtav1"]),
        ("AV1, rav1e / librav1e", ["librav1e"]), ("AV1, Intel QSV / av1_qsv", ["av1_qsv"]),
        ("AV1, AMD AMF / av1_amf", ["av1_amf"]), ("AV1, NVIDIA NVENC / av1_nvenc", ["av1_nvenc"]),
        ("VP8, libvpx / libvpx / libvpx-vp8", ["libvpx", "libvpx-vp8"]), ("VP9, libvpx / libvpx-vp9", ["libvpx-vp9"]),
        ("VP9, Intel QSV / vp9_qsv", ["vp9_qsv"]), ("MPEG-4 Part 2, Xvid / libxvid", ["libxvid"]),
        ("MPEG-4 Part 2, native / mpeg4", ["mpeg4"]), ("Theora, libtheora / libtheora", ["libtheora"]),
        ("ProRes, native / prores", ["prores"]), ("ProRes, Anatoliy / prores_aw", ["prores_aw"]),
        ("ProRes, Kostya / prores_ks", ["prores_ks"]), ("FFV1, native / ffv1", ["ffv1"]),
        ("MJPEG, native / mjpeg", ["mjpeg"]), ("JPEG 2000, native / jpeg2000", ["jpeg2000"]),
        ("JPEG 2000, OpenJPEG / libopenjpeg", ["libopenjpeg"]), ("WebP, libwebp / libwebp", ["libwebp"]),
        ("WebP, animated libwebp / libwebp_anim", ["libwebp_anim"]), ("EVC, XEVE / libxeve", ["libxeve"]),
        ("AVS2, xavs2 / libxavs2", ["libxavs2"]), ("APV, OpenAPV / liboapv", ["liboapv"])
    ];

    private static readonly string[] PSCodecContainerNames =
        ["MP4", "Matroska", "MOV", "WebM", "AVI", "MPEG-TS", "FLV", "Ogg"];

    private static readonly Dictionary<string, string[]> PSCodecContainerTable = new(StringComparer.Ordinal)
    {
        ["H.264"] = ["MP4", "Matroska", "MOV", "AVI", "MPEG-TS", "FLV"],
        ["H.265"] = ["MP4", "Matroska", "MOV", "MPEG-TS"],
        ["H.266/VVC"] = ["Matroska", "MPEG-TS"],
        ["AV1"] = ["MP4", "Matroska", "MOV", "WebM"],
        ["VP8"] = ["Matroska", "WebM"],
        ["VP9"] = ["MP4", "Matroska", "WebM"],
        ["MPEG-4 Part 2"] = ["MP4", "Matroska", "MOV", "AVI"],
        ["Theora"] = ["Matroska", "Ogg"],
        ["ProRes"] = ["Matroska", "MOV"],
        ["FFV1"] = ["Matroska", "AVI"],
        ["MJPEG"] = ["MP4", "Matroska", "MOV", "AVI"],
        ["JPEG 2000"] = ["Matroska", "MOV", "AVI"],
        ["EVC"] = ["MP4", "Matroska"],
        ["AVS2"] = ["Matroska"],
        ["APV"] = ["MP4", "Matroska"]
    };

    private UIElement PSModePlateBuild() => PSPlateBuild(PSFieldBuild(LLocalization.LLocalizationTextRead("Roster.Field.Mode"), psModeCombo));

    private static HashSet<string>? psCodecAvailable;
    private static Task? psCodecProbeTask;

    internal static void PSCodecProbeStart() => psCodecProbeTask = Task.Run(PSCodecProbeRun);

    private static async Task PSCodecProbeRun()
    {
        var pAvailable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pCandidate in PSCodecCandidates)
        {
            foreach (string pEncoder in pCandidate.PSCodecValues)
            {
                if ((await LTrial.LTrialRun(pEncoder, LTrialKind.LTrialKindVideo)).LTrialSuccess)
                {
                    pAvailable.Add(pEncoder);
                }
            }
        }

        if (pAvailable.Count > 0)
        {
            psCodecAvailable = pAvailable;
        }
    }

    private void PSCodecRefreshArrange()
    {
        if (psCodecProbeTask is { IsCompleted: false } pTask)
        {
            pTask.ContinueWith(
                _ => Dispatcher.BeginInvoke(() => { if (IsLoaded) PSCodecContainerHandle(); }),
                TaskScheduler.Default);
        }
    }

    private static string[] PSCodecItemsRead() =>
        PSCodecCandidates
            .Where(PSCodecAvailableCheck)
            .Select(pCandidate => pCandidate.PSCodecText)
            .ToArray();

    private static string[] PSCodecItemsRead(string pContainer)
    {
        if (!PSCodecContainerNames.Contains(pContainer))
        {
            return PSCodecItemsRead();
        }

        return PSCodecCandidates
            .Where(pCandidate => PSCodecContainerCheck(pCandidate.PSCodecText, pContainer) && PSCodecAvailableCheck(pCandidate))
            .Select(pCandidate => pCandidate.PSCodecText)
            .ToArray();
    }

    private static bool PSCodecAvailableCheck((string PSCodecText, string[] PSCodecValues) pCandidate) =>
        psCodecAvailable is not { } pSet || pCandidate.PSCodecValues.Any(pSet.Contains);

    private static bool PSCodecContainerCheck(string pText, string pContainer) =>
        PSCodecContainerTable.TryGetValue(pText.Split(',')[0].Trim(), out string[]? pContainers)
        && pContainers.Contains(pContainer);

    private void PSCodecContainerHandle()
    {
        string pContainer = PSComboTextRead(psOutputContainerCombo);
        string pCurrent = psVideoEncoderCombo.SelectedItem as string ?? string.Empty;
        string[] pItems = PSCodecItemsRead(pContainer);
        psVideoEncoderCombo.ItemsSource = pItems;
        psVideoEncoderCombo.SelectedItem = pItems.Contains(pCurrent) ? pCurrent : pItems.FirstOrDefault();
    }

    private static string PSCodecValueRead(string pText)
    {
        foreach (var pCandidate in PSCodecCandidates)
        {
            if (string.Equals(pCandidate.PSCodecText, pText, StringComparison.Ordinal))
            {
                return pCandidate.PSCodecValues.FirstOrDefault() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private async Task PSCodecVerifyHandle(ComboBox pCombo, Button pButton)
    {
        string pSelected = pCombo.SelectedItem as string ?? string.Empty;
        pButton.IsEnabled = false;
        pButton.Content = LLocalization.LLocalizationTextRead("Encoder.Verification.Checking");
        var pAvailable = new List<string>();
        var pAvailableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pLog = new StringBuilder();
        pLog.AppendLine($"Verification: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        pLog.AppendLine("Command pattern: ffmpeg -hide_banner -loglevel error -f lavfi -i testsrc2=size=64x64:rate=1 -frames:v 1 -an -c:v <encoder> -f null -");
        foreach (var pCandidate in PSCodecCandidates)
        {
            bool pCandidateAvailable = false;
            pLog.AppendLine();
            pLog.AppendLine(pCandidate.PSCodecText);
            foreach (string pEncoder in pCandidate.PSCodecValues)
            {
                LTrialResult pResult = await LTrial.LTrialRun(pEncoder, LTrialKind.LTrialKindVideo);
                pCandidateAvailable |= pResult.LTrialSuccess;
                if (pResult.LTrialSuccess)
                {
                    pAvailableNames.Add(pEncoder);
                }

                pLog.AppendLine($"  {pEncoder}: {(pResult.LTrialSuccess ? "OK" : "FAIL")} - {pResult.LTrialMessage}");
            }

            if (pCandidateAvailable)
            {
                pAvailable.Add(pCandidate.PSCodecText);
            }
        }

        psCodecAvailable = pAvailableNames.Count > 0 ? pAvailableNames : psCodecAvailable;
        pCombo.ItemsSource = pAvailable;
        pCombo.SelectedItem = pAvailable.Contains(pSelected) ? pSelected : pAvailable.FirstOrDefault();
        psCodecLog = pLog.ToString();
        pButton.Content = LLocalization.LLocalizationTextRead("Encoder.Button.Verify");
        pButton.IsEnabled = true;
    }
}
