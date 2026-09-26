using System.Globalization;
using System.Text;
using System.Text.Json;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

internal static class LScoutStream
{
    internal static async Task<LBridgeStream?> LScoutStreamRead(
        string lScoutMediaPath, CancellationToken lScoutToken = default)
    {
        if (string.IsNullOrWhiteSpace(lScoutMediaPath) || !File.Exists(lScoutMediaPath))
        {
            return null;
        }

        try
        {
            var lScoutJson = new StringBuilder();
            await new LEmployer(LTool.LToolFfprobeRead())
                .LEmployerRun(
                    LScoutStreamCreate(lScoutMediaPath),
                    lScoutToken,
                    lScoutLine => lScoutJson.AppendLine(lScoutLine))
                .ConfigureAwait(false);
            lScoutToken.ThrowIfCancellationRequested();
            return LScoutStreamParse(lScoutJson.ToString());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception lScoutException)
        {
            LRunner.LRunnerRecord(
                $"Stream properties could not be read '{Path.GetFileName(lScoutMediaPath)}'",
                lScoutException);
            return null;
        }
    }

    internal static IReadOnlyList<string> LScoutStreamCreate(string lScoutMediaPath) =>
    [
        "-v", "quiet",
        "-select_streams", "v:0",
        "-show_entries",
        "stream=codec_name,profile,pix_fmt,color_space,color_primaries,color_transfer,color_range," +
            "r_frame_rate,bit_rate,time_base",
        "-print_format", "json",
        "-i", lScoutMediaPath
    ];

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
        lScoutElement.TryGetProperty(lScoutName, out JsonElement lScoutValue)
            && lScoutValue.ValueKind == JsonValueKind.String
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
            && long.TryParse(
                lScoutValue.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long lScoutParsed)
            ? lScoutParsed
            : 0;
    }
}
