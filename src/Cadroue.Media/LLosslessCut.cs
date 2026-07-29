using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cadroue.Media;

public sealed record LLosslessCutSegment(
    int LLosslessCutSegmentIndex,
    decimal? LLosslessCutSegmentStartSeconds,
    decimal? LLosslessCutSegmentEndSeconds,
    string LLosslessCutSegmentName,
    bool LLosslessCutSegmentStartSpecified,
    bool LLosslessCutSegmentEndSpecified,
    bool LLosslessCutSegmentObject);

public sealed record LLosslessCutProject(
    int? LLosslessCutProjectVersion,
    string LLosslessCutProjectMediaFileName,
    IReadOnlyList<LLosslessCutSegment> LLosslessCutProjectSegments);

public sealed record LLosslessCutIssue(int LLosslessCutIssueIndex, string LLosslessCutIssueReason);

public sealed record LLosslessCutResult(
    int? LLosslessCutResultVersion,
    string LLosslessCutResultMediaFileName,
    bool LLosslessCutResultMediaMatch,
    IReadOnlyList<LSidecarSectionRecord> LLosslessCutResultSections,
    IReadOnlyList<LLosslessCutIssue> LLosslessCutResultIssues);

public static class LLosslessCut
{
    public const string LLosslessCutExtension = ".llc";
    private const int LLosslessCutVersionSupported = 1;

    public static LLosslessCutProject LLosslessCutRead(string lLosslessCutPath)
    {
        string lLosslessCutText = File.ReadAllText(lLosslessCutPath);
        return LLosslessCutParse(lLosslessCutText);
    }

    public static LLosslessCutProject LLosslessCutParse(string lLosslessCutText)
    {
        using var lLosslessCutStringReader = new StringReader(lLosslessCutText);
        using var lLosslessCutJsonReader = new JsonTextReader(lLosslessCutStringReader)
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Decimal
        };

        JToken lLosslessCutRoot = JToken.ReadFrom(lLosslessCutJsonReader);
        if (lLosslessCutRoot is not JObject lLosslessCutObject)
        {
            throw new JsonReaderException("The LosslessCut project root is not an object.");
        }

        int? lLosslessCutVersion = LLosslessCutIntegerRead(lLosslessCutObject["version"]);
        string lLosslessCutMediaFileName = LLosslessCutStringRead(lLosslessCutObject["mediaFileName"]);
        var lLosslessCutSegments = new List<LLosslessCutSegment>();

        if (lLosslessCutObject["cutSegments"] is JArray lLosslessCutArray)
        {
            for (int lLosslessCutIndex = 0; lLosslessCutIndex < lLosslessCutArray.Count; lLosslessCutIndex++)
            {
                if (lLosslessCutArray[lLosslessCutIndex] is not JObject lLosslessCutSegmentObject)
                {
                    lLosslessCutSegments.Add(new LLosslessCutSegment(
                        lLosslessCutIndex,
                        null,
                        null,
                        string.Empty,
                        false,
                        false,
                        false));
                    continue;
                }

                JToken? lLosslessCutStartToken = lLosslessCutSegmentObject["start"];
                JToken? lLosslessCutEndToken = lLosslessCutSegmentObject["end"];
                bool lLosslessCutStartSpecified = lLosslessCutStartToken is not null
                    && lLosslessCutStartToken.Type != JTokenType.Null
                    && lLosslessCutStartToken.Type != JTokenType.Undefined;
                bool lLosslessCutEndSpecified = lLosslessCutEndToken is not null
                    && lLosslessCutEndToken.Type != JTokenType.Null
                    && lLosslessCutEndToken.Type != JTokenType.Undefined;

                lLosslessCutSegments.Add(new LLosslessCutSegment(
                    lLosslessCutIndex,
                    LLosslessCutDecimalRead(lLosslessCutStartToken),
                    LLosslessCutDecimalRead(lLosslessCutEndToken),
                    LLosslessCutStringRead(lLosslessCutSegmentObject["name"]),
                    lLosslessCutStartSpecified,
                    lLosslessCutEndSpecified,
                    true));
            }
        }

