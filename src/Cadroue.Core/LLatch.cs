using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Cadroue.Core;

internal sealed class LLatchGate
{
    internal readonly object LLatchResourceLock = new();
    internal readonly object LLatchLifetimeLock = new();
    internal int LLatchUsers;
}

public sealed class LLatchScope : IDisposable
{
    private readonly string lLatchKey;
    private readonly LLatchGate lLatchGate;
    private readonly Mutex? lLatchMutex;
    private bool lLatchDisposed;

    internal LLatchScope(string lLatchKey, LLatchGate lLatchGate, Mutex? lLatchMutex)
    {
        this.lLatchKey = lLatchKey;
        this.lLatchGate = lLatchGate;
        this.lLatchMutex = lLatchMutex;
    }

    public void Dispose()
    {
        if (lLatchDisposed)
        {
            return;
        }

        lLatchDisposed = true;
        try
        {
            lLatchMutex?.ReleaseMutex();
        }
        catch (ApplicationException)
        {
        }
        finally
        {
            lLatchMutex?.Dispose();
            Monitor.Exit(lLatchGate.LLatchResourceLock);
            LLatch.LLatchRelease(lLatchKey, lLatchGate);
        }
    }
}

public static class LLatch
{
    private static readonly ConcurrentDictionary<string, LLatchGate> lLatchGates =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly TimeSpan lLatchWaitLimit = TimeSpan.FromSeconds(5);

    public static LLatchScope LLatchClaim(string lLatchResourcePath)
    {
        string lLatchKey = Path.GetFullPath(lLatchResourcePath);
        LLatchGate lLatchGate = LLatchGateClaim(lLatchKey);
        Monitor.Enter(lLatchGate.LLatchResourceLock);

        Mutex? lLatchMutex = null;
        try
        {
            lLatchMutex = new Mutex(false, LLatchNameCreate(lLatchKey));
            try
            {
                if (!lLatchMutex.WaitOne(lLatchWaitLimit))
                {
                    throw new TimeoutException("LLatch claim timed out for " + lLatchKey);
                }
            }
            catch (AbandonedMutexException)
            {
            }

            return new LLatchScope(lLatchKey, lLatchGate, lLatchMutex);
        }
        catch
        {
            lLatchMutex?.Dispose();
            Monitor.Exit(lLatchGate.LLatchResourceLock);
            LLatchRelease(lLatchKey, lLatchGate);
            throw;
        }
    }

    private static LLatchGate LLatchGateClaim(string lLatchKey)
    {
        while (true)
        {
            LLatchGate lLatchGate = lLatchGates.GetOrAdd(lLatchKey, _ => new LLatchGate());
            lock (lLatchGate.LLatchLifetimeLock)
            {
                if (!lLatchGates.TryGetValue(lLatchKey, out LLatchGate? lLatchCurrent)
                    || !ReferenceEquals(lLatchCurrent, lLatchGate))
                {
                    continue;
                }

                lLatchGate.LLatchUsers++;
                return lLatchGate;
            }
        }
    }

    internal static void LLatchRelease(string lLatchKey, LLatchGate lLatchGate)
    {
        lock (lLatchGate.LLatchLifetimeLock)
        {
            if (--lLatchGate.LLatchUsers == 0)
            {
                lLatchGates.TryRemove(new KeyValuePair<string, LLatchGate>(lLatchKey, lLatchGate));
            }
        }
    }

    private static string LLatchNameCreate(string lLatchKey)
    {
        string lLatchHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(lLatchKey.ToUpperInvariant())));
        return @"Global\Cadroue.Latch." + lLatchHash;
    }
}
