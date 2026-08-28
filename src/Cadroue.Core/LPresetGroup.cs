namespace Cadroue.Core;

public sealed record LPresetGroup(
    string LPresetGroupName,
    IReadOnlyList<LPresetRecord> LPresetGroupPresets);
