using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("Preset")]
public sealed class LPresetCollection { }

public sealed class TPresets : IDisposable
{
    public string NativeDefaultName => LPreset.LPresetSplitDefault;

    public void SeedNames(params string[] names)
    {
        LPreset.LPresetNames.Clear();
        foreach (string name in names)
        {
            LPreset.LPresetNames.Add(name);
        }

        LPresetSelection.LPresetLoadSeam = name => new LPresetRecord { LPresetName = name };
        LPresetSelection.LPresetRenameSeam = (old, renamed, record) => true;
        LPresetSelection.LPresetNativeSeam = name =>
            string.Equals(name, NativeDefaultName, StringComparison.OrdinalIgnoreCase);
    }

    public string CreateUniqueName(string baseName) => LPreset.LPresetNameCreate(baseName);

    public string FileName(string raw) => LPreset.LPresetFileFormat(raw);

    public string ImportName(string stored, string path) => LPreset.LPresetNameResolve(stored, path);

    public IReadOnlyList<(string Name, IReadOnlyList<string> Presets)> NativeLoad() =>
        LPresetStore.LPresetNativeLoad()
            .Select(group => (
                group.LPresetGroupName,
                (IReadOnlyList<string>)group.LPresetGroupPresets.Select(record => record.LPresetName).ToArray()))
            .ToArray();

    public IReadOnlyList<(string Name, IReadOnlyList<string> Presets)> NativeLoad(string folderPath) =>
        LPresetStore.LPresetNativeLoad(folderPath)
            .Select(group => (
                group.LPresetGroupName,
                (IReadOnlyList<string>)group.LPresetGroupPresets.Select(record => record.LPresetName).ToArray()))
            .ToArray();

    public bool NativeFormatValid() => LPresetStore.LPresetNativeLoad()
        .SelectMany(group => group.LPresetGroupPresets)
        .All(record => record.LPresetVideo is not null
            && record.LPresetAudio is not null
            && !string.IsNullOrWhiteSpace(record.LPresetDisplay));

    public void NativeSave(string name, string path) =>
        LPresetStore.LPresetFileSave(new LPresetRecord { LPresetName = name }, path);

    public (bool Ok, string SelectionName) RenameSelection(string current, string old, string renamed)
    {
        LPresetSelection selection = new(current);
        bool ok = selection.LPresetSelectionCommit(old, renamed);
        return (ok, selection.LPresetSelectionName);
    }

    public string RenameSelectionNameDuringSeam(string current, string old, string renamed)
    {
        LPresetSelection selection = new(current);
        string nameDuringSeam = string.Empty;
        LPresetSelection.LPresetRenameSeam = (_, _, _) =>
        {
            nameDuringSeam = selection.LPresetSelectionName;
            return true;
        };
        selection.LPresetSelectionCommit(old, renamed);
        return nameDuringSeam;
    }

    public void Dispose()
    {
        LPreset.LPresetNames.Clear();
        LPresetSelection.LPresetLoadSeam = null;
        LPresetSelection.LPresetRenameSeam = null;
        LPresetSelection.LPresetNativeSeam = null;
    }
}
