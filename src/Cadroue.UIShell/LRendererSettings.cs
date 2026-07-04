using System;
using System.IO;

namespace Cadroue.UIShell;

public sealed class LRendererSettings
{
    public string? LRendererFfmpegLibraryFolder { get; set; }

    public bool LRendererFfmpegLibraryFolderCustomReady =>
        LRendererFfmpegLibraryFolderValidate(LRendererFfmpegLibraryFolder);

    public static LRendererSettings LRendererSettingsDefaultCreate()
    {
        return new LRendererSettings
        {
            LRendererFfmpegLibraryFolder = null
        };
    }

    public LRendererSettings LRendererFfmpegLibraryFolderChange(string? lRendererFfmpegLibraryFolder)
    {
        return new LRendererSettings
        {
            LRendererFfmpegLibraryFolder = string.IsNullOrWhiteSpace(lRendererFfmpegLibraryFolder)
                ? null
                : Path.GetFullPath(lRendererFfmpegLibraryFolder)
        };
    }

    public static bool LRendererFfmpegLibraryFolderValidate(string? lRendererFfmpegLibraryFolder)
    {
        if (string.IsNullOrWhiteSpace(lRendererFfmpegLibraryFolder) || !Directory.Exists(lRendererFfmpegLibraryFolder))
        {
            return false;
        }

        return LRendererFfmpegLibraryFileExists(lRendererFfmpegLibraryFolder, "avcodec*.dll")
            && LRendererFfmpegLibraryFileExists(lRendererFfmpegLibraryFolder, "avformat*.dll")
            && LRendererFfmpegLibraryFileExists(lRendererFfmpegLibraryFolder, "avutil*.dll")
            && LRendererFfmpegLibraryFileExists(lRendererFfmpegLibraryFolder, "swscale*.dll")
            && LRendererFfmpegLibraryFileExists(lRendererFfmpegLibraryFolder, "swresample*.dll");
    }

    public static string? LRendererFfmpegLibraryFolderLocalFind()
    {
        string? lRendererPathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(lRendererPathEnv))
        {
            return null;
        }

        foreach (string lRendererDir in lRendererPathEnv.Split(Path.PathSeparator))
        {
            if (LRendererFfmpegLibraryFolderValidate(lRendererDir))
            {
                return lRendererDir;
            }
        }

        return null;
    }

    private static bool LRendererFfmpegLibraryFileExists(string lRendererFfmpegLibraryFolder, string lRendererPattern)
    {
        return Directory.EnumerateFiles(lRendererFfmpegLibraryFolder, lRendererPattern, SearchOption.TopDirectoryOnly).Any();
    }
}
