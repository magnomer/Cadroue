namespace Cadroue.Media;

internal static class LMediaFailure
{
    internal static string LMediaFailureFormat(int exitCode, string errorText)
    {
        string diagnostic = LMediaDiagnosticNormalize(errorText);
        return $"ffprobe failed with exit code {exitCode}. {diagnostic}";
    }

    internal static string LMediaStallFormat(string errorText)
    {
        string diagnostic = LMediaDiagnosticNormalize(errorText);
        return $"ffprobe produced no output for {LMediaProcess.LMediaIdleLimit.TotalSeconds:0} seconds "
            + $"and was stopped. {diagnostic}";
    }

    internal static string LMediaEmptyFormat(string errorText)
    {
        string diagnostic = LMediaDiagnosticNormalize(errorText);
        return $"ffprobe did not return media information. {diagnostic}";
    }

    internal static string LMediaInvalidFormat(string errorText)
    {
        string diagnostic = LMediaDiagnosticNormalize(errorText);
        return $"ffprobe returned invalid media information JSON. {diagnostic}";
    }

    internal static string LMediaDiagnosticNormalize(string errorText)
    {
        string diagnostic = string.IsNullOrWhiteSpace(errorText)
            ? "No ffprobe diagnostic message was returned."
            : errorText.Trim();

        return diagnostic.Length <= 2000 ? diagnostic : diagnostic[..2000];
    }
}
