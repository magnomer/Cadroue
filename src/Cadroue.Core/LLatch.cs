using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Cadroue.Core;

public sealed class LLatchScope : IDisposable
{
    private readonly object lLatchGate;
    private readonly Mutex? lLatchMutex;
    private bool lLatchDisposed;

    internal LLatchScope(object lLatchGate, Mutex? lLatchMutex)
    {
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
            Monitor.Exit(lLatchGate);
        }
    }
}

public static class LLatch
{
    private static readonly ConcurrentDictionary<string, object> lLatchGates =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly TimeSpan lLatchWaitLimit = TimeSpan.FromSeconds(5);

    public static LLatchScope LLatchClaim(string lLatchResourcePath)
    {
        string lLatchKey = Path.GetFullPath(lLatchResourcePath);
        object lLatchGate = lLatchGates.GetOrAdd(lLatchKey, _ => new object());
        Monitor.Enter(lLatchGate);

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

            return new LLatchScope(lLatchGate, lLatchMutex);
        }
        catch
        {
            lLatchMutex?.Dispose();
            Monitor.Exit(lLatchGate);
            throw;
        }
    }

    private static string LLatchNameCreate(string lLatchKey)
    {
        string lLatchHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(lLatchKey.ToUpperInvariant())));
        return @"Global\Cadroue.Latch." + lLatchHash;
    }
}
