namespace Cadroue.Core;

public sealed partial class LSchedule
{
    private static LWorkRecord? LScheduleRecordParse(string lScheduleRecordJson)
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

    private static LWorkRecord? LScheduleRecordRead(string lDepotFilePath)
    {
        try
        {
            return LWorkRecord.LWorkRecordParse(File.ReadAllText(lDepotFilePath));
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static bool LScheduleRecordSave(LWorkRecord lWorkRecord, LDepotFolder lDepotFolder)
    {
        string lDepotFilePath = LDepot.LDepotFileRead(lDepotFolder, lWorkRecord.LWorkId);
        if (!LScheduleFileSave(lDepotFilePath, lWorkRecord.LWorkJsonCreate()))
        {
            LDepotIndex.LDepotIndexInvalidate();
            return false;
        }

        LDepotIndex.LDepotIndexSet(lWorkRecord, lDepotFolder);
        return true;
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
