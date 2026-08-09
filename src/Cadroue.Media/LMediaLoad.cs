using Cadroue.Core;

namespace Cadroue.Media;

public enum LMediaLoadKind
{
    LMediaLoadSuccess,
    LMediaLoadFailure,
    LMediaLoadCancelled,
    LMediaLoadUnloaded,
    LMediaLoadObsolete
}

public sealed record LMediaLoadOutcome(
    LMediaLoadKind LMediaLoadKind,
    string LMediaLoadPath,
    LMediaInfo? LMediaLoadInfo,
    string? LMediaLoadError);

/// <summary>
/// Owns the backend lifecycle of the current media source. A load is one operation:
/// validation, probing, cancellation, ordering, and committing current identity happen here.
/// </summary>
public sealed class LMediaLoad : IDisposable
{
    private readonly object lMediaLoadGate = new();
    private readonly Func<string, CancellationToken, Task<LMediaInfo>> lMediaLoadReader;
    private CancellationTokenSource? lMediaLoadCancellation;
    private long lMediaLoadGeneration;
    private bool lMediaLoadDisposed;
    private bool lMediaLoadPending;
    private string? lMediaLoadCurrentPath;
    private LMediaInfo? lMediaLoadCurrentInfo;

    public LMediaLoad()
        : this((lMediaLoadPath, lMediaLoadToken) =>
            Task.Run(() => LMedia.LMediaFfprobeRead(lMediaLoadPath, lMediaLoadToken), lMediaLoadToken))
    {
    }

    internal LMediaLoad(Func<string, CancellationToken, Task<LMediaInfo>> lMediaLoadReader)
    {
        this.lMediaLoadReader = lMediaLoadReader ?? throw new ArgumentNullException(nameof(lMediaLoadReader));
    }

    public event Action<LMediaLoadOutcome>? LMediaLoadCompleted;

    public string? LMediaLoadCurrentPath
    {
        get
        {
            lock (lMediaLoadGate)
            {
                return lMediaLoadCurrentPath;
            }
        }
    }

    public LMediaInfo? LMediaLoadCurrentInfo
    {
        get
        {
            lock (lMediaLoadGate)
            {
                return lMediaLoadCurrentInfo;
            }
        }
    }

    public async Task<LMediaLoadOutcome> LMediaLoadAsync(
        string lMediaLoadPath,
        CancellationToken lMediaLoadToken = default)
    {
        string lMediaLoadResolvedPath;
        try
        {
            lMediaLoadResolvedPath = string.IsNullOrWhiteSpace(lMediaLoadPath)
                ? string.Empty
                : Path.GetFullPath(lMediaLoadPath);
        }
        catch (Exception lMediaLoadException) when (lMediaLoadException is ArgumentException or NotSupportedException)
        {
            return LMediaLoadReject(lMediaLoadPath ?? string.Empty, "The media path is invalid.");
        }

        CancellationTokenSource lMediaLoadRequest;
        long lMediaLoadRequestGeneration;
        lock (lMediaLoadGate)
        {
            ObjectDisposedException.ThrowIf(lMediaLoadDisposed, this);
            lMediaLoadCancellation?.Cancel();
            lMediaLoadCancellation?.Dispose();
            lMediaLoadCancellation = CancellationTokenSource.CreateLinkedTokenSource(lMediaLoadToken);
            lMediaLoadRequest = lMediaLoadCancellation;
            lMediaLoadRequestGeneration = ++lMediaLoadGeneration;
            lMediaLoadPending = true;
        }

        if (string.IsNullOrWhiteSpace(lMediaLoadResolvedPath))
        {
            return LMediaLoadFailCurrent(lMediaLoadRequestGeneration, lMediaLoadResolvedPath, "A media path is required.");
        }

        if (!File.Exists(lMediaLoadResolvedPath))
        {
            return LMediaLoadFailCurrent(lMediaLoadRequestGeneration, lMediaLoadResolvedPath, "The media source does not exist.");
        }

        if (!LMedia.LMediaCheck(lMediaLoadResolvedPath))
        {
            return LMediaLoadFailCurrent(lMediaLoadRequestGeneration, lMediaLoadResolvedPath, "The media source type is not supported.");
        }

        try
        {
            LMediaInfo lMediaLoadInfo = await lMediaLoadReader(
                lMediaLoadResolvedPath,
                lMediaLoadRequest.Token).ConfigureAwait(false);

            LMediaLoadOutcome lMediaLoadOutcome;
            lock (lMediaLoadGate)
            {
                if (lMediaLoadDisposed || lMediaLoadRequestGeneration != lMediaLoadGeneration)
                {
                    return new LMediaLoadOutcome(
                        LMediaLoadKind.LMediaLoadObsolete,
                        lMediaLoadResolvedPath,
                        null,
                        null);
                }

                if (lMediaLoadRequest.IsCancellationRequested)
                {
                    lMediaLoadPending = false;
                    lMediaLoadOutcome = new LMediaLoadOutcome(
                        LMediaLoadKind.LMediaLoadCancelled,
                        lMediaLoadResolvedPath,
                        null,
                        "The media load was cancelled.");
                }
                else
                {
                    lMediaLoadPending = false;
                    lMediaLoadCurrentPath = lMediaLoadResolvedPath;
                    lMediaLoadCurrentInfo = lMediaLoadInfo;
                    lMediaLoadOutcome = new LMediaLoadOutcome(
                        LMediaLoadKind.LMediaLoadSuccess,
                        lMediaLoadResolvedPath,
                        lMediaLoadInfo,
                        null);
                }
            }

            LMediaLoadRaise(lMediaLoadOutcome);
            return lMediaLoadOutcome;
        }
        catch (OperationCanceledException)
        {
            lock (lMediaLoadGate)
            {
                if (lMediaLoadDisposed || lMediaLoadRequestGeneration != lMediaLoadGeneration)
                {
                    return new LMediaLoadOutcome(
                        LMediaLoadKind.LMediaLoadObsolete,
                        lMediaLoadResolvedPath,
                        null,
                        null);
                }
            }

            return LMediaLoadFailCurrent(
                lMediaLoadRequestGeneration,
                lMediaLoadResolvedPath,
                "The media load was cancelled.",
                LMediaLoadKind.LMediaLoadCancelled);
        }
        catch (Exception lMediaLoadException)
        {
            return LMediaLoadFailCurrent(
                lMediaLoadRequestGeneration,
                lMediaLoadResolvedPath,
                lMediaLoadException.Message);
        }
    }

