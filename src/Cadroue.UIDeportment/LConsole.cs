namespace Cadroue.UIDeportment;

public sealed class LConsole
{
    private bool lConsoleSpinning;
    private bool lConsoleCaretReady;
    private bool lConsoleProgressPending;
    private bool lConsoleBackward;
    private double lConsoleProgress;
    private string? lConsoleReloadName;
    private string lConsoleSceneName = string.Empty;

    public bool LConsoleSpinning => lConsoleSpinning;

    public bool LConsoleCaretReady => lConsoleCaretReady;

    public bool LConsoleProgressPending => lConsoleProgressPending;

    public bool LConsoleBackward => lConsoleBackward;

    public double LConsoleProgress => lConsoleProgress;

    public string? LConsoleReloadName => lConsoleReloadName;

    public string LConsoleSceneName => lConsoleSceneName;

    public bool LConsoleSpinSet(bool lSpinning)
    {
        if (lConsoleSpinning == lSpinning)
        {
            return false;
        }

        lConsoleSpinning = lSpinning;
        return true;
    }

    public bool LConsoleCaretSet()
    {
        if (lConsoleCaretReady)
        {
            return false;
        }

        lConsoleCaretReady = true;
        return true;
    }

    public void LConsolePendingSet(bool lProgressPending) => lConsoleProgressPending = lProgressPending;

    public bool LConsoleProgressSet(double lTarget)
    {
        double lClamped = Math.Clamp(lTarget, 0, 1);
        if (lClamped.Equals(lConsoleProgress))
        {
            return false;
        }

        lConsoleBackward = lClamped < lConsoleProgress;
        lConsoleProgress = lClamped;
        return true;
    }

    public void LConsoleReloadSet(string? lReloadName) => lConsoleReloadName = lReloadName;

    public string? LConsoleReloadRead()
    {
        string? lReloadName = lConsoleReloadName;
        lConsoleReloadName = null;
        return lReloadName;
    }

    public void LConsoleSceneSet(string lSceneName) => lConsoleSceneName = lSceneName;

    public bool LConsoleSceneCheck(string lSceneName) =>
        string.Equals(lConsoleSceneName, lSceneName, StringComparison.OrdinalIgnoreCase);
}
