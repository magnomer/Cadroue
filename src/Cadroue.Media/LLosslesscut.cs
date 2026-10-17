using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cadroue.Media;

public sealed record LLosslesscutProject(
    int? LLosslesscutProjectVersion,
    string LLosslesscutProjectMedia,
    IReadOnlyList<LClip> LLosslesscutProjectSegments);

public static class LLosslesscut
{
    public const string LLosslesscutExtension = ".llc";
    private const int LLosslesscutVersionSupported = 1;

    public static DateTime LLosslesscutStampRead(string lLosslesscutPath) =>
        File.GetLastWriteTime(lLosslesscutPath);

    public static LLosslesscutProject LLosslesscutRead(string lLosslesscutPath)
    {
        string lLosslesscutText = File.ReadAllText(lLosslesscutPath);
        return LLosslesscutParse(lLosslesscutText);
    }

    public static LLosslesscutProject? LLosslesscutProjectRead(
        string lLosslesscutPath,
        out Exception? lLosslesscutFault)
    {
        try
        {
            lLosslesscutFault = null;
            return LLosslesscutRead(lLosslesscutPath);
        }
        catch (Exception lLosslesscutException) when (
            lLosslesscutException is IOException
                or UnauthorizedAccessException
                or JsonException
                or ArgumentException)
        {
            lLosslesscutFault = lLosslesscutException;
            return null;
        }
    }

    public static LLosslesscutProject LLosslesscutParse(string lLosslesscutText)
    {
        using var lLosslesscutStringReader = new StringReader(lLosslesscutText);
        using var lLosslesscutJsonReader = new JsonTextReader(lLosslesscutStringReader)
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Decimal
        };

        JToken lLosslesscutRoot = JToken.ReadFrom(lLosslesscutJsonReader);
        if (lLosslesscutRoot is not JObject lLosslesscutObject)
        {
            throw new JsonReaderException("The LosslessCut project root is not an object.");
        }

        int? lLosslesscutVersion = LLosslesscutIntegerRead(lLosslesscutObject["version"]);
        string lLosslesscutMediaFileName = LLosslesscutStringRead(lLosslesscutObject["mediaFileName"]);
        var lLosslesscutSegments = new List<LClip>();

        if (lLosslesscutObject["cutSegments"] is JArray lLosslesscutArray)
        {
            for (int lLosslesscutIndex = 0; lLosslesscutIndex < lLosslesscutArray.Count; lLosslesscutIndex++)
            {
                if (lLosslesscutArray[lLosslesscutIndex] is not JObject lLosslesscutSegmentObject)
                {
                    lLosslesscutSegments.Add(new LClip(
                        lLosslesscutIndex,
                        null,
                        null,
                        string.Empty,
                        false,
                        false,
                        false));
                    continue;
                }

                JToken? lLosslesscutStartToken = lLosslesscutSegmentObject["start"];
                JToken? lLosslesscutEndToken = lLosslesscutSegmentObject["end"];
                bool lLosslesscutStartSpecified = lLosslesscutStartToken is not null
                    && lLosslesscutStartToken.Type != JTokenType.Null
                    && lLosslesscutStartToken.Type != JTokenType.Undefined;
                bool lLosslesscutEndSpecified = lLosslesscutEndToken is not null
                    && lLosslesscutEndToken.Type != JTokenType.Null
                    && lLosslesscutEndToken.Type != JTokenType.Undefined;

                lLosslesscutSegments.Add(new LClip(
                    lLosslesscutIndex,
                    LLosslesscutDecimalRead(lLosslesscutStartToken),
                    LLosslesscutDecimalRead(lLosslesscutEndToken),
                    LLosslesscutStringRead(lLosslesscutSegmentObject["name"]),
                    lLosslesscutStartSpecified,
                    lLosslesscutEndSpecified,
                    true));
            }
        }

        return new LLosslesscutProject(
            lLosslesscutVersion,
            lLosslesscutMediaFileName,
            lLosslesscutSegments);
    }

    public static bool LLosslesscutVersionCheck(int? lLosslesscutVersion) =>
        lLosslesscutVersion is null or LLosslesscutVersionSupported;

    public static string LLosslesscutPathNormalize(string lLosslesscutSourcePath) =>
        Path.GetFullPath(lLosslesscutSourcePath);

    internal static bool LLosslesscutMediaMatch(
        string lLosslesscutSourcePath,
        string lLosslesscutMediaFileName) =>
        string.Equals(
            Path.GetFileName(lLosslesscutSourcePath),
            Path.GetFileName(lLosslesscutMediaFileName),
            StringComparison.OrdinalIgnoreCase);

    private static decimal? LLosslesscutDecimalRead(JToken? lLosslesscutToken)
    {
        if (lLosslesscutToken is null || lLosslesscutToken.Type is JTokenType.Null or JTokenType.Undefined)
        {
            return null;
        }

        if (lLosslesscutToken.Type is JTokenType.Integer or JTokenType.Float)
        {
            return lLosslesscutToken.Value<decimal>();
        }

        return decimal.TryParse(
            lLosslesscutToken.Value<string>(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal lLosslesscutValue)
            ? lLosslesscutValue
            : null;
    }

    private static int? LLosslesscutIntegerRead(JToken? lLosslesscutToken)
    {
        if (lLosslesscutToken is null || lLosslesscutToken.Type is JTokenType.Null or JTokenType.Undefined)
        {
            return null;
        }

        return int.TryParse(
            lLosslesscutToken.ToString(),
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out int lLosslesscutValue)
            ? lLosslesscutValue
            : null;
    }

    private static string LLosslesscutStringRead(JToken? lLosslesscutToken) =>
        lLosslesscutToken?.Type == JTokenType.String ? lLosslesscutToken.Value<string>() ?? string.Empty : string.Empty;
}
