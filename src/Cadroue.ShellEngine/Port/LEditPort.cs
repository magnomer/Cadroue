namespace Cadroue.ShellEngine;

public interface LEditPort
{
    void LEditAllRun(
        string lPresetName,
        IReadOnlyList<(string LEditPath, Guid LEditBatch)> lSources,
        Guid lRelayTarget,
        Guid lSourceTab);

    // Merges the persistent audio plan into each path's saved plan and saves it; the paths that fail are traced.
    void LEditAudioSave(
        LInspectorPort lPersistent,
        IReadOnlyList<string> lPaths,
        bool lSkipPersistent,
        bool lSkipApply);
}
