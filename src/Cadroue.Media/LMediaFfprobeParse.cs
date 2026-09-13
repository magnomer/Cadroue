using System.Globalization;
using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Media;

public static partial class LMedia
{
    internal static LMediaInfo LMediaFfprobeParse(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        TimeSpan duration = TimeSpan.Zero;
        TimeSpan start = TimeSpan.Zero;
        if (root.TryGetProperty("format", out JsonElement fmt)
            && fmt.TryGetProperty("duration", out JsonElement durEl)
            && double.TryParse(durEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double durSeconds))
        {
            duration = TimeSpan.FromSeconds(durSeconds);
        }

        if (fmt.ValueKind != JsonValueKind.Undefined
            && fmt.TryGetProperty("start_time", out JsonElement startEl)
            && double.TryParse(startEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double startSeconds))
        {
            start = TimeSpan.FromSeconds(startSeconds);
        }

        int videoWidth = 0, videoHeight = 0, videoRotation = 0;
        double fps = 0d;
        string videoCodec = "unknown";
        string videoPixel = "";
        string videoRange = "";
        TimeSpan videoDuration = TimeSpan.Zero;
        bool audioPresent = false;
        string audioCodec = "";
        int sampleRate = 0, channels = 0, audioBitrate = 0;

        if (root.TryGetProperty("streams", out JsonElement streams))
        {
            foreach (JsonElement stream in streams.EnumerateArray())
            {
                string? codecType = stream.TryGetProperty("codec_type", out JsonElement ct) ? ct.GetString() : null;
                if (codecType == "video" && videoWidth == 0)
                {
                    videoWidth = stream.TryGetProperty("width", out JsonElement w) ? w.GetInt32() : 0;
                    videoHeight = stream.TryGetProperty("height", out JsonElement h) ? h.GetInt32() : 0;
                    videoRotation = LMediaRotationResolve(stream);
                    if (videoRotation is 90 or 270)
                    {
                        (videoWidth, videoHeight) = (videoHeight, videoWidth);
                    }

                    videoCodec = stream.TryGetProperty("codec_name", out JsonElement cn) ? cn.GetString() ?? "unknown" : "unknown";
                    videoPixel = stream.TryGetProperty("pix_fmt", out JsonElement pf) ? pf.GetString() ?? "" : "";
                    videoRange = stream.TryGetProperty("color_range", out JsonElement cr) ? cr.GetString() ?? "" : "";
                    fps = LMediaFpsResolve(stream);
                    if (stream.TryGetProperty("duration", out JsonElement videoDurationElement)
                        && double.TryParse(
                            videoDurationElement.GetString(),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out double videoDurationSeconds))
                    {
                        videoDuration = TimeSpan.FromSeconds(videoDurationSeconds);
                    }
                }
                else if (codecType == "audio" && !audioPresent)
                {
                    audioPresent = true;
                    audioCodec = stream.TryGetProperty("codec_name", out JsonElement acn) ? acn.GetString() ?? "unknown" : "unknown";
                    if (stream.TryGetProperty("sample_rate", out JsonElement sr))
                        int.TryParse(sr.GetString(), out sampleRate);
                    channels = stream.TryGetProperty("channels", out JsonElement ch) ? ch.GetInt32() : 0;
                    if (stream.TryGetProperty("bit_rate", out JsonElement abr))
                        int.TryParse(abr.GetString(), out audioBitrate);
                }
            }
        }

        return new LMediaInfo(duration, videoWidth, videoHeight, fps, videoCodec, audioPresent, audioCodec, sampleRate, channels)
        {
            LMediaAudioBitrate = audioPresent ? audioBitrate : 0,
            LMediaStartTime = start,
            LMediaVideoDuration = videoDuration,
            LMediaVideoPixel = videoPixel,
            LMediaVideoRange = videoRange,
            LMediaVideoRotation = videoRotation
        };
    }

    private static int LMediaRotationResolve(JsonElement videoStream)
    {
        double rotation = 0d;
        if (videoStream.TryGetProperty("side_data_list", out JsonElement sideDataList)
            && sideDataList.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement sideData in sideDataList.EnumerateArray())
            {
                if (sideData.TryGetProperty("rotation", out JsonElement rotationElement)
                    && rotationElement.TryGetDouble(out double sideRotation))
                {
                    rotation = sideRotation;
                    break;
                }
            }
        }

        if (rotation == 0d
            && videoStream.TryGetProperty("tags", out JsonElement tags)
            && tags.TryGetProperty("rotate", out JsonElement rotateTag)
            && double.TryParse(
                rotateTag.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double tagRotation))
        {
            rotation = tagRotation;
        }

        int rounded = (int)Math.Round(rotation / 90d) * 90;
        return ((rounded % 360) + 360) % 360;
    }

    private static double LMediaFpsResolve(JsonElement videoStream)
    {
        string? fpsString = null;
        if (videoStream.TryGetProperty("r_frame_rate", out JsonElement rfr)) fpsString = rfr.GetString();
        if (string.IsNullOrEmpty(fpsString) || fpsString == "0/0")
            if (videoStream.TryGetProperty("avg_frame_rate", out JsonElement afr)) fpsString = afr.GetString();

        if (string.IsNullOrEmpty(fpsString)) return 0d;

        string[] parts = fpsString.Split('/');
        if (parts.Length == 2
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double num)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double den)
            && den > 0d)
        {
            return num / den;
        }

        return 0d;
    }
}
