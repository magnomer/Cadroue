using System;
using System.IO;
using System.Linq;

namespace Cadroue.Infrastructure;

public static class LRendererLibrary
{
    public const string LRendererProgramFile = "ffmpeg.exe";
    public const string LRendererProgramPath = "ffmpeg";

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
