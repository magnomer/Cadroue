using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LFrameStore
{
    private const string LFrameFolderName = "Cadroue";
    private const string LFrameFileName = "frame.json";

    public static LFrameState LFrameStateCurrent { get; private set; } = LFrameState.LFrameDefaultCreate();

    public static LFrameState LFrameLoad()
    {
        LFrameStateCurrent = LFrameRead();
        return LFrameStateCurrent;
    }

    private static LFrameState LFrameRead()
    {
        string lFramePath = LFramePathCreate();
        if (!File.Exists(lFramePath))
        {
            return LFrameState.LFrameDefaultCreate();
        }

        try
        {
            LFrameState lFrameState = JsonSerializer.Deserialize<LFrameState>(File.ReadAllText(lFramePath))
                ?? LFrameState.LFrameDefaultCreate();
            lFrameState.LFrameNormalize();
            return lFrameState;
        }
        catch
        {
            return LFrameState.LFrameDefaultCreate();
        }
    }

    public static void LFrameSave(LFrameState lFrameState)
    {
        LFrameStateCurrent = lFrameState;
        try
        {
            string lFramePath = LFramePathCreate();
            string? lFrameFolder = Path.GetDirectoryName(lFramePath);
            if (!string.IsNullOrWhiteSpace(lFrameFolder))
            {
                Directory.CreateDirectory(lFrameFolder);
            }

            lFrameState.LFrameNormalize();
            File.WriteAllText(
                lFramePath,
                JsonSerializer.Serialize(lFrameState, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
        }
    }

    private static string LFramePathCreate()
    {
        string lFrameApplicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lFrameApplicationDataFolder, LFrameFolderName, LFrameFileName);
    }
}
