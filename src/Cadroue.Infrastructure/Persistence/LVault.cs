using System.Text.Json;

namespace Cadroue.Infrastructure;

public enum LVaultOutcome
{
    LVaultMissing = 0,
    LVaultLoaded,
    LVaultUnreadable
}

public sealed record LVaultResult<LVaultPayload>(LVaultOutcome LVaultOutcome, LVaultPayload? LVaultValue)
    where LVaultPayload : class;

public static class LVault
{
    private static readonly JsonSerializerOptions lVaultOptions = new() { WriteIndented = true };

    public static LVaultResult<LVaultPayload> LVaultRead<LVaultPayload>(string lVaultPath)
        where LVaultPayload : class
    {
        if (!File.Exists(lVaultPath))
        {
            return new LVaultResult<LVaultPayload>(LVaultOutcome.LVaultMissing, null);
        }

        try
        {
            LVaultPayload? lVaultValue = JsonSerializer.Deserialize<LVaultPayload>(File.ReadAllText(lVaultPath));
            if (lVaultValue is null)
            {
                LVaultQuarantineRun();
                return new LVaultResult<LVaultPayload>(LVaultOutcome.LVaultUnreadable, null);
            }

            return new LVaultResult<LVaultPayload>(LVaultOutcome.LVaultLoaded, lVaultValue);
        }
        catch
        {
            LVaultQuarantineRun();
            return new LVaultResult<LVaultPayload>(LVaultOutcome.LVaultUnreadable, null);
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

    public static bool LVaultSave<LVaultPayload>(string lVaultPath, LVaultPayload lVaultValue)
        where LVaultPayload : class
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
