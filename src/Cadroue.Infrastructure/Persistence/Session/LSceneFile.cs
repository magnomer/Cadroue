using System;
using System.IO;
using System.Linq;
using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LSceneFile
{
    public static string LSceneFileResolve(string lSceneName, string lSceneDefault)
    {
        char[] lInvalid = Path.GetInvalidFileNameChars();
        string lClean = new string(lSceneName.Trim()
            .Select(lCharacter => lInvalid.Contains(lCharacter) ? '_' : lCharacter)
            .ToArray());
        return string.IsNullOrWhiteSpace(lClean) ? $"{lSceneDefault}.json" : $"{lClean}.json";
    }

    public static void LSceneFileSave(LSceneDocument lScene, string lScenePath)
    {
        string lSceneJson = JsonSerializer.Serialize(lScene, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(lScenePath, lSceneJson);
    }

    public static string LSceneStemResolve(string lSceneName, string lScenePath)
    {
        string lSceneTrimmed = lSceneName.Trim();
        return lSceneTrimmed.Length > 0 ? lSceneTrimmed : Path.GetFileNameWithoutExtension(lScenePath).Trim();
    }

    public static LSceneDocument? LSceneFileLoad(string lScenePath)
    {
        string lSceneJson = File.ReadAllText(lScenePath);
        return LSceneCanonical.LSceneNormalize(JsonSerializer.Deserialize<LSceneDocument>(lSceneJson));
    }
}
