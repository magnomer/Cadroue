using Microsoft.Data.Sqlite;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed record LDepotEntry(LDepotFolder LDepotEntryFolder, string LDepotEntryJson);

public static class LDepotIndex
{
    private const int LDepotBusyTimeout = 5;

    private static bool lDepotSchemaChecked;

    public static bool LDepotDirty { get; private set; } = true;

    public static void LDepotDirtySet() => LDepotDirty = true;

    public static void LDepotIndexCreate(string? lDepotRoot = null)
    {
        lDepotRoot ??= LDepot.LDepotRootRead();
        try
        {
            LDepot.LDepotCreate(lDepotRoot);
            using SqliteConnection lDepotConnection = LDepotConnectionOpen(lDepotRoot);
            using (SqliteCommand lDepotCommand = lDepotConnection.CreateCommand())
            {
                lDepotCommand.CommandText = """
                    CREATE TABLE IF NOT EXISTS work (
                        work_id     TEXT PRIMARY KEY,
                        folder      TEXT NOT NULL,
                        state       TEXT NOT NULL,
                        priority    TEXT NOT NULL,
                        create_time TEXT NOT NULL,
                        owner_pid   INTEGER NOT NULL DEFAULT 0,
                        output_name TEXT NOT NULL DEFAULT '',
                        record      TEXT NOT NULL DEFAULT ''
                    );
                    CREATE INDEX IF NOT EXISTS work_folder ON work (folder);
                    """;
                lDepotCommand.ExecuteNonQuery();
            }

            if (!lDepotSchemaChecked)
            {
                try
                {
                    using SqliteCommand lDepotAlter = lDepotConnection.CreateCommand();
                    lDepotAlter.CommandText = "ALTER TABLE work ADD COLUMN record TEXT NOT NULL DEFAULT '';";
                    lDepotAlter.ExecuteNonQuery();
                }
                catch (SqliteException)
                {
                }

                lDepotSchemaChecked = true;
            }
        }
        catch (Exception lDepotException) when (lDepotException is SqliteException or IOException)
        {
            LDepotDirtySet();
            LDepotIndexRecord("could not be opened", lDepotException);
        }
    }

    public static void LDepotIndexSet(LWorkDocument lWorkRecord, LDepotFolder lDepotFolder)
    {
        try
        {
            using SqliteConnection lDepotConnection = LDepotConnectionOpen();
            using SqliteCommand lDepotCommand = lDepotConnection.CreateCommand();
            lDepotCommand.CommandText = """
                INSERT INTO work (work_id, folder, state, priority, create_time, owner_pid, output_name, record)
                VALUES ($id, $folder, $state, $priority, $create, $owner, $name, $record)
                ON CONFLICT(work_id) DO UPDATE SET
                    folder = $folder, state = $state, priority = $priority,
                    owner_pid = $owner, output_name = $name, record = $record;
                """;
            LDepotRowApply(lDepotCommand, lWorkRecord, lDepotFolder);
            lDepotCommand.ExecuteNonQuery();
        }
        catch (Exception lDepotException) when (lDepotException is SqliteException or IOException)
        {
            LDepotDirtySet();
            LDepotIndexRecord($"update failed for '{lWorkRecord.LWorkOutputName}'", lDepotException);
        }
    }

    public static void LDepotIndexRemove(Guid lWorkId)
    {
        try
        {
            using SqliteConnection lDepotConnection = LDepotConnectionOpen();
            using SqliteCommand lDepotCommand = lDepotConnection.CreateCommand();
            lDepotCommand.CommandText = "DELETE FROM work WHERE work_id = $id;";
            lDepotCommand.Parameters.AddWithValue("$id", lWorkId.ToString("N"));
            lDepotCommand.ExecuteNonQuery();
        }
        catch (Exception lDepotException) when (lDepotException is SqliteException or IOException)
        {
            LDepotDirtySet();
            LDepotIndexRecord("removal failed", lDepotException);
        }
    }

    public static IReadOnlyList<LDepotEntry> LDepotRecordsRead()
    {
        var lDepotRecords = new List<LDepotEntry>();
        try
        {
            using SqliteConnection lDepotConnection = LDepotConnectionOpen();
            using SqliteCommand lDepotCommand = lDepotConnection.CreateCommand();
            lDepotCommand.CommandText = "SELECT folder, record FROM work ORDER BY create_time;";

            using SqliteDataReader lDepotReader = lDepotCommand.ExecuteReader();
            while (lDepotReader.Read())
            {
                string lDepotRecord = lDepotReader.GetString(1);
                if (string.IsNullOrEmpty(lDepotRecord))
                {
                    LDepotDirtySet();
                    continue;
                }

                lDepotRecords.Add(new LDepotEntry(
                    LDepotEnumRead(lDepotReader.GetString(0), LDepotFolder.LDepotFolderScheduled),
                    lDepotRecord));
            }
        }
        catch (Exception lDepotException) when (lDepotException is SqliteException or IOException)
        {
            LDepotDirtySet();
            LDepotIndexRecord("could not be read", lDepotException);
        }

        return lDepotRecords;
    }

    private static void LDepotIndexRecord(string lDepotDetail, Exception lDepotException)
        => LTraceLog.LTraceWarningRecord(
            $"Queue index {lDepotDetail} (the index rebuilds from the work folders): {lDepotException.Message}");

