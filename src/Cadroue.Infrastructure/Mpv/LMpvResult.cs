using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LMpv
{
    private const string LMpvResultFolder = "Cadroue";
    private const string LMpvResultFile = "mpvprobe.json";

    public static LMpvProbe LMpvResultRead() => LMpvResultRead(LMpvStampRead());

    public static LMpvProbe LMpvResultRead(string lStamp)
    {
        LMpvResultRecord? lRecord = LMpvResultLoad();
        if (lRecord is null)
        {
            return LMpvProbe.LMpvProbeUnknown;
        }

        return string.Equals(lRecord.LMpvStamp, lStamp, StringComparison.Ordinal)
            ? lRecord.LMpvOutcome
            : LMpvProbe.LMpvProbeUnknown;
    }

    public static void LMpvResultSave(LMpvProbe lOutcome) => LMpvResultSave(lOutcome, LMpvStampRead());

    public static void LMpvResultSave(LMpvProbe lOutcome, string lStamp)
    {
        var lRecord = new LMpvResultRecord
        {
            LMpvOutcome = lOutcome,
            LMpvStamp = lStamp
        };

        string lPath = LMpvPathResolve();
        string lTempPath = lPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            string? lFolder = Path.GetDirectoryName(lPath);
            if (!string.IsNullOrWhiteSpace(lFolder))
            {
                Directory.CreateDirectory(lFolder);
            }

            string lJson = JsonSerializer.Serialize(lRecord, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(lTempPath, lJson);
            File.Move(lTempPath, lPath, overwrite: true);
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
            try
            {
                if (File.Exists(lTempPath))
                {
                    File.Delete(lTempPath);
                }
            }
            catch (Exception lCleanup) when (lCleanup is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    public static LMpvResultRecord? LMpvResultLoad()
    {
        string lPath = LMpvPathResolve();
        if (!File.Exists(lPath))
        {
            return null;
        }

        try
        {
            string lJson = File.ReadAllText(lPath);
            return JsonSerializer.Deserialize<LMpvResultRecord>(lJson);
        }
        catch
        {
            return null;
        }
    }

    public static string LMpvStampRead() => LMpvStampCreate(LMpvVersionRead(), LMpvLibraryRead());

    public static string LMpvStampCreate(string lAppVersion, string lLibraryPath)
    {
        return lAppVersion + "|" + LMpvLibraryFormat(lLibraryPath);
    }

    private static string LMpvLibraryFormat(string lLibraryPath)
    {
        try
        {
            if (!File.Exists(lLibraryPath))
            {
                return "missing:" + lLibraryPath;
            }

            string? lFileVersion = FileVersionInfo.GetVersionInfo(lLibraryPath).FileVersion;
            if (!string.IsNullOrWhiteSpace(lFileVersion))
            {
                return "ver:" + lFileVersion;
            }

            var lInfo = new FileInfo(lLibraryPath);
            return "stat:" + lInfo.LastWriteTimeUtc.Ticks + ":" + lInfo.Length;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
            return "unreadable:" + lLibraryPath;
        }
    }

    private static string LMpvVersionRead()
    {
        Assembly lAssembly = Assembly.GetEntryAssembly() ?? typeof(LMpv).Assembly;
        string? lInformational = lAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(lInformational)
            ? lAssembly.GetName().Version?.ToString() ?? "unknown"
            : lInformational;
    }

    private static string LMpvPathResolve()
    {
        string lAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lAppData, LMpvResultFolder, LMpvResultFile);
    }
}

public sealed class LMpvResultRecord
{
    public LMpvProbe LMpvOutcome { get; set; } = LMpvProbe.LMpvProbeUnknown;

    public string LMpvStamp { get; set; } = "";
}
