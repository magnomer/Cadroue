using Cadroue.Core;

namespace Cadroue.Application;

public sealed partial class LPreset
{
    public const string LPresetSplitDefault = "Split (default)";
    public const string LPresetMergeDefault = "Merge (default)";

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> LPresetExtensionTable =
        LRepertoireCatalog.LRepertoireExtensionTable;
}
