using System.IO;
using System.Text;

namespace Cadroue.UIShell;

public static class LAppLog
{
    private static readonly object lLogLock = new();
    private static readonly string lLogFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Cadroue");

    public static readonly string LLogPath = Path.Combine(lLogFolder, "Cadroue.log");

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
            return File.Exists(LLogPath) ? File.ReadAllText(LLogPath, Encoding.UTF8) : string.Empty;
        }
    }

    public static void LClear()
    {
        lock (lLogLock)
        {
            Directory.CreateDirectory(lLogFolder);
            File.WriteAllText(LLogPath, string.Empty, Encoding.UTF8);
        }

        LLogAppend?.Invoke(string.Empty);
    }

    private static void LWrite(string lLevel, string lMessage, Exception? lException)
    {
        string lEntry = LEntryCreate(lLevel, lMessage, lException);
        lock (lLogLock)
        {
            Directory.CreateDirectory(lLogFolder);
            File.AppendAllText(LLogPath, lEntry, Encoding.UTF8);
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
