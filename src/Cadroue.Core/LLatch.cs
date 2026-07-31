using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Cadroue.Core;

public sealed class LLatchScope : IDisposable
{
    private readonly object lLatchGate;
    private readonly Mutex? lLatchMutex;
    private readonly bool lLatchHeld;
    private bool lLatchDisposed;

    internal LLatchScope(object lLatchGate, Mutex? lLatchMutex, bool lLatchHeld)
    {
        this.lLatchGate = lLatchGate;
        this.lLatchMutex = lLatchMutex;
        this.lLatchHeld = lLatchHeld;
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
            if (lLatchHeld)
            {
                lLatchMutex?.ReleaseMutex();
            }
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
        bool lLatchHeld = false;
        try
        {
            lLatchMutex = new Mutex(false, LLatchNameCreate(lLatchKey));
            try
            {
                lLatchHeld = lLatchMutex.WaitOne(lLatchWaitLimit);
            }
            catch (AbandonedMutexException)
            {
                lLatchHeld = true;
            }

            return new LLatchScope(lLatchGate, lLatchMutex, lLatchHeld);
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
        return "Cadroue.Latch." + lLatchHash;
    }
}
