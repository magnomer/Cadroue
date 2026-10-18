using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The options window's mpv probe: whether mpv can preview, refreshed by a check that counts a failure as unusable.
public sealed class QSOptionsMpv
{
    private bool qsOptionsMpvEnabled;

    internal QSOptionsMpv(bool lMpvEnabled)
    {
        qsOptionsMpvEnabled = lMpvEnabled;
    }

    public event Action<bool>? QSOptionsMpvChanged;

    public bool QSOptionsMpvEnabled => qsOptionsMpvEnabled;

    public void QSOptionsMpvSet(bool lEnabled)
    {
        qsOptionsMpvEnabled = lEnabled;
        QSOptionsMpvChanged?.Invoke(lEnabled);
    }

    public async Task QSOptionsMpvUpdate()
    {
        bool lUsable = await QSOptionsUsableCheck();
        QSOptionsMpvSet(lUsable);
    }

    private static async Task<bool> QSOptionsUsableCheck()
    {
        try
        {
            return await LRenderer.LRendererMpvCheck();
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Options mpv check failed", lException);
            return false;
        }
    }
}
