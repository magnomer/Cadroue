using System.Globalization;
using System.Text.Json;

namespace Cadroue.ShellEngine;

public static class LEncodeLoudnorm
{
    public static string LEncodeLoudnormRead(string lStderr)
    {
        int lStart = lStderr.LastIndexOf('{');
        int lEnd = lStderr.LastIndexOf('}');
        if (lStart < 0 || lEnd <= lStart)
        {
            return string.Empty;
        }

        try
        {
            Dictionary<string, string>? lValues =
                JsonSerializer.Deserialize<Dictionary<string, string>>(lStderr.Substring(lStart, lEnd - lStart + 1));
            if (lValues is null
                || !lValues.TryGetValue("input_i", out string? lInputI)
                || !lValues.TryGetValue("input_tp", out string? lInputTp)
                || !lValues.TryGetValue("input_lra", out string? lInputLra)
                || !lValues.TryGetValue("input_thresh", out string? lInputThresh)
                || !lValues.TryGetValue("target_offset", out string? lTargetOffset)
                || !LEncodeMeasuredCheck(lInputI)
                || !LEncodeMeasuredCheck(lInputTp)
                || !LEncodeMeasuredCheck(lInputLra)
                || !LEncodeMeasuredCheck(lInputThresh)
                || !LEncodeMeasuredCheck(lTargetOffset))
            {
                return string.Empty;
            }

            return $":measured_I={lInputI}:measured_TP={lInputTp}:measured_LRA={lInputLra}:" +
                $"measured_thresh={lInputThresh}:offset={lTargetOffset}:linear=true";
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static bool LEncodeMeasuredCheck(string lValue) =>
        double.TryParse(lValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double lMeasured)
        && double.IsFinite(lMeasured);
}
