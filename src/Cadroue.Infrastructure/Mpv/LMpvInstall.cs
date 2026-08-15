using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LMpvInstallResult
{
    public bool LMpvInstallSuccess { get; init; }

    public string LMpvInstallMessage { get; init; } = string.Empty;
}

internal sealed class LMpvInstallRecord
{
    public string LMpvInstallFolder { get; set; } = string.Empty;
}

public sealed partial class LMpv
{
    private const string LMpvRootFolder = "local-mpv";
    private const string LMpvInstallName = "runtime";
    private const string LMpvRecordName = "local-mpv.json";

    private const string LMpvReleaseUrl =
        "https://api.github.com/repos/zhongfly/mpv-winbuild/releases/latest";
    private const string LMpvAssetPrefix = "mpv-dev-lgpl-x86_64-";
    private const string LMpvAssetVariant = "-v3-";
    private const string LMpvAssetSuffix = ".7z";

    public static string LMpvRootRead() => Path.Combine(LDepot.LDepotRootRead(), LMpvRootFolder);

    public static bool LMpvInstalledCheck() => LMpvFolderRead() is not null;

    public static string? LMpvFolderRead()
    {
        try
        {
            string lRecordPath = LMpvRecordFind();
            if (!File.Exists(lRecordPath))
            {
                return null;
            }

            LMpvInstallRecord? lRecord = JsonSerializer.Deserialize<LMpvInstallRecord>(File.ReadAllText(lRecordPath));
            string? lFolder = lRecord?.LMpvInstallFolder;
            if (!string.IsNullOrWhiteSpace(lFolder) && File.Exists(Path.Combine(lFolder, LMpvLibraryFile)))
            {
                return lFolder;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public static async Task<LMpvInstallResult> LMpvInstallStart()
    {
        string lRoot = LMpvRootRead();
        string lInstallFolder = Path.Combine(lRoot, LMpvInstallName);
        string lArchivePath = string.Empty;

        try
        {
            Directory.CreateDirectory(lInstallFolder);
            string lUrl = await LMpvUrlResolve();
            lArchivePath = Path.Combine(lRoot, LMpvNameResolve(lUrl));
            LTraceLog.LTraceInfoRecord($"Local mpv download: {lUrl}");
            await LMpvArchiveSave(lUrl, lArchivePath);
            await LMpvArchiveRead(lArchivePath, lInstallFolder);

            string lDll = Path.Combine(lInstallFolder, LMpvLibraryFile);
            if (!File.Exists(lDll))
            {
                throw new FileNotFoundException($"{LMpvLibraryFile} was not found in the mpv archive.");
            }

            LMpvRecordSave(new LMpvInstallRecord { LMpvInstallFolder = lInstallFolder });
            LMpvArchiveDelete(lArchivePath);
            LTraceLog.LTraceInfoRecord($"Local mpv installed: {lDll}");

            return new LMpvInstallResult
            {
                LMpvInstallSuccess = true,
                LMpvInstallMessage = string.Empty
            };
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Local mpv install failed", lException);
            LMpvArchiveDelete(lArchivePath);
            return new LMpvInstallResult
            {
                LMpvInstallSuccess = false,
                LMpvInstallMessage = lException.Message
            };
        }
    }

    private static async Task<string> LMpvUrlResolve()
    {
        using HttpClient lClient = LMpvClientCreate();
        string lJson = await lClient.GetStringAsync(LMpvReleaseUrl);

        using JsonDocument lDocument = JsonDocument.Parse(lJson);
        if (lDocument.RootElement.TryGetProperty("assets", out JsonElement lAssets))
        {
            foreach (JsonElement lAsset in lAssets.EnumerateArray())
            {
                string? lName = lAsset.TryGetProperty("name", out JsonElement lNameElement)
                    ? lNameElement.GetString()
                    : null;
                if (lName is null
                    || !lName.StartsWith(LMpvAssetPrefix, StringComparison.OrdinalIgnoreCase)
                    || lName.Contains(LMpvAssetVariant, StringComparison.OrdinalIgnoreCase)
                    || !lName.EndsWith(LMpvAssetSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string? lDownloadUrl = lAsset.TryGetProperty("browser_download_url", out JsonElement lUrlElement)
                    ? lUrlElement.GetString()
                    : null;
                if (!string.IsNullOrWhiteSpace(lDownloadUrl))
                {
                    return lDownloadUrl;
                }
            }
        }

        throw new InvalidOperationException("No LGPL x86_64 libmpv asset was found in the latest mpv-winbuild release.");
    }

    private static string LMpvNameResolve(string lUrl)
    {
        string lName = Path.GetFileName(new Uri(lUrl).AbsolutePath);
        return string.IsNullOrWhiteSpace(lName) ? "mpv-dev.archive" : lName;
    }

    private static async Task LMpvArchiveSave(string lUrl, string lArchivePath)
    {
        using HttpClient lClient = LMpvClientCreate();
        using HttpResponseMessage lResponse =
            await lClient.GetAsync(lUrl, HttpCompletionOption.ResponseHeadersRead);
        lResponse.EnsureSuccessStatusCode();

        using FileStream lFile = File.Create(lArchivePath);
        await lResponse.Content.CopyToAsync(lFile);
    }

    private static HttpClient LMpvClientCreate()
    {
        var lClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        lClient.DefaultRequestHeaders.UserAgent.ParseAdd("Cadroue");
        return lClient;
    }

    private static async Task LMpvArchiveRead(string lArchivePath, string lInstallFolder)
    {
        if (lArchivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            LMpvZipRead(lArchivePath, lInstallFolder);
            return;
        }

        await LMpvExtractRun("tar", $"-xf \"{lArchivePath}\" -C \"{lInstallFolder}\" {LMpvLibraryFile}", lInstallFolder);
    }

    private static void LMpvZipRead(string lArchivePath, string lInstallFolder)
    {
        using ZipArchive lArchive = ZipFile.OpenRead(lArchivePath);
        ZipArchiveEntry? lEntry = lArchive.Entries
            .FirstOrDefault(lItem => string.Equals(lItem.Name, LMpvLibraryFile, StringComparison.OrdinalIgnoreCase));
        if (lEntry is null)
        {
            throw new FileNotFoundException($"{LMpvLibraryFile} was not found in the mpv archive.");
        }

        lEntry.ExtractToFile(Path.Combine(lInstallFolder, LMpvLibraryFile), true);
    }

    private static async Task LMpvExtractRun(string lFileName, string lArguments, string lWorkingDirectory)
    {
        LTraceLog.LTraceInfoRecord($"Local mpv command: {lFileName} {lArguments}");
        using var lProcess = new Process
        {
            StartInfo = new ProcessStartInfo(lFileName, lArguments)
            {
                WorkingDirectory = lWorkingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        lProcess.Start();
        string lOutput = await lProcess.StandardOutput.ReadToEndAsync();
        string lError = await lProcess.StandardError.ReadToEndAsync();
        await lProcess.WaitForExitAsync();
        if (lProcess.ExitCode != 0)
        {
            throw new InvalidOperationException($"{lFileName} failed with exit code {lProcess.ExitCode}.\n{lOutput}\n{lError}");
        }
    }

    private static void LMpvArchiveDelete(string lArchivePath)
    {
        try
        {
            if (File.Exists(lArchivePath))
            {
                File.Delete(lArchivePath);
            }
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string LMpvRecordFind() => Path.Combine(LMpvRootRead(), LMpvRecordName);

    private static void LMpvRecordSave(LMpvInstallRecord lRecord)
    {
        Directory.CreateDirectory(LMpvRootRead());
        File.WriteAllText(
            LMpvRecordFind(),
            JsonSerializer.Serialize(lRecord, new JsonSerializerOptions { WriteIndented = true }));
    }
}
