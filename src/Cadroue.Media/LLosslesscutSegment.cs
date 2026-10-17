using Cadroue.Core;

namespace Cadroue.Media;

public sealed record LLosslesscutIssue(int LLosslesscutIssueIndex, string LLosslesscutIssueReason);

public sealed record LLosslesscutResult(
    int? LLosslesscutResultVersion,
    string LLosslesscutResultMedia,
    bool LLosslesscutResultAgreement,
    IReadOnlyList<LSidecarSectionEntry> LLosslesscutResultSections,
    IReadOnlyList<LLosslesscutIssue> LLosslesscutResultIssues);

public static class LLosslesscutSegment
{
    public static LLosslesscutResult LLosslesscutValidate(
        LLosslesscutProject lLosslesscutProject,
        string lLosslesscutSourcePath,
        TimeSpan lLosslesscutDuration)
    {
        bool lLosslesscutMediaMatch = string.IsNullOrWhiteSpace(lLosslesscutProject.LLosslesscutProjectMedia)
            || LLosslesscut.LLosslesscutMediaMatch(
                lLosslesscutSourcePath,
                lLosslesscutProject.LLosslesscutProjectMedia);

        long lLosslesscutDurationMilliseconds = Math.Max(0, (long)Math.Round(lLosslesscutDuration.TotalMilliseconds));
        var lLosslesscutSections = new List<LSidecarSectionEntry>();
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

            lLosslesscutSections.Add(new LSidecarSectionEntry
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

    private static long LLosslesscutMillisecondsResolve(decimal lLosslesscutSeconds) =>
        checked((long)decimal.Round(lLosslesscutSeconds * 1000m, 0, MidpointRounding.AwayFromZero));
}
