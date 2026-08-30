using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

internal static class LScoutStream
{
    internal static LBridgeStream? LScoutStreamRead(string lScoutMediaPath, CancellationToken lScoutToken = default)
    {
        if (string.IsNullOrWhiteSpace(lScoutMediaPath) || !File.Exists(lScoutMediaPath))
        {
            return null;
        }

        ProcessStartInfo lScoutStartInfo = LScoutStreamStart(lScoutMediaPath);

        Process? lScoutProcess = null;
        try
        {
            lScoutProcess = Process.Start(lScoutStartInfo);
            if (lScoutProcess is null)
            {
                return null;
            }

            LCustody.LCustodyAttach(lScoutProcess);
            using CancellationTokenRegistration lScoutKill = lScoutToken.Register(
                static p => { try { ((Process)p!).Kill(); } catch { } }, lScoutProcess);

            Task<string> lScoutError = lScoutProcess.StandardError.ReadToEndAsync();
            string lScoutJson = lScoutProcess.StandardOutput.ReadToEnd();
            lScoutProcess.WaitForExit();
            lScoutError.Wait(CancellationToken.None);
            lScoutToken.ThrowIfCancellationRequested();
            return LScoutStreamParse(lScoutJson);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception lScoutException) when (lScoutException is not OperationCanceledException)
        {
            LRunner.LRunnerRecord($"Stream properties could not be read '{Path.GetFileName(lScoutMediaPath)}'", lScoutException);
            return null;
        }
        finally
        {
            if (lScoutProcess is not null && !lScoutProcess.HasExited)
                try { lScoutProcess.Kill(); } catch { }
            lScoutProcess?.Dispose();
        }
    }

    internal static ProcessStartInfo LScoutStreamStart(string lScoutMediaPath)
    {
        var lScoutStartInfo = new ProcessStartInfo(LTool.LToolFfprobeRead())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        lScoutStartInfo.ArgumentList.Add("-v");
        lScoutStartInfo.ArgumentList.Add("quiet");
        lScoutStartInfo.ArgumentList.Add("-select_streams");
        lScoutStartInfo.ArgumentList.Add("v:0");
        lScoutStartInfo.ArgumentList.Add("-show_entries");
        lScoutStartInfo.ArgumentList.Add(
            "stream=codec_name,profile,pix_fmt,color_space,color_primaries,color_transfer,color_range,r_frame_rate,bit_rate,time_base");
        lScoutStartInfo.ArgumentList.Add("-print_format");
        lScoutStartInfo.ArgumentList.Add("json");
        lScoutStartInfo.ArgumentList.Add("-i");
        lScoutStartInfo.ArgumentList.Add(lScoutMediaPath);
        return lScoutStartInfo;
    }

    private static LBridgeStream? LScoutStreamParse(string lScoutJson)
    {
        if (string.IsNullOrWhiteSpace(lScoutJson))
        {
            return null;
        }

        using JsonDocument lScoutDocument = JsonDocument.Parse(lScoutJson);
        if (!lScoutDocument.RootElement.TryGetProperty("streams", out JsonElement lScoutStreams)
            || lScoutStreams.ValueKind != JsonValueKind.Array
            || lScoutStreams.GetArrayLength() == 0)
        {
            return null;
        }

        JsonElement lScoutStream = lScoutStreams[0];

        return new LBridgeStream(
            LScoutTextRead(lScoutStream, "codec_name"),
            LScoutTextRead(lScoutStream, "profile"),
            LScoutTextRead(lScoutStream, "pix_fmt"),
            LScoutTextRead(lScoutStream, "color_space"),
            LScoutTextRead(lScoutStream, "color_primaries"),
            LScoutTextRead(lScoutStream, "color_transfer"),
            LScoutTextRead(lScoutStream, "color_range"),
            LScoutTextRead(lScoutStream, "r_frame_rate"),
            LScoutLongRead(lScoutStream, "bit_rate"),
            LScoutTextRead(lScoutStream, "time_base"));
    }

    private static string LScoutTextRead(JsonElement lScoutElement, string lScoutName) =>
        lScoutElement.TryGetProperty(lScoutName, out JsonElement lScoutValue) && lScoutValue.ValueKind == JsonValueKind.String
            ? lScoutValue.GetString() ?? string.Empty
            : string.Empty;

    private static long LScoutLongRead(JsonElement lScoutElement, string lScoutName)
    {
        if (!lScoutElement.TryGetProperty(lScoutName, out JsonElement lScoutValue))
        {
            return 0;
        }

        if (lScoutValue.ValueKind == JsonValueKind.Number && lScoutValue.TryGetInt64(out long lScoutInteger))
        {
            return lScoutInteger;
        }

        return lScoutValue.ValueKind == JsonValueKind.String
            && long.TryParse(lScoutValue.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long lScoutParsed)
            ? lScoutParsed
            : 0;
    }
}
