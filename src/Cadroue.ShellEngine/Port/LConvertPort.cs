namespace Cadroue.ShellEngine;

public interface LConvertPort
{
    void LConvertRun(
        string lPresetName,
        IReadOnlyList<(string LConvertPath, Guid LConvertBatch)> lSources,
        bool lUrgent,
        Guid lRelayTarget,
        Guid lSourceTab);
}
