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
    private string? lMediaCurrentPath;
    private LMediaInfo? lMediaCurrentInfo;

    public LMediaLoad()
    {
        lMediaLoadReader = (lMediaLoadPath, lMediaLoadToken) =>
            Task.Run(
                () => LMedia.LMediaPreviewRead(lMediaLoadPath, lMediaLoadToken, LMediaLoadTail),
                lMediaLoadToken);
    }

    public bool LMediaLoadTail { get; set; } = true;

    internal LMediaLoad(Func<string, CancellationToken, Task<LMediaInfo>> lMediaLoadReader)
    {
        this.lMediaLoadReader = lMediaLoadReader ?? throw new ArgumentNullException(nameof(lMediaLoadReader));
    }

    public event Action<LMediaLoadOutcome>? LMediaLoadCompleted;

    public string? LMediaCurrentPath
    {
        get
        {
            lock (lMediaLoadGate)
            {
                return lMediaCurrentPath;
            }
        }
    }

    public LMediaInfo? LMediaCurrentInfo
    {
        get
        {
            lock (lMediaLoadGate)
            {
                return lMediaCurrentInfo;
            }
        }
    }

    public async Task<LMediaLoadOutcome> LMediaLoadStart(
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
            return LMediaLoadCancel(lMediaLoadPath ?? string.Empty, "The media path is invalid.");
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
            return LMediaLoadResolve(lMediaLoadRequestGeneration, lMediaLoadResolvedPath, "A media path is required.");
        }

        if (!File.Exists(lMediaLoadResolvedPath))
        {
            return LMediaLoadResolve(lMediaLoadRequestGeneration, lMediaLoadResolvedPath, "The media source does not exist.");
        }

        if (!LMedia.LMediaCheck(lMediaLoadResolvedPath))
        {
            return LMediaLoadResolve(lMediaLoadRequestGeneration, lMediaLoadResolvedPath, "The media source type is not supported.");
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
                    lMediaCurrentPath = lMediaLoadResolvedPath;
                    lMediaCurrentInfo = lMediaLoadInfo;
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

            return LMediaLoadResolve(
                lMediaLoadRequestGeneration,
                lMediaLoadResolvedPath,
                "The media load was cancelled.",
                LMediaLoadKind.LMediaLoadCancelled);
        }
        catch (Exception lMediaLoadException)
        {
            return LMediaLoadResolve(
                lMediaLoadRequestGeneration,
                lMediaLoadResolvedPath,
                lMediaLoadException.Message);
        }
    }

    public bool LMediaLoadClose()
    {
        LMediaLoadOutcome? lMediaLoadOutcome = null;
        lock (lMediaLoadGate)
        {
            if (lMediaLoadDisposed)
            {
                return false;
            }

            bool lMediaLoadChanged = lMediaLoadPending
                || lMediaCurrentPath is not null
                || lMediaCurrentInfo is not null;
            if (!lMediaLoadChanged)
            {
                return false;
            }

            lMediaLoadCancellation?.Cancel();
            lMediaLoadGeneration++;
            lMediaLoadPending = false;
            lMediaCurrentPath = null;
            lMediaCurrentInfo = null;
            lMediaLoadOutcome = new LMediaLoadOutcome(
                LMediaLoadKind.LMediaLoadUnloaded,
                string.Empty,
                null,
                null);
        }

        LMediaLoadRaise(lMediaLoadOutcome);
        return true;
    }

    private LMediaLoadOutcome LMediaLoadCancel(string lMediaLoadPath, string lMediaLoadError)
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

    private LMediaLoadOutcome LMediaLoadResolve(
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
            lMediaCurrentPath = null;
            lMediaCurrentInfo = null;
            lMediaLoadCancellation?.Cancel();
            lMediaLoadCancellation?.Dispose();
            lMediaLoadCancellation = null;
            LMediaLoadCompleted = null;
        }
    }
}
