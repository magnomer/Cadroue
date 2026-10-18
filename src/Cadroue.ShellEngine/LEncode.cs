using System.Globalization;
using System.Text;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed record LEncodeStage(
    string LEncodeStageArguments,
    LWorkStage LEncodeStageKind,
    string LEncodeStageLabel,
    string LEncodeStagePath,
    bool LEncodeStageTemporary,
    bool LEncodeStageMeasure = false,
    string LEncodeStageInput = "");

internal enum LEncodeChainMode
{
    LEncodeChainPlain,
    LEncodeChainAnalyze,
    LEncodeChainCorrection
}

public static class LEncode
{
    public const string LEncodeMeasureToken = "@@MEASURED@@";

    public const double LEncodeStatsPeriod = 0.5;

    internal static void LEncodeHeaderAppend(StringBuilder lArguments)
    {
        lArguments.Append("-hide_banner -nostdin -y");
        lArguments.Append(" -progress pipe:1 -nostats");
        lArguments.Append(CultureInfo.InvariantCulture,
            $" -stats_period {LEncodeStatsPeriod.ToString("0.###", CultureInfo.InvariantCulture)}");
    }

    internal static void LEncodeMuxerAppend(StringBuilder lArguments, LWorkItem lWorkItem)
    {
        string lMuxer = LRepertoireCatalog.LRepertoireMuxerResolve(
            lWorkItem.LWorkOutput.LEncodingContainer, lWorkItem.LWorkOutputPath);
        if (lMuxer.Length > 0)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -f {lMuxer}");
        }
    }

    internal static bool LEncodeSourceCheck(string lValue) =>
        string.IsNullOrWhiteSpace(lValue)
        || string.Equals(lValue, "Same as source", StringComparison.OrdinalIgnoreCase);

    internal static string LEncodeTimeFormat(TimeSpan lTime) =>
        lTime.TotalSeconds.ToString("0.#######", CultureInfo.InvariantCulture);

    internal static string LEncodeFormat(string lPath) => $"\"{lPath}\"";

    internal static string LEncodeValueFormat(string lValue) =>
        lValue.Any(lCharacter => char.IsWhiteSpace(lCharacter) || lCharacter == '"')
            ? LEncodeFormat(lValue.Replace("\"", "\\\""))
            : lValue;
}
