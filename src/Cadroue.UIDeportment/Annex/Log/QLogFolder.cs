using Cadroue.Conduct;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The trace folder's files as the log lists them, each with its label, and the one picked; a new listing keeps the
// pick while its file is still listed and is told on only when the files differ.
public sealed class QLogFolder
{
    private readonly CUsher qLogUsher;
    private readonly Action<string, string> qLogErrorHandler;
    private readonly List<QLogFile> qLogFiles = [];
    private int qLogFileIndex;

    internal QLogFolder(CUsher lUsher, Action<string, string> lErrorRaise)
    {
        qLogUsher = lUsher;
        qLogErrorHandler = lErrorRaise;
    }

    public event Action? QLogFilesChanged;

    public IReadOnlyList<QLogFile> QLogFiles => qLogFiles;

    public int QLogFileIndex => qLogFileIndex;

    internal (string, List<string>) QLogFilesRead()
    {
        string lCurrentPath = LTraceFolder.LTracePathRead();
        LTraceReadResult<List<string>> lFilesResult = LTraceReader.LTraceFilesRead();
        List<string> lFiles = lFilesResult.LTraceReadValue;
        if (!lFilesResult.LTraceReadSuccess)
        {
            qLogErrorHandler("Log.Error.List", lFilesResult.LTraceReadError);
        }

        return (lCurrentPath, lFiles);
    }

    internal static void QLogCurrentInsert(List<string> lFiles, string lCurrentPath)
    {
        if (!lFiles.Contains(lCurrentPath, StringComparer.OrdinalIgnoreCase))
        {
            lFiles.Insert(0, lCurrentPath);
        }
    }

    internal string QLogSelectedResolve(List<string> lFiles, string lCurrentPath, bool lFollowCurrent)
    {
        string lSelectedPath = !lFollowCurrent && qLogFiles.Count > 0
            ? qLogFiles[Math.Clamp(qLogFileIndex, 0, qLogFiles.Count - 1)].QLogFilePath
            : lCurrentPath;
        if (!lFiles.Contains(lSelectedPath, StringComparer.OrdinalIgnoreCase))
        {
            lSelectedPath = lCurrentPath;
        }

        return lSelectedPath;
    }

    internal void QLogFilesApply(List<string> lFiles, string lCurrentPath, string lSelectedPath)
    {
        if (qLogFiles.Select(lFile => lFile.QLogFilePath).SequenceEqual(lFiles, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        qLogFiles.Clear();
        qLogFileIndex = 0;
        foreach (string lFile in lFiles)
        {
            bool lCurrent = string.Equals(lFile, lCurrentPath, StringComparison.OrdinalIgnoreCase);
            if (string.Equals(lFile, lSelectedPath, StringComparison.OrdinalIgnoreCase))
            {
                qLogFileIndex = qLogFiles.Count;
            }

            qLogFiles.Add(new QLogFile(lFile, QLogLabelRead(lFile, lCurrent)));
        }

        QLogFilesChanged?.Invoke();
    }

    private string QLogLabelRead(string lFile, bool lCurrent) =>
        lCurrent
            ? QLocalizationCatalog.QLocalizationTextRead("Log.File.Current")
            : qLogUsher.CUsherStemRead(lFile);

    internal bool QLogIndexCheck(int lIndex) => lIndex >= 0 && lIndex < qLogFiles.Count;

    internal void QLogIndexSet(int lIndex) => qLogFileIndex = lIndex;
}
