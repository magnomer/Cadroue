using System.Text;

using Cadroue.Media;

namespace Cadroue.Tests;

internal sealed class TLosslesscut : IDisposable
{
    internal sealed record TClip(
        int TClipIndex,
        decimal? TClipStartSeconds,
        decimal? TClipEndSeconds,
        string TClipName,
        bool TClipStartSpecified,
        bool TClipEndSpecified,
        bool TClipObject);

    internal sealed class TLosslesscutProject
    {
        internal TLosslesscutProject(LLosslesscutProject project)
        {
            TLosslesscutData = project;
            TLosslesscutSegments = project.LLosslesscutProjectSegments
                .Select(segment => new TClip(
                    segment.LClipIndex,
                    segment.LClipStartSeconds,
                    segment.LClipEndSeconds,
                    segment.LClipName,
                    segment.LClipStartSpecified,
                    segment.LClipEndSpecified,
                    segment.LClipObject))
                .ToArray();
        }

        internal int? TLosslesscutVersion => TLosslesscutData.LLosslesscutProjectVersion;
        internal string TLosslesscutMedia => TLosslesscutData.LLosslesscutProjectMedia;
        internal IReadOnlyList<TClip> TLosslesscutSegments { get; }
        internal bool TLosslesscutSupported => LLosslesscut.LLosslesscutVersionCheck(TLosslesscutVersion);
        private LLosslesscutProject TLosslesscutData { get; }

        internal TLosslesscutResult TLosslesscutValidate(string sourcePath, TimeSpan duration) => TLosslesscutResultCreate(
            LLosslesscut.LLosslesscutValidate(TLosslesscutData, sourcePath, duration));
    }

    internal sealed record TSection(long TSectionStartMilliseconds, long TSectionEndMilliseconds, string TSectionName);
    internal sealed record TLosslesscutIssue(int TLosslesscutIndex, string TLosslesscutReason);
    internal sealed record TLosslesscutResult(
        int? TLosslesscutVersion,
        string TLosslesscutMedia,
        bool TLosslesscutAgreement,
        IReadOnlyList<TSection> TLosslesscutSections,
        IReadOnlyList<TLosslesscutIssue> TLosslesscutIssues);

    private readonly string tLosslesscutRoot = Path.Combine(
        Path.GetTempPath(),
        $"Cadroue-LosslessCut-{Guid.NewGuid():N}");
    private bool tLosslesscutDisposed;

    internal TLosslesscut() => Directory.CreateDirectory(tLosslesscutRoot);

    internal static TLosslesscutProject TLosslesscutParse(string text) => new(LLosslesscut.LLosslesscutParse(text));

    internal static TLosslesscutResult TLosslesscutValidate(TLosslesscutProject project, string sourcePath, TimeSpan duration) =>
        project.TLosslesscutValidate(sourcePath, duration);

    private static TLosslesscutResult TLosslesscutResultCreate(LLosslesscutResult result) =>
        new(
            result.LLosslesscutResultVersion,
            result.LLosslesscutResultMedia,
            result.LLosslesscutResultAgreement,
            result.LLosslesscutResultSections
                .Select(section => new TSection(
                    section.LSidecarStartMilliseconds,
                    section.LSidecarEndMilliseconds,
                    section.LSidecarName))
                .ToArray(),
            result.LLosslesscutResultIssues
                .Select(issue => new TLosslesscutIssue(issue.LLosslesscutIssueIndex, issue.LLosslesscutIssueReason))
                .ToArray());

    internal string TSourceCreate(string name)
    {
        string path = Path.Combine(tLosslesscutRoot, name);
        File.WriteAllBytes(path, Array.Empty<byte>());
        return path;
    }

    internal string TLosslesscutAdjacentCreate(string name, string text)
    {
        string path = Path.Combine(tLosslesscutRoot, name);
        File.WriteAllText(path, text, Encoding.UTF8);
        return path;
    }

    internal IReadOnlyList<string> TLosslesscutAdjacentRead(string sourcePath) =>
        LLosslesscut.LLosslesscutAdjacentRead(sourcePath);

    public void Dispose()
    {
        if (tLosslesscutDisposed)
        {
            return;
        }

        tLosslesscutDisposed = true;
        try
        {
            Directory.Delete(tLosslesscutRoot, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
