using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

using Xunit;

namespace Cadroue.Tests;

internal sealed record TPresetSelectionResult(bool TPresetSelectionOk, string TPresetSelectionName);

[CollectionDefinition("Preset")]
public sealed class LPresetCollection { }

public sealed class TPreset : IDisposable
{
    public string TPresetNativeName => LPreset.LPresetSplitDefault;

    public void TPresetSeedCreate(params string[] names)
    {
        LPreset.LPresetNames.Clear();
        foreach (string name in names)
        {
            LPreset.LPresetNames.Add(name);
        }

        LPresetSelection.LPresetLoadSeam = name => new LPresetRecord { LPresetName = name };
        LPresetSelection.LPresetRenameSeam = (old, renamed, record) => true;
        LPresetSelection.LPresetNativeSeam = name =>
            string.Equals(name, TPresetNativeName, StringComparison.OrdinalIgnoreCase);
    }

    public string TPresetNameCreate(string baseName) => LPreset.LPresetNameCreate(baseName);

    public string TPresetFileRead(string raw) => LPreset.LPresetFileFormat(raw);

    public string TPresetImportRead(string stored, string path) => LPreset.LPresetNameResolve(stored, path);

    public IReadOnlyList<LPresetGroup> TPresetNativeLoad() => LPresetStore.LPresetNativeLoad();

    public IReadOnlyList<LPresetGroup> TPresetNativeLoad(string folderPath) =>
        LPresetStore.LPresetNativeLoad(folderPath);

    public bool TPresetFormatCheck() => LPresetStore.LPresetNativeLoad()
        .SelectMany(group => group.LPresetGroupPresets)
        .All(record => record.LPresetVideo is not null
            && record.LPresetAudio is not null
            && !string.IsNullOrWhiteSpace(record.LPresetDisplay));

    public void TPresetNativeSave(string name, string path) =>
        LPresetStore.LPresetFileSave(new LPresetRecord { LPresetName = name }, path);

    internal TPresetSelectionResult TPresetSelectionChange(string current, string old, string renamed)
    {
        LPresetSelection selection = new(current);
        bool ok = selection.LPresetSelectionCommit(old, renamed);
        return new TPresetSelectionResult(ok, selection.LPresetSelectionName);
    }

    public string TPresetSeamChange(string current, string old, string renamed)
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
