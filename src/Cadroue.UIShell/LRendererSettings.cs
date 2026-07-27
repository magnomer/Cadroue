using System;
using System.IO;

namespace Cadroue.UIShell;

public sealed class LRendererSettings
{
    public const string LRendererProgramFile = "ffmpeg.exe";
    public const string LRendererProgramPath = "ffmpeg";

    public string? LRendererFfmpegLibraryFolder { get; set; }

    public bool LRendererFfmpegLibraryFolderCustomReady =>
        LRendererFolderValidate(LRendererFfmpegLibraryFolder);

    public static LRendererSettings LRendererDefaultCreate()
    {
        return new LRendererSettings
        {
            LRendererFfmpegLibraryFolder = null
        };
    }

    public LRendererSettings LRendererFolderChange(string? lRendererFfmpegLibraryFolder)
    {
        return new LRendererSettings
        {
            LRendererFfmpegLibraryFolder = string.IsNullOrWhiteSpace(lRendererFfmpegLibraryFolder)
                ? null
                : Path.GetFullPath(lRendererFfmpegLibraryFolder)
        };
    }

    public static bool LRendererFolderValidate(string? lRendererFfmpegLibraryFolder)
    {
        if (string.IsNullOrWhiteSpace(lRendererFfmpegLibraryFolder) || !Directory.Exists(lRendererFfmpegLibraryFolder))
        {
            return false;
        }

        return LRendererFileExist(lRendererFfmpegLibraryFolder, "avcodec*.dll")
            && LRendererFileExist(lRendererFfmpegLibraryFolder, "avformat*.dll")
            && LRendererFileExist(lRendererFfmpegLibraryFolder, "avutil*.dll")
            && LRendererFileExist(lRendererFfmpegLibraryFolder, "swscale*.dll")
            && LRendererFileExist(lRendererFfmpegLibraryFolder, "swresample*.dll");
    }

    public static bool LRendererProgramExist(string? lRendererFolder)
    {
        return !string.IsNullOrWhiteSpace(lRendererFolder)
            && File.Exists(Path.Combine(lRendererFolder, LRendererProgramFile));
    }

    public static string LRendererProgramRead(string? lRendererFolder)
    {
        return LRendererProgramExist(lRendererFolder)
            ? Path.Combine(lRendererFolder!, LRendererProgramFile)
            : LRendererProgramPath;
    }

    public static string? LRendererFolderFind()
    {
        string? lRendererPathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(lRendererPathEnv))
        {
            return null;
        }

        foreach (string lRendererDir in lRendererPathEnv.Split(Path.PathSeparator))
        {
            if (LRendererFolderValidate(lRendererDir))
            {
                return lRendererDir;
            }
        }

        return null;
    }

    private static bool LRendererFileExist(string lRendererFfmpegLibraryFolder, string lRendererPattern)
    {
        return Directory.EnumerateFiles(lRendererFfmpegLibraryFolder, lRendererPattern, SearchOption.TopDirectoryOnly).Any();
    }
}
