namespace Cadroue.ShellEngine;

public interface LEditPort
{
    void LEditAllRun(
        string lPresetName,
        IReadOnlyList<(string LEditPath, Guid LEditBatch)> lSources,
        Guid lRelayTarget,
        Guid lSourceTab);
}
