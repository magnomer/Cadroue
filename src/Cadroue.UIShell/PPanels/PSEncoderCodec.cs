using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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

    private UIElement PSModePlateBuild() => PSPlateBuild(PSFieldBuild(LLocalization.LLocalizationTextRead("Roster.Field.Mode"), psModeCombo));

    private static string[] PSCodecItemsRead() =>
        PSCodecCandidates.Select(pCandidate => pCandidate.PSCodecText).ToArray();

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
                var pResult = await PSCodecCompatibleRead(pEncoder);
                pCandidateAvailable |= pResult.PSCodecSuccess;
                pLog.AppendLine($"  {pEncoder}: {(pResult.PSCodecSuccess ? "OK" : "FAIL")} - {pResult.PSCodecMessage}");
            }

            if (pCandidateAvailable)
            {
                pAvailable.Add(pCandidate.PSCodecText);
            }
        }

        pCombo.ItemsSource = pAvailable;
        pCombo.SelectedItem = pAvailable.Contains(pSelected) ? pSelected : pAvailable.FirstOrDefault();
        psCodecLog = pLog.ToString();
        pButton.Content = LLocalization.LLocalizationTextRead("Encoder.Button.Verify");
        pButton.IsEnabled = true;
    }

    private static async Task<(bool PSCodecSuccess, string PSCodecMessage)> PSCodecCompatibleRead(string pEncoder)
    {
        using var pProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = App.LRendererProgramCurrent,
                Arguments = $"-hide_banner -loglevel error -f lavfi -i testsrc2=size=64x64:rate=1 -frames:v 1 -an -c:v {pEncoder} -f null -",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };

        try
        {
            pProcess.Start();
            Task<string> pErrorTask = pProcess.StandardError.ReadToEndAsync();
            Task<string> pOutputTask = pProcess.StandardOutput.ReadToEndAsync();
            Task pExitTask = pProcess.WaitForExitAsync();
            if (await Task.WhenAny(pExitTask, Task.Delay(TimeSpan.FromSeconds(6))) != pExitTask)
            {
                pProcess.Kill(true);
                return (false, LLocalization.LLocalizationTextRead("Encoder.Verification.Timeout"));
            }

            string pMessage = PSCodecLogCompact(await pErrorTask, await pOutputTask);
            return (pProcess.ExitCode == 0, $"exit {pProcess.ExitCode}{pMessage}");
        }
        catch (Exception pException)
        {
            return (false, pException.Message);
        }
    }

    private static string PSCodecLogCompact(string pError, string pOutput)
    {
        string pMessage = string.IsNullOrWhiteSpace(pError) ? pOutput : pError;
        pMessage = pMessage.Replace("\r", " ").Replace("\n", " ").Trim();
        return string.IsNullOrWhiteSpace(pMessage) ? string.Empty : $": {pMessage[..Math.Min(500, pMessage.Length)]}";
    }
}