    public static IReadOnlyList<LScheduleEntry> LDepotIndexRebuild(string? lDepotRoot = null)
    {
        lDepotRoot ??= LDepot.LDepotRootRead();
        LDepotIndexCreate(lDepotRoot);
        var lDepotEntries = new List<LScheduleEntry>();
        try
        {
            using SqliteConnection lDepotConnection = LDepotConnectionOpen(lDepotRoot);
            using SqliteTransaction lDepotTransaction = lDepotConnection.BeginTransaction();

            using (SqliteCommand lDepotClear = lDepotConnection.CreateCommand())
            {
                lDepotClear.Transaction = lDepotTransaction;
                lDepotClear.CommandText = "DELETE FROM work;";
                lDepotClear.ExecuteNonQuery();
            }

            foreach (LDepotFolder lDepotFolder in Enum.GetValues<LDepotFolder>())
            {
                foreach (string lDepotFilePath in LDepot.LDepotFilesRead(lDepotFolder, lDepotRoot))
                {
                    LWorkDocument? lWorkRecord = LScheduleStore.LScheduleRecordRead(lDepotFilePath);
                    if (lWorkRecord is null)
                    {
                        continue;
                    }

                    lDepotEntries.Add(new LScheduleEntry(lDepotFolder, lWorkRecord));
                    using SqliteCommand lDepotInsert = lDepotConnection.CreateCommand();
                    lDepotInsert.Transaction = lDepotTransaction;
                    lDepotInsert.CommandText = """
                        INSERT OR REPLACE INTO work
                            (work_id, folder, state, priority, create_time, owner_pid, output_name, record)
                        VALUES ($id, $folder, $state, $priority, $create, $owner, $name, $record);
                        """;
                    LDepotRowApply(lDepotInsert, lWorkRecord, lDepotFolder);
                    lDepotInsert.ExecuteNonQuery();
                }
            }

            lDepotTransaction.Commit();
            LDepotDirty = false;
        }
        catch (Exception lDepotException) when (lDepotException is SqliteException or IOException)
        {
            LDepotDirtySet();
            LDepotIndexRecord("rebuild failed", lDepotException);
        }

        return lDepotEntries;
    }

    public static void LDepotIndexRelease()
    {
        try
        {
            using (SqliteConnection lDepotConnection = LDepotConnectionOpen())
            using (SqliteCommand lDepotCommand = lDepotConnection.CreateCommand())
            {
                lDepotCommand.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                lDepotCommand.ExecuteNonQuery();
            }
        }
        catch (Exception lDepotException) when (lDepotException is SqliteException or IOException)
        {
            LDepotIndexRecord("could not be flushed before the move", lDepotException);
        }

        SqliteConnection.ClearAllPools();
    }

    public static void LDepotIndexCompact(string? lDepotRoot = null)
    {
        try
        {
            using SqliteConnection lDepotConnection = LDepotConnectionOpen(lDepotRoot);
            using SqliteCommand lDepotCommand = lDepotConnection.CreateCommand();
            lDepotCommand.CommandText = "PRAGMA wal_checkpoint(TRUNCATE); VACUUM;";
            lDepotCommand.ExecuteNonQuery();
        }
        catch (Exception lDepotException) when (lDepotException is SqliteException or IOException)
        {
            LDepotIndexRecord("compaction failed", lDepotException);
        }
    }

    private static void LDepotRowApply(
        SqliteCommand lDepotCommand, LWorkDocument lWorkRecord, LDepotFolder lDepotFolder)
    {
        lDepotCommand.Parameters.AddWithValue("$id", lWorkRecord.LWorkId.ToString("N"));
        lDepotCommand.Parameters.AddWithValue("$folder", lDepotFolder.ToString());
        lDepotCommand.Parameters.AddWithValue("$state", lWorkRecord.LWorkStateName);
        lDepotCommand.Parameters.AddWithValue("$priority", lWorkRecord.LWorkPriorityName);
        lDepotCommand.Parameters.AddWithValue("$create", lWorkRecord.LWorkCreateTime.ToString("O"));
        lDepotCommand.Parameters.AddWithValue("$owner", lWorkRecord.LWorkOwnerProcess);
        lDepotCommand.Parameters.AddWithValue("$name", lWorkRecord.LWorkOutputName);
        lDepotCommand.Parameters.AddWithValue("$record", lWorkRecord.LWorkJsonCreate());
    }

    private static SqliteConnection LDepotConnectionOpen(string? lDepotRoot = null)
    {
        var lDepotBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = LDepot.LDepotIndexFind(lDepotRoot),
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private
        };

        var lDepotConnection = new SqliteConnection(lDepotBuilder.ToString());
        lDepotConnection.Open();

        using (SqliteCommand lDepotPragma = lDepotConnection.CreateCommand())
        {
            lDepotPragma.CommandText = $"PRAGMA busy_timeout = {LDepotBusyTimeout * 1000}; "
                + "PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL;";
            lDepotPragma.ExecuteNonQuery();
        }

        return lDepotConnection;
    }

    private static LDepotEnum LDepotEnumRead<LDepotEnum>(string lDepotValue, LDepotEnum lDepotFallback)
        where LDepotEnum : struct =>
        Enum.TryParse(lDepotValue, out LDepotEnum lDepotParsed) ? lDepotParsed : lDepotFallback;
}
