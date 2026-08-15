using System.Text;

using Cadroue.Media;

namespace Cadroue.Tests;

internal sealed class TLosslessCut : IDisposable
{
    internal sealed record TClip(
        int Index,
        decimal? StartSeconds,
        decimal? EndSeconds,
        string Name,
        bool StartSpecified,
        bool EndSpecified,
        bool IsObject);

    internal sealed class TProject
    {
        internal TProject(LLosslesscutProject project)
        {
            Project = project;
            Segments = project.LLosslesscutProjectSegments
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

        internal int? Version => Project.LLosslesscutProjectVersion;
        internal string Media => Project.LLosslesscutProjectMedia;
        internal IReadOnlyList<TClip> Segments { get; }
        internal bool VersionSupported => LLosslesscut.LLosslesscutVersionCheck(Version);
        private LLosslesscutProject Project { get; }

        internal TResult Validate(string sourcePath, TimeSpan duration) => ResultCreate(
            LLosslesscut.LLosslesscutValidate(Project, sourcePath, duration));
    }

    internal sealed record TSection(long StartMilliseconds, long EndMilliseconds, string Name);
    internal sealed record TIssue(int Index, string Reason);
    internal sealed record TResult(
        int? Version,
        string Media,
        bool MediaMatch,
        IReadOnlyList<TSection> Sections,
        IReadOnlyList<TIssue> Issues);

    private readonly string tLosslessCutRoot = Path.Combine(
        Path.GetTempPath(),
        $"Cadroue-LosslessCut-{Guid.NewGuid():N}");
    private bool tLosslessCutDisposed;

    internal TLosslessCut() => Directory.CreateDirectory(tLosslessCutRoot);

    internal static TProject Parse(string text) => new(LLosslesscut.LLosslesscutParse(text));

    internal static TResult Validate(TProject project, string sourcePath, TimeSpan duration) =>
        project.Validate(sourcePath, duration);

    private static TResult ResultCreate(LLosslesscutResult result) =>
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
                .Select(issue => new TIssue(issue.LLosslesscutIssueIndex, issue.LLosslesscutIssueReason))
                .ToArray());

    internal string SourceCreate(string name)
    {
        string path = Path.Combine(tLosslessCutRoot, name);
        File.WriteAllBytes(path, Array.Empty<byte>());
        return path;
    }

    internal string AdjacentCreate(string name, string text)
    {
        string path = Path.Combine(tLosslessCutRoot, name);
        File.WriteAllText(path, text, Encoding.UTF8);
        return path;
    }

    internal IReadOnlyList<string> AdjacentRead(string sourcePath) =>
        LLosslesscut.LLosslesscutAdjacentRead(sourcePath);

    public void Dispose()
    {
        if (tLosslessCutDisposed)
        {
            return;
        }

        tLosslessCutDisposed = true;
        try
        {
            Directory.Delete(tLosslessCutRoot, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
