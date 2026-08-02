using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cadroue.Media;

public sealed record LClip(
    int LClipIndex,
    decimal? LClipStartSeconds,
    decimal? LClipEndSeconds,
    string LClipName,
    bool LClipStartSpecified,
    bool LClipEndSpecified,
    bool LClipObject);

public sealed record LLosslesscutProject(
    int? LLosslesscutProjectVersion,
    string LLosslesscutProjectMedia,
    IReadOnlyList<LClip> LLosslesscutProjectSegments);

public sealed record LLosslesscutIssue(int LLosslesscutIssueIndex, string LLosslesscutIssueReason);

public sealed record LLosslesscutResult(
    int? LLosslesscutResultVersion,
    string LLosslesscutResultMedia,
    bool LLosslesscutResultMatch,
    IReadOnlyList<LSidecarSectionRecord> LLosslesscutResultSections,
    IReadOnlyList<LLosslesscutIssue> LLosslesscutResultIssues);

public static class LLosslesscut
{
    public const string LLosslesscutExtension = ".llc";
    private const int LLosslesscutVersionSupported = 1;

    public static LLosslesscutProject LLosslesscutRead(string lLosslesscutPath)
    {
        string lLosslesscutText = File.ReadAllText(lLosslesscutPath);
        return LLosslesscutParse(lLosslesscutText);
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

    public static LLosslesscutResult LLosslesscutValidate(
        LLosslesscutProject lLosslesscutProject,
        string lLosslesscutSourcePath,
        TimeSpan lLosslesscutDuration)
    {
        bool lLosslesscutMediaMatch = string.IsNullOrWhiteSpace(lLosslesscutProject.LLosslesscutProjectMedia)
            || LLosslesscutMediaMatch(
                lLosslesscutSourcePath,
                lLosslesscutProject.LLosslesscutProjectMedia);

        long lLosslesscutDurationMilliseconds = Math.Max(0, (long)Math.Round(lLosslesscutDuration.TotalMilliseconds));
        var lLosslesscutSections = new List<LSidecarSectionRecord>();
        var lLosslesscutIssues = new List<LLosslesscutIssue>();

        foreach (LClip lLosslesscutSegment in lLosslesscutProject.LLosslesscutProjectSegments)
        {
            if (!lLosslesscutSegment.LClipObject)
            {
                lLosslesscutIssues.Add(new LLosslesscutIssue(
                    lLosslesscutSegment.LClipIndex,
                    "segment is not an object"));
                continue;
            }

            if (lLosslesscutSegment.LClipStartSpecified
                && lLosslesscutSegment.LClipStartSeconds is null)
            {
                lLosslesscutIssues.Add(new LLosslesscutIssue(
                    lLosslesscutSegment.LClipIndex,
                    "start is not numeric"));
                continue;
            }

            if (lLosslesscutSegment.LClipEndSpecified
                && lLosslesscutSegment.LClipEndSeconds is null)
            {
                lLosslesscutIssues.Add(new LLosslesscutIssue(
                    lLosslesscutSegment.LClipIndex,
                    "end is not numeric"));
                continue;
            }

            long lLosslesscutStartMilliseconds = 0;
            long lLosslesscutEndMilliseconds;
            try
            {
                if (lLosslesscutSegment.LClipStartSeconds is decimal lLosslesscutStartSeconds)
                {
                    lLosslesscutStartMilliseconds = LLosslesscutMillisecondsResolve(lLosslesscutStartSeconds);
                }

                if (lLosslesscutSegment.LClipEndSeconds is decimal lLosslesscutEndSeconds)
                {
                    lLosslesscutEndMilliseconds = LLosslesscutMillisecondsResolve(lLosslesscutEndSeconds);
                }
                else if (lLosslesscutDurationMilliseconds > 0)
                {
                    lLosslesscutEndMilliseconds = lLosslesscutDurationMilliseconds;
                }
                else
                {
                    lLosslesscutIssues.Add(new LLosslesscutIssue(
                        lLosslesscutSegment.LClipIndex,
                        "end is omitted and the open media duration is unavailable"));
                    continue;
                }
            }
            catch (OverflowException)
            {
                lLosslesscutIssues.Add(new LLosslesscutIssue(
                    lLosslesscutSegment.LClipIndex,
                    "time is outside the supported range"));
                continue;
            }

            if (lLosslesscutStartMilliseconds < 0)
            {
                lLosslesscutIssues.Add(new LLosslesscutIssue(
                    lLosslesscutSegment.LClipIndex,
                    "start is negative"));
                continue;
            }

            if (lLosslesscutEndMilliseconds <= lLosslesscutStartMilliseconds)
            {
                lLosslesscutIssues.Add(new LLosslesscutIssue(
                    lLosslesscutSegment.LClipIndex,
                    "end does not follow start"));
                continue;
            }

            if (lLosslesscutDurationMilliseconds > 0 && lLosslesscutEndMilliseconds > lLosslesscutDurationMilliseconds)
            {
                lLosslesscutIssues.Add(new LLosslesscutIssue(
                    lLosslesscutSegment.LClipIndex,
                    "end exceeds the open media duration"));
                continue;
            }

            lLosslesscutSections.Add(new LSidecarSectionRecord
            {
                LSidecarStartMilliseconds = lLosslesscutStartMilliseconds,
                LSidecarEndMilliseconds = lLosslesscutEndMilliseconds,
                LSidecarColorIndex = lLosslesscutSections.Count,
                LSidecarName = lLosslesscutSegment.LClipName,
                LSidecarPrefix = string.Empty,
                LSidecarSuffix = string.Empty
            });
        }

        return new LLosslesscutResult(
            lLosslesscutProject.LLosslesscutProjectVersion,
            lLosslesscutProject.LLosslesscutProjectMedia,
            lLosslesscutMediaMatch,
            lLosslesscutSections,
            lLosslesscutIssues);
    }

    public static bool LLosslesscutVersionCheck(int? lLosslesscutVersion) =>
        lLosslesscutVersion is null or LLosslesscutVersionSupported;

    public static IReadOnlyList<string> LLosslesscutAdjacentRead(string lLosslesscutSourcePath)
    {
        string lLosslesscutFullPath = Path.GetFullPath(lLosslesscutSourcePath);
        string? lLosslesscutFolderPath = Path.GetDirectoryName(lLosslesscutFullPath);
        if (string.IsNullOrWhiteSpace(lLosslesscutFolderPath) || !Directory.Exists(lLosslesscutFolderPath))
        {
            return Array.Empty<string>();
        }

        IEnumerable<string> lLosslesscutCandidates;
        try
        {
            lLosslesscutCandidates = Directory
                .EnumerateFiles(
                    lLosslesscutFolderPath,
                    $"*{LLosslesscutExtension}",
                    SearchOption.TopDirectoryOnly)
                .ToArray();
        }
        catch (Exception lLosslesscutException) when (
            lLosslesscutException is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return Array.Empty<string>();
        }

        var lLosslesscutMatches = new List<string>();
        foreach (string lLosslesscutCandidatePath in lLosslesscutCandidates)
        {
            try
            {
                LLosslesscutProject lLosslesscutProject = LLosslesscutRead(lLosslesscutCandidatePath);
                if (!string.IsNullOrWhiteSpace(lLosslesscutProject.LLosslesscutProjectMedia)
                    && LLosslesscutMediaMatch(
                        lLosslesscutFullPath,
                        lLosslesscutProject.LLosslesscutProjectMedia))
                {
                    lLosslesscutMatches.Add(lLosslesscutCandidatePath);
                }
            }
            catch (Exception lLosslesscutException) when (
                lLosslesscutException is IOException
                    or UnauthorizedAccessException
                    or JsonException
                    or ArgumentException)
            {
                // An unrelated or malformed project must not block adjacent-project detection.
            }
        }

        return lLosslesscutMatches
            .OrderByDescending(lLosslesscutPath => File.GetLastWriteTimeUtc(lLosslesscutPath))
            .ThenBy(lLosslesscutPath => Path.GetFileName(lLosslesscutPath), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool LLosslesscutMediaMatch(
        string lLosslesscutSourcePath,
        string lLosslesscutMediaFileName) =>
        string.Equals(
            Path.GetFileName(lLosslesscutSourcePath),
            Path.GetFileName(lLosslesscutMediaFileName),
            StringComparison.OrdinalIgnoreCase);

    private static long LLosslesscutMillisecondsResolve(decimal lLosslesscutSeconds) =>
        checked((long)decimal.Round(lLosslesscutSeconds * 1000m, 0, MidpointRounding.AwayFromZero));

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