        return new LLosslessCutProject(
            lLosslessCutVersion,
            lLosslessCutMediaFileName,
            lLosslessCutSegments);
    }

    public static LLosslessCutResult LLosslessCutValidate(
        LLosslessCutProject lLosslessCutProject,
        string lLosslessCutSourcePath,
        TimeSpan lLosslessCutDuration)
    {
        bool lLosslessCutMediaMatch = string.IsNullOrWhiteSpace(lLosslessCutProject.LLosslessCutProjectMediaFileName)
            || LLosslessCutMediaMatch(
                lLosslessCutSourcePath,
                lLosslessCutProject.LLosslessCutProjectMediaFileName);

        long lLosslessCutDurationMilliseconds = Math.Max(0, (long)Math.Round(lLosslessCutDuration.TotalMilliseconds));
        var lLosslessCutSections = new List<LSidecarSectionRecord>();
        var lLosslessCutIssues = new List<LLosslessCutIssue>();

        foreach (LLosslessCutSegment lLosslessCutSegment in lLosslessCutProject.LLosslessCutProjectSegments)
        {
            if (!lLosslessCutSegment.LLosslessCutSegmentObject)
            {
                lLosslessCutIssues.Add(new LLosslessCutIssue(
                    lLosslessCutSegment.LLosslessCutSegmentIndex,
                    "segment is not an object"));
                continue;
            }

            if (lLosslessCutSegment.LLosslessCutSegmentStartSpecified
                && lLosslessCutSegment.LLosslessCutSegmentStartSeconds is null)
            {
                lLosslessCutIssues.Add(new LLosslessCutIssue(
                    lLosslessCutSegment.LLosslessCutSegmentIndex,
                    "start is not numeric"));
                continue;
            }

            if (lLosslessCutSegment.LLosslessCutSegmentEndSpecified
                && lLosslessCutSegment.LLosslessCutSegmentEndSeconds is null)
            {
                lLosslessCutIssues.Add(new LLosslessCutIssue(
                    lLosslessCutSegment.LLosslessCutSegmentIndex,
                    "end is not numeric"));
                continue;
            }

            long lLosslessCutStartMilliseconds = 0;
            long lLosslessCutEndMilliseconds;
            try
            {
                if (lLosslessCutSegment.LLosslessCutSegmentStartSeconds is decimal lLosslessCutStartSeconds)
                {
                    lLosslessCutStartMilliseconds = LLosslessCutMillisecondsConvert(lLosslessCutStartSeconds);
                }

                if (lLosslessCutSegment.LLosslessCutSegmentEndSeconds is decimal lLosslessCutEndSeconds)
                {
                    lLosslessCutEndMilliseconds = LLosslessCutMillisecondsConvert(lLosslessCutEndSeconds);
                }
                else if (lLosslessCutDurationMilliseconds > 0)
                {
                    lLosslessCutEndMilliseconds = lLosslessCutDurationMilliseconds;
                }
                else
                {
                    lLosslessCutIssues.Add(new LLosslessCutIssue(
                        lLosslessCutSegment.LLosslessCutSegmentIndex,
                        "end is omitted and the open media duration is unavailable"));
                    continue;
                }
            }
            catch (OverflowException)
            {
                lLosslessCutIssues.Add(new LLosslessCutIssue(
                    lLosslessCutSegment.LLosslessCutSegmentIndex,
                    "time is outside the supported range"));
                continue;
            }

            if (lLosslessCutStartMilliseconds < 0)
            {
                lLosslessCutIssues.Add(new LLosslessCutIssue(
                    lLosslessCutSegment.LLosslessCutSegmentIndex,
                    "start is negative"));
                continue;
            }

            if (lLosslessCutEndMilliseconds <= lLosslessCutStartMilliseconds)
            {
                lLosslessCutIssues.Add(new LLosslessCutIssue(
                    lLosslessCutSegment.LLosslessCutSegmentIndex,
                    "end does not follow start"));
                continue;
            }

            if (lLosslessCutDurationMilliseconds > 0 && lLosslessCutEndMilliseconds > lLosslessCutDurationMilliseconds)
            {
                lLosslessCutIssues.Add(new LLosslessCutIssue(
                    lLosslessCutSegment.LLosslessCutSegmentIndex,
                    "end exceeds the open media duration"));
                continue;
            }

            lLosslessCutSections.Add(new LSidecarSectionRecord
            {
                StartMilliseconds = lLosslessCutStartMilliseconds,
                EndMilliseconds = lLosslessCutEndMilliseconds,
                ColorIndex = lLosslessCutSections.Count,
                Name = lLosslessCutSegment.LLosslessCutSegmentName,
                Prefix = string.Empty,
                Suffix = string.Empty
            });
        }

        return new LLosslessCutResult(
            lLosslessCutProject.LLosslessCutProjectVersion,
            lLosslessCutProject.LLosslessCutProjectMediaFileName,
            lLosslessCutMediaMatch,
            lLosslessCutSections,
            lLosslessCutIssues);
    }

    public static bool LLosslessCutVersionCheck(int? lLosslessCutVersion) =>
        lLosslessCutVersion is null or LLosslessCutVersionSupported;

    public static IReadOnlyList<string> LLosslessCutAdjacentRead(string lLosslessCutSourcePath)
    {
        string lLosslessCutFullPath = Path.GetFullPath(lLosslessCutSourcePath);
        string? lLosslessCutFolderPath = Path.GetDirectoryName(lLosslessCutFullPath);
        if (string.IsNullOrWhiteSpace(lLosslessCutFolderPath) || !Directory.Exists(lLosslessCutFolderPath))
        {
            return Array.Empty<string>();
        }

        IEnumerable<string> lLosslessCutCandidates;
        try
        {
            lLosslessCutCandidates = Directory
                .EnumerateFiles(
                    lLosslessCutFolderPath,
                    $"*{LLosslessCutExtension}",
                    SearchOption.TopDirectoryOnly)
                .ToArray();
        }
        catch (Exception lLosslessCutException) when (
            lLosslessCutException is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return Array.Empty<string>();
        }

        var lLosslessCutMatches = new List<string>();
        foreach (string lLosslessCutCandidatePath in lLosslessCutCandidates)
        {
            try
            {
                LLosslessCutProject lLosslessCutProject = LLosslessCutRead(lLosslessCutCandidatePath);
                if (!string.IsNullOrWhiteSpace(lLosslessCutProject.LLosslessCutProjectMediaFileName)
                    && LLosslessCutMediaMatch(
                        lLosslessCutFullPath,
                        lLosslessCutProject.LLosslessCutProjectMediaFileName))
                {
                    lLosslessCutMatches.Add(lLosslessCutCandidatePath);
                }
            }
            catch (Exception lLosslessCutException) when (
                lLosslessCutException is IOException
                    or UnauthorizedAccessException
                    or JsonException
                    or ArgumentException)
            {
                // An unrelated or malformed project must not block adjacent-project detection.
            }
        }

        return lLosslessCutMatches
            .OrderByDescending(lLosslessCutPath => File.GetLastWriteTimeUtc(lLosslessCutPath))
            .ThenBy(lLosslessCutPath => Path.GetFileName(lLosslessCutPath), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool LLosslessCutMediaMatch(
        string lLosslessCutSourcePath,
        string lLosslessCutMediaFileName) =>
        string.Equals(
            Path.GetFileName(lLosslessCutSourcePath),
            Path.GetFileName(lLosslessCutMediaFileName),
            StringComparison.OrdinalIgnoreCase);

    private static long LLosslessCutMillisecondsConvert(decimal lLosslessCutSeconds) =>
        checked((long)decimal.Round(lLosslessCutSeconds * 1000m, 0, MidpointRounding.AwayFromZero));

    private static decimal? LLosslessCutDecimalRead(JToken? lLosslessCutToken)
    {
        if (lLosslessCutToken is null || lLosslessCutToken.Type is JTokenType.Null or JTokenType.Undefined)
        {
            return null;
        }

        if (lLosslessCutToken.Type is JTokenType.Integer or JTokenType.Float)
        {
            return lLosslessCutToken.Value<decimal>();
        }

        return decimal.TryParse(
            lLosslessCutToken.Value<string>(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal lLosslessCutValue)
            ? lLosslessCutValue
            : null;
    }

    private static int? LLosslessCutIntegerRead(JToken? lLosslessCutToken)
    {
        if (lLosslessCutToken is null || lLosslessCutToken.Type is JTokenType.Null or JTokenType.Undefined)
        {
            return null;
        }

        return int.TryParse(
            lLosslessCutToken.ToString(),
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out int lLosslessCutValue)
            ? lLosslessCutValue
            : null;
    }

    private static string LLosslessCutStringRead(JToken? lLosslessCutToken) =>
        lLosslessCutToken?.Type == JTokenType.String ? lLosslessCutToken.Value<string>() ?? string.Empty : string.Empty;
}
