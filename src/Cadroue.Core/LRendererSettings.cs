using System.IO;

namespace Cadroue.Core;

public sealed class LRendererSettings
{
    public string? LRendererLibraryFolder { get; set; }

    public static LRendererSettings LRendererDefaultCreate()
    {
        return new LRendererSettings
        {
            LRendererLibraryFolder = null
        };
    }

    public LRendererSettings LRendererFolderChange(string? lRendererFfmpegLibraryFolder)
    {
        return new LRendererSettings
        {
            LRendererLibraryFolder = string.IsNullOrWhiteSpace(lRendererFfmpegLibraryFolder)
                ? null
                : Path.GetFullPath(lRendererFfmpegLibraryFolder)
        };
    }
}
