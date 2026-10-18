namespace Cadroue.ShellEngine;

public interface LFixPort
{
    void LFixRun(
        string lPresetName,
        IReadOnlyList<(string LFixPath, Guid LFixBatch)> lSources,
        bool lUrgent,
        Guid lRelayTarget,
        Guid lSourceTab);
}
