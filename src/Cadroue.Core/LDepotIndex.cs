using Microsoft.Data.Sqlite;

namespace Cadroue.Core;

public sealed record LDepotIndexRow(
    Guid LDepotRowWorkId,
    LDepotFolder LDepotRowFolder,
    LWorkState LDepotRowState,
    LWorkPriority LDepotRowPriority,
    DateTimeOffset LDepotRowCreateTime,
    int LDepotRowOwnerProcessId,
    string LDepotRowOutputName);

public static class LDepotIndex
{
    private const int LDepotBusyTimeoutSeconds = 5;

    public static void LDepotIndexEnsure()
    {
        LDepot.LDepotEnsure();
        using SqliteConnection lDepotConnection = LDepotConnectionOpen();
        using SqliteCommand lDepotCommand = lDepotConnection.CreateCommand();
        lDepotCommand.CommandText = """
            CREATE TABLE IF NOT EXISTS work (
                work_id     TEXT PRIMARY KEY,
                folder      TEXT NOT NULL,
                state       TEXT NOT NULL,
                priority    TEXT NOT NULL,
                create_time TEXT NOT NULL,
                owner_pid   INTEGER NOT NULL DEFAULT 0,
                output_name TEXT NOT NULL DEFAULT ''
            );
            CREATE INDEX IF NOT EXISTS work_folder ON work (folder);
            """;
        lDepotCommand.ExecuteNonQuery();
    }

    public static void LDepotIndexSet(LWorkRecord lWorkRecord, LDepotFolder lDepotFolder)
    {
        using SqliteConnection lDepotConnection = LDepotConnectionOpen();
        using SqliteCommand lDepotCommand = lDepotConnection.CreateCommand();
        lDepotCommand.CommandText = """
            INSERT INTO work (work_id, folder, state, priority, create_time, owner_pid, output_name)
            VALUES ($id, $folder, $state, $priority, $create, $owner, $name)
            ON CONFLICT(work_id) DO UPDATE SET
                folder = $folder, state = $state, priority = $priority,
                owner_pid = $owner, output_name = $name;
            """;
        lDepotCommand.Parameters.AddWithValue("$id", lWorkRecord.WorkId.ToString("N"));
        lDepotCommand.Parameters.AddWithValue("$folder", lDepotFolder.ToString());
        lDepotCommand.Parameters.AddWithValue("$state", lWorkRecord.State);
        lDepotCommand.Parameters.AddWithValue("$priority", lWorkRecord.Priority);
        lDepotCommand.Parameters.AddWithValue("$create", lWorkRecord.CreateTime.ToString("O"));
        lDepotCommand.Parameters.AddWithValue("$owner", lWorkRecord.OwnerProcessId);
        lDepotCommand.Parameters.AddWithValue("$name", lWorkRecord.OutputName);
        lDepotCommand.ExecuteNonQuery();
    }

    public static void LDepotIndexRemove(Guid lWorkId)
    {
        using SqliteConnection lDepotConnection = LDepotConnectionOpen();
        using SqliteCommand lDepotCommand = lDepotConnection.CreateCommand();
        lDepotCommand.CommandText = "DELETE FROM work WHERE work_id = $id;";
        lDepotCommand.Parameters.AddWithValue("$id", lWorkId.ToString("N"));
        lDepotCommand.ExecuteNonQuery();
    }

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
        LDepotIndexEnsure();
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
                        (work_id, folder, state, priority, create_time, owner_pid, output_name)
                    VALUES ($id, $folder, $state, $priority, $create, $owner, $name);
                    """;
                lDepotInsert.Parameters.AddWithValue("$id", lWorkRecord.WorkId.ToString("N"));
                lDepotInsert.Parameters.AddWithValue("$folder", lDepotFolder.ToString());
                lDepotInsert.Parameters.AddWithValue("$state", lWorkRecord.State);
                lDepotInsert.Parameters.AddWithValue("$priority", lWorkRecord.Priority);
                lDepotInsert.Parameters.AddWithValue("$create", lWorkRecord.CreateTime.ToString("O"));
                lDepotInsert.Parameters.AddWithValue("$owner", lWorkRecord.OwnerProcessId);
                lDepotInsert.Parameters.AddWithValue("$name", lWorkRecord.OutputName);
                lDepotInsert.ExecuteNonQuery();
            }
        }

        lDepotTransaction.Commit();
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
            DataSource = LDepot.LDepotIndexPathRead(),
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private
        };

        var lDepotConnection = new SqliteConnection(lDepotBuilder.ToString());
        lDepotConnection.Open();

        using (SqliteCommand lDepotPragma = lDepotConnection.CreateCommand())
        {
            lDepotPragma.CommandText = $"PRAGMA busy_timeout = {LDepotBusyTimeoutSeconds * 1000}; PRAGMA journal_mode = WAL;";
            lDepotPragma.ExecuteNonQuery();
        }

        return lDepotConnection;
    }

    private static TEnum LDepotEnumRead<TEnum>(string lDepotValue, TEnum lDepotFallback) where TEnum : struct =>
        Enum.TryParse(lDepotValue, out TEnum lDepotParsed) ? lDepotParsed : lDepotFallback;
}
