using Cadroue.Core;

namespace Cadroue.Application;

public static class LAudioName
{
    internal static string LAudioNameCreate(string lAudioSourcePath, string lAudioFolder, LEncoding lAudioOutput)
    {
        string lAudioStem = Path.GetFileNameWithoutExtension(lAudioSourcePath);
        string lAudioExtension = LEncodingPath.LEncodingExtensionResolve(lAudioOutput, lAudioSourcePath);
        string lAudioBaseName = string.IsNullOrWhiteSpace(lAudioExtension)
            ? lAudioStem
            : $"{lAudioStem}.{lAudioExtension}";

        string lAudioCandidate = Path.Combine(lAudioFolder, lAudioBaseName);
        if (!string.Equals(
                Path.GetFullPath(lAudioCandidate),
                Path.GetFullPath(lAudioSourcePath),
                StringComparison.OrdinalIgnoreCase))
        {
            return lAudioBaseName;
        }

        return string.IsNullOrWhiteSpace(lAudioExtension)
            ? $"{lAudioStem}_audio"
            : $"{lAudioStem}_audio.{lAudioExtension}";
    }
}
