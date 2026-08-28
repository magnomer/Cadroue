namespace Cadroue.Core;

public sealed record LFixWorkDescription(
    IReadOnlyList<string> LFixSourcePaths,
    LEncoding LFixOutput,
    IReadOnlyDictionary<string, LWorkMedia>? LFixMedia = null,
    IReadOnlyDictionary<string, Guid>? LFixRelays = null);
