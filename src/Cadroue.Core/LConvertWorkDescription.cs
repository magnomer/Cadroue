namespace Cadroue.Core;

public sealed record LConvertWorkDescription(
    IReadOnlyList<string> LConvertSourcePaths,
    LEncoding LConvertOutput,
    IReadOnlyDictionary<string, LWorkMedia>? LConvertMedia = null,
    IReadOnlyDictionary<string, Guid>? LConvertRelays = null);