    public bool LMediaUnload()
    {
        LMediaLoadOutcome? lMediaLoadOutcome = null;
        lock (lMediaLoadGate)
        {
            if (lMediaLoadDisposed)
            {
                return false;
            }

            bool lMediaLoadChanged = lMediaLoadPending
                || lMediaLoadCurrentPath is not null
                || lMediaLoadCurrentInfo is not null;
            if (!lMediaLoadChanged)
            {
                return false;
            }

            lMediaLoadCancellation?.Cancel();
            lMediaLoadGeneration++;
            lMediaLoadPending = false;
            lMediaLoadCurrentPath = null;
            lMediaLoadCurrentInfo = null;
            lMediaLoadOutcome = new LMediaLoadOutcome(
                LMediaLoadKind.LMediaLoadUnloaded,
                string.Empty,
                null,
                null);
        }

        LMediaLoadRaise(lMediaLoadOutcome);
        return true;
    }

    private LMediaLoadOutcome LMediaLoadReject(string lMediaLoadPath, string lMediaLoadError)
    {
        lock (lMediaLoadGate)
        {
            ObjectDisposedException.ThrowIf(lMediaLoadDisposed, this);
            lMediaLoadCancellation?.Cancel();
            lMediaLoadGeneration++;
            lMediaLoadPending = false;
        }

        var lMediaLoadOutcome = new LMediaLoadOutcome(
            LMediaLoadKind.LMediaLoadFailure,
            lMediaLoadPath,
            null,
            lMediaLoadError);
        LMediaLoadRaise(lMediaLoadOutcome);
        return lMediaLoadOutcome;
    }

    private LMediaLoadOutcome LMediaLoadFailCurrent(
        long lMediaLoadRequestGeneration,
        string lMediaLoadPath,
        string lMediaLoadError,
        LMediaLoadKind lMediaLoadKind = LMediaLoadKind.LMediaLoadFailure)
    {
        lock (lMediaLoadGate)
        {
            if (lMediaLoadDisposed || lMediaLoadRequestGeneration != lMediaLoadGeneration)
            {
                return new LMediaLoadOutcome(
                    LMediaLoadKind.LMediaLoadObsolete,
                    lMediaLoadPath,
                    null,
                    null);
            }

            lMediaLoadPending = false;
        }

        var lMediaLoadOutcome = new LMediaLoadOutcome(
            lMediaLoadKind,
            lMediaLoadPath,
            null,
            lMediaLoadError);
        LMediaLoadRaise(lMediaLoadOutcome);
        return lMediaLoadOutcome;
    }

    private void LMediaLoadRaise(LMediaLoadOutcome lMediaLoadOutcome)
    {
        Delegate[] lMediaLoadHandlers = LMediaLoadCompleted?.GetInvocationList() ?? Array.Empty<Delegate>();
        foreach (Action<LMediaLoadOutcome> lMediaLoadHandler in lMediaLoadHandlers.Cast<Action<LMediaLoadOutcome>>())
        {
            try
            {
                lMediaLoadHandler(lMediaLoadOutcome);
            }
            catch
            {
            }
        }
    }

    public void Dispose()
    {
        lock (lMediaLoadGate)
        {
            if (lMediaLoadDisposed)
            {
                return;
            }

            lMediaLoadDisposed = true;
            lMediaLoadGeneration++;
            lMediaLoadPending = false;
            lMediaLoadCurrentPath = null;
            lMediaLoadCurrentInfo = null;
            lMediaLoadCancellation?.Cancel();
            lMediaLoadCancellation?.Dispose();
            lMediaLoadCancellation = null;
            LMediaLoadCompleted = null;
        }
    }
}
