using System.IO;
using System.Text;
using Cadroue.Core;

namespace Cadroue.UIShell;

public static class LAppLog
{
    private static readonly object lLogLock = new();

    public const string LLogFolderName = "log";

    private static readonly string lLogFileName =
        $"Cadroue-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log";

    public static string LLogFolderRead() => Path.Combine(LDepot.LDepotRootRead(), LLogFolderName);

    public static string LLogPathRead() => Path.Combine(LLogFolderRead(), lLogFileName);

    public static event Action<string>? LLogAppend;

    public static void LInfo(string lMessage)
    {
        LWrite("Info", lMessage, null);
    }

    public static void LError(string lMessage, Exception? lException = null)
    {
        LWrite("Error", lMessage, lException);
    }

    public static string LTextRead()
    {
        lock (lLogLock)
        {
            string lLogPath = LLogPathRead();
            return File.Exists(lLogPath) ? File.ReadAllText(lLogPath, Encoding.UTF8) : string.Empty;
        }
    }

    public static void LClear()
    {
        lock (lLogLock)
        {
            string lLogPath = LLogPathRead();
            Directory.CreateDirectory(LLogFolderRead());
            File.WriteAllText(lLogPath, string.Empty, Encoding.UTF8);
        }

        LLogAppend?.Invoke(string.Empty);
    }

    private static void LWrite(string lLevel, string lMessage, Exception? lException)
    {
        string lEntry = LEntryCreate(lLevel, lMessage, lException);
        lock (lLogLock)
        {
            try
            {
                Directory.CreateDirectory(LLogFolderRead());
                File.AppendAllText(LLogPathRead(), lEntry, Encoding.UTF8);
            }
            catch (Exception)
            {
            }
        }

        LLogAppend?.Invoke(lEntry);
    }

    private static string LEntryCreate(string lLevel, string lMessage, Exception? lException)
    {
        var lBuilder = new StringBuilder();
        lBuilder.Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
        lBuilder.Append(" [");
        lBuilder.Append(lLevel);
        lBuilder.Append("] ");
        lBuilder.AppendLine(lMessage);
        if (lException is not null)
        {
            lBuilder.AppendLine(lException.ToString());
        }

        return lBuilder.ToString();
    }
}
