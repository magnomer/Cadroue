using System.Text.Json;

namespace Cadroue.Infrastructure;

public enum LVaultOutcome
{
    LVaultMissing = 0,
    LVaultLoaded,
    LVaultUnreadable
}

public sealed record LVaultResult<T>(LVaultOutcome LVaultOutcome, T? LVaultValue)
    where T : class;

public static class LVault
{
    private static readonly JsonSerializerOptions lVaultOptions = new() { WriteIndented = true };

    public static LVaultResult<T> LVaultRead<T>(string lVaultPath)
        where T : class
    {
        if (!File.Exists(lVaultPath))
        {
            return new LVaultResult<T>(LVaultOutcome.LVaultMissing, null);
        }

        try
        {
            T? lVaultValue = JsonSerializer.Deserialize<T>(File.ReadAllText(lVaultPath));
            if (lVaultValue is null)
            {
                LVaultQuarantineRun();
                return new LVaultResult<T>(LVaultOutcome.LVaultUnreadable, null);
            }

            return new LVaultResult<T>(LVaultOutcome.LVaultLoaded, lVaultValue);
        }
        catch
        {
            LVaultQuarantineRun();
            return new LVaultResult<T>(LVaultOutcome.LVaultUnreadable, null);
        }

        void LVaultQuarantineRun()
        {
            try
            {
                string lVaultCorrupt = lVaultPath + ".corrupt";
                if (File.Exists(lVaultCorrupt))
                {
                    File.Delete(lVaultCorrupt);
                }

                File.Move(lVaultPath, lVaultCorrupt);
            }
            catch
            {
            }
        }
    }

    public static bool LVaultSave<T>(string lVaultPath, T lVaultValue)
        where T : class
    {
        string lVaultTemporary = lVaultPath + ".tmp";
        try
        {
            string? lVaultFolder = Path.GetDirectoryName(lVaultPath);
            if (!string.IsNullOrWhiteSpace(lVaultFolder))
            {
                Directory.CreateDirectory(lVaultFolder);
            }

            File.WriteAllText(lVaultTemporary, JsonSerializer.Serialize(lVaultValue, lVaultOptions));
            if (File.Exists(lVaultPath))
            {
                File.Replace(lVaultTemporary, lVaultPath, null);
            }
            else
            {
                File.Move(lVaultTemporary, lVaultPath);
            }

            return true;
        }
        catch
        {
            try
            {
                if (File.Exists(lVaultTemporary))
                {
                    File.Delete(lVaultTemporary);
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
