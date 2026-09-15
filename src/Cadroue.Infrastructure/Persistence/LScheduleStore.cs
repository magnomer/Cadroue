using Cadroue.Core;

namespace Cadroue.Infrastructure;

internal static class LScheduleStore
{
    private static readonly HashSet<string> lScheduleRejectedPaths = new(StringComparer.OrdinalIgnoreCase);

    internal static LWorkRecord? LScheduleRecordParse(string lScheduleRecordJson)
    {
        try
        {
            return LWorkRecord.LWorkRecordParse(lScheduleRecordJson);
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal static LWorkRecord? LScheduleRecordRead(string lDepotFilePath)
    {
        LWorkRecord? lWorkRecord;
        try
        {
            lWorkRecord = LWorkRecord.LWorkRecordParse(File.ReadAllText(lDepotFilePath));
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        if (lWorkRecord is null)
        {
            LScheduleRejectionRecord(lDepotFilePath, "is not a valid work record and is ignored");
            return null;
        }

        if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(lDepotFilePath), "N", out Guid lDepotId)
            || lDepotId != lWorkRecord.LWorkId)
        {
            LScheduleRejectionRecord(lDepotFilePath, "names a different work id than its file and is ignored");
            return null;
        }

        return lWorkRecord;
    }

    private static void LScheduleRejectionRecord(string lDepotFilePath, string lScheduleReason)
    {
        lock (lScheduleRejectedPaths)
        {
            if (!lScheduleRejectedPaths.Add(lDepotFilePath))
            {
                return;
            }
        }

        LTraceLog.LTraceWarningRecord($"Schedule: record '{Path.GetFileName(lDepotFilePath)}' {lScheduleReason}");
    }

    internal static bool LScheduleRecordSave(LWorkRecord lWorkRecord, LDepotFolder lDepotFolder)
    {
        string lDepotFilePath = LDepot.LDepotFileRead(lDepotFolder, lWorkRecord.LWorkId);
        if (!LScheduleFileSave(lDepotFilePath, lWorkRecord.LWorkJsonCreate()))
        {
            LDepotIndex.LDepotDirtySet();
            return false;
        }

        LDepotIndex.LDepotIndexSet(lWorkRecord, lDepotFolder);
        return true;
    }

    internal static bool LScheduleRecordMove(LWorkRecord lWorkRecord, LDepotFolder lDepotFrom, LDepotFolder lDepotTo)
    {
        if (!LScheduleMove(lWorkRecord.LWorkId, lDepotFrom, lDepotTo))
        {
            return false;
        }

        if (LScheduleRecordSave(lWorkRecord, lDepotTo))
        {
            return true;
        }

        LTraceLog.LTraceWarningRecord(
            LScheduleMove(lWorkRecord.LWorkId, lDepotTo, lDepotFrom)
                ? $"Schedule: work '{lWorkRecord.LWorkOutputName}' " +
                  $"[{LSchedule.LScheduleIdShorten(lWorkRecord.LWorkId)}] " +
                  $"could not be written as {lDepotTo} and stays {lDepotFrom}"
                : $"Schedule: work '{lWorkRecord.LWorkOutputName}' " +
                  $"[{LSchedule.LScheduleIdShorten(lWorkRecord.LWorkId)}] " +
                  $"could not be written as {lDepotTo} and could not be returned to {lDepotFrom}");
        return false;
    }

    private static bool LScheduleFileSave(string lDepotFilePath, string lDepotContent)
    {
        string lDepotTempPath = lDepotFilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(lDepotTempPath, lDepotContent);
            File.Move(lDepotTempPath, lDepotFilePath, overwrite: true);
            return true;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
            try
            {
                if (File.Exists(lDepotTempPath))
                {
                    File.Delete(lDepotTempPath);
                }
            }
            catch (Exception lCleanup) when (lCleanup is IOException or UnauthorizedAccessException)
            {
            }

            return false;
        }
    }

    private static bool LScheduleMove(Guid lWorkId, LDepotFolder lDepotFrom, LDepotFolder lDepotTo)
    {
        string lDepotFromPath = LDepot.LDepotFileRead(lDepotFrom, lWorkId);
        string lDepotToPath = LDepot.LDepotFileRead(lDepotTo, lWorkId);

        try
        {
            File.Move(lDepotFromPath, lDepotToPath, overwrite: false);
            return true;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
