namespace Cadroue.UIDeportment;

public sealed class LPlayer
{
    private volatile bool lPlayerAccurateActive;
    private volatile bool lPlayerRendererPending;
    private string lPlayerFilterApplied = string.Empty;
    private string? lPlayerAudioApplied;

    public bool LPlayerAccurateActive => lPlayerAccurateActive;

    public bool LPlayerRendererPending => lPlayerRendererPending;

    public string LPlayerFilterApplied => lPlayerFilterApplied;

    public string? LPlayerAudioApplied => lPlayerAudioApplied;

    public Task LPlayerOpenStart(string lSourcePath, Action<string> lOpen) =>
        Task.Run(() => lOpen(lSourcePath));

    public bool LPlayerAccurateSet()
    {
        bool lWasRunning = lPlayerAccurateActive;
        lPlayerAccurateActive = true;
        return lWasRunning;
    }

    public void LPlayerAccurateReset() => lPlayerAccurateActive = false;

    public void LPlayerRendererSet(bool lPending) => lPlayerRendererPending = lPending;

    public bool LPlayerSeekCommit(int lSeekMilliseconds)
    {
        lPlayerAccurateActive = false;
        if (!lPlayerRendererPending || lSeekMilliseconds < 0)
        {
            return false;
        }

        lPlayerRendererPending = false;
        return true;
    }

    public void LPlayerFilterSet(string lFilter) => lPlayerFilterApplied = lFilter;

    public void LPlayerAudioSet(string? lAudio) => lPlayerAudioApplied = lAudio;

    public void LPlayerAppliedReset()
    {
        lPlayerFilterApplied = string.Empty;
        lPlayerAudioApplied = null;
    }
}
