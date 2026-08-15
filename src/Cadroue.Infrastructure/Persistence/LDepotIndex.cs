using Microsoft.Data.Sqlite;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed record LDepotIndexRow(
    Guid LLedgerWorkId,
    LDepotFolder LLedgerFolder,
    LWorkState LLedgerState,
    LWorkPriority LLedgerPriority,
    DateTimeOffset LLedgerCreateTime,
    int LLedgerProcessId,
    string LLedgerOutputName);

public static class LDepotIndex
{
    private const int LDepotBusyTimeout = 5;

    private static bool lDepotSchemaChecked;

    public static bool LDepotIndexDirty { get; private set; } = true;

    public static void LDepotIndexDirtySet() => LDepotIndexDirty = true;

    public static void LDepotIndexCreate()
    {
        try
        {
            LDepot.LDepotCreate();
            using SqliteConnection lDepotConnection = LDepotConnectionOpen();
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
                    // the column already exists on a current-schema table
                }

                lDepotSchemaChecked = true;
            }
        }
        catch (Exception lDepotException) when (lDepotException is SqliteException or IOException)
        {
            LDepotIndexDirtySet();
            LDepotIndexRecord("could not be opened", lDepotException);
        }
    }

    public static void LDepotIndexSet(LWorkRecord lWorkRecord, LDepotFolder lDepotFolder)
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
            lDepotCommand.Parameters.AddWithValue("$id", lWorkRecord.LWorkId.ToString("N"));
            lDepotCommand.Parameters.AddWithValue("$folder", lDepotFolder.ToString());
            lDepotCommand.Parameters.AddWithValue("$state", lWorkRecord.LWorkStateName);
            lDepotCommand.Parameters.AddWithValue("$priority", lWorkRecord.LWorkPriorityName);
            lDepotCommand.Parameters.AddWithValue("$create", lWorkRecord.LWorkCreateTime.ToString("O"));
            lDepotCommand.Parameters.AddWithValue("$owner", lWorkRecord.LWorkOwnerProcess);
            lDepotCommand.Parameters.AddWithValue("$name", lWorkRecord.LWorkOutputName);
            lDepotCommand.Parameters.AddWithValue("$record", lWorkRecord.LWorkJsonCreate());
            lDepotCommand.ExecuteNonQuery();
        }
        catch (Exception lDepotException) when (lDepotException is SqliteException or IOException)
        {
            LDepotIndexDirtySet();
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
            LDepotIndexDirtySet();
            LDepotIndexRecord("removal failed", lDepotException);
        }
    }

    public static IReadOnlyList<(LDepotFolder LLedgerFolder, string LDepotRowRecord)> LDepotRecordsRead()
    {
        var lDepotRecords = new List<(LDepotFolder, string)>();
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
                    LDepotIndexDirtySet();
                    continue;
                }

                lDepotRecords.Add((LDepotEnumRead(lDepotReader.GetString(0), LDepotFolder.LDepotFolderScheduled), lDepotRecord));
            }
        }
        catch (Exception lDepotException) when (lDepotException is SqliteException or IOException)
        {
            LDepotIndexDirtySet();
            LDepotIndexRecord("could not be read", lDepotException);
        }

        return lDepotRecords;
    }

    private static void LDepotIndexRecord(string lDepotDetail, Exception lDepotException)
        => LTraceLog.LTraceWarningRecord(
            $"Queue index {lDepotDetail} (the index rebuilds from the work folders): {lDepotException.Message}");

    public static IReadOnlyList<LDepotIndexRow> LDepotIndexRead()
    {
        var lDepotRows = new List<LDepotIndexRow>();
        using SqliteConnection lDepotConnection = LDepotConnectionOpen();
        using SqliteCommand lDepotCommand = lDepotConnection.CreateCommand();
        lDepotCommand.CommandText = """
            SELECT work_id, folder, state, priority, create_time, owner_pid, output_name
            FROM work ORDER BY create_time;
            """;

        using SqliteDataReader lDepotReader = lDepotCommand.ExecuteReader();
        while (lDepotReader.Read())
        {
            lDepotRows.Add(new LDepotIndexRow(
                Guid.TryParse(lDepotReader.GetString(0), out Guid lWorkId) ? lWorkId : Guid.Empty,
                LDepotEnumRead(lDepotReader.GetString(1), LDepotFolder.LDepotFolderScheduled),
                LDepotEnumRead(lDepotReader.GetString(2), LWorkState.LWorkStatePending),
                LDepotEnumRead(lDepotReader.GetString(3), LWorkPriority.LWorkPriorityNormal),
                DateTimeOffset.TryParse(lDepotReader.GetString(4), out DateTimeOffset lCreate)
                    ? lCreate
                    : DateTimeOffset.MinValue,
                lDepotReader.GetInt32(5),
                lDepotReader.GetString(6)));
        }

        return lDepotRows;
    }

    public static void LDepotIndexRebuild()
    {
        LDepotIndexCreate();
        try
        {
            using SqliteConnection lDepotConnection = LDepotConnectionOpen();
            using SqliteTransaction lDepotTransaction = lDepotConnection.BeginTransaction();

            using (SqliteCommand lDepotClear = lDepotConnection.CreateCommand())
            {
                lDepotClear.Transaction = lDepotTransaction;
                lDepotClear.CommandText = "DELETE FROM work;";
                lDepotClear.ExecuteNonQuery();
            }

            foreach (LDepotFolder lDepotFolder in Enum.GetValues<LDepotFolder>())
            {
                foreach (string lDepotFilePath in LDepot.LDepotFilesRead(lDepotFolder))
                {
                    LWorkRecord? lWorkRecord = LDepotRecordRead(lDepotFilePath);
                    if (lWorkRecord is null)
                    {
                        continue;
                    }

                    using SqliteCommand lDepotInsert = lDepotConnection.CreateCommand();
                    lDepotInsert.Transaction = lDepotTransaction;
                    lDepotInsert.CommandText = """
                        INSERT OR REPLACE INTO work
                            (work_id, folder, state, priority, create_time, owner_pid, output_name, record)
                        VALUES ($id, $folder, $state, $priority, $create, $owner, $name, $record);
                        """;
                    lDepotInsert.Parameters.AddWithValue("$id", lWorkRecord.LWorkId.ToString("N"));
                    lDepotInsert.Parameters.AddWithValue("$folder", lDepotFolder.ToString());
                    lDepotInsert.Parameters.AddWithValue("$state", lWorkRecord.LWorkStateName);
                    lDepotInsert.Parameters.AddWithValue("$priority", lWorkRecord.LWorkPriorityName);
                    lDepotInsert.Parameters.AddWithValue("$create", lWorkRecord.LWorkCreateTime.ToString("O"));
                    lDepotInsert.Parameters.AddWithValue("$owner", lWorkRecord.LWorkOwnerProcess);
                    lDepotInsert.Parameters.AddWithValue("$name", lWorkRecord.LWorkOutputName);
                    lDepotInsert.Parameters.AddWithValue("$record", lWorkRecord.LWorkJsonCreate());
                    lDepotInsert.ExecuteNonQuery();
                }
            }

            lDepotTransaction.Commit();
            LDepotIndexDirty = false;
        }
        catch (Exception lDepotException) when (lDepotException is SqliteException or IOException)
        {
            LDepotIndexDirtySet();
            LDepotIndexRecord("rebuild failed", lDepotException);
        }
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

    public static void LDepotIndexCompact()
    {
        try
        {
            using SqliteConnection lDepotConnection = LDepotConnectionOpen();
            using SqliteCommand lDepotCommand = lDepotConnection.CreateCommand();
            lDepotCommand.CommandText = "PRAGMA wal_checkpoint(TRUNCATE); VACUUM;";
            lDepotCommand.ExecuteNonQuery();
        }
        catch (Exception lDepotException) when (lDepotException is SqliteException or IOException)
        {
            LDepotIndexRecord("compaction failed", lDepotException);
        }
    }

    private static LWorkRecord? LDepotRecordRead(string lDepotFilePath)
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

    private static SqliteConnection LDepotConnectionOpen()
    {
        var lDepotBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = LDepot.LDepotIndexFind(),
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private
        };

        var lDepotConnection = new SqliteConnection(lDepotBuilder.ToString());
        lDepotConnection.Open();

        using (SqliteCommand lDepotPragma = lDepotConnection.CreateCommand())
        {
            lDepotPragma.CommandText = $"PRAGMA busy_timeout = {LDepotBusyTimeout * 1000}; PRAGMA journal_mode = WAL;";
            lDepotPragma.ExecuteNonQuery();
        }

        return lDepotConnection;
    }

    private static TEnum LDepotEnumRead<TEnum>(string lDepotValue, TEnum lDepotFallback) where TEnum : struct =>
        Enum.TryParse(lDepotValue, out TEnum lDepotParsed) ? lDepotParsed : lDepotFallback;
}
