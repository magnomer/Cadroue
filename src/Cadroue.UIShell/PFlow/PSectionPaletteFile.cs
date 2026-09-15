using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Cadroue.UIShell.PFlow;

internal static partial class PSectionPalette
{
    private const string PSectionHiddenFile = ".hidden.json";

    private static string PSectionHiddenRead() =>
        Path.Combine(Cadroue.Infrastructure.LDepot.LDepotPaletteRead(), PSectionHiddenFile);

    private static void PSectionHiddenLoad()
    {
        pSectionHidden.Clear();
        try
        {
            string pHiddenPath = PSectionHiddenRead();
            if (!File.Exists(pHiddenPath))
            {
                return;
            }

            foreach (string pName in JsonSerializer.Deserialize<string[]>(File.ReadAllText(pHiddenPath))
                ?? Array.Empty<string>())
            {
                if (!PSectionFixedCheck(pName))
                {
                    pSectionHidden.Add(pName);
                }
            }
        }
        catch (Exception)
        {
            pSectionHidden.Clear();
        }
    }

    private static void PSectionHiddenSave()
    {
        try
        {
            File.WriteAllText(
                PSectionHiddenRead(),
                JsonSerializer.Serialize(pSectionHidden.ToArray(), new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static IEnumerable<string> PSectionFilesRead()
    {
        try
        {
            return Directory.EnumerateFiles(Cadroue.Infrastructure.LDepot.LDepotPaletteRead(), "*.json")
                .Where(pPath => !string.Equals(
                    Path.GetFileName(pPath),
                    PSectionHiddenFile,
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(pPath => pPath);
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }

    private static PSectionSwatch? PSectionFileRead(string pFilePath)
    {
        try
        {
            PSectionPaletteRecord? pRecord = JsonSerializer.Deserialize<PSectionPaletteRecord>(
                File.ReadAllText(pFilePath));
            if (pRecord is null
                || string.IsNullOrWhiteSpace(pRecord.PSectionPaletteName)
                || pRecord.PSectionPaletteColors.Length == 0)
            {
                return null;
            }

            var pColors = new List<Color>();
            foreach (string pHex in pRecord.PSectionPaletteColors)
            {
                if (PSectionHexParse(pHex) is { } pColor)
                {
                    pColors.Add(pColor);
                }
            }

            return pColors.Count == 0
                ? null
                : new PSectionSwatch(pRecord.PSectionPaletteName.Trim(), pColors.ToArray(), pFilePath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string PSectionFileCreate(string pName)
    {
        string pSafe = new(pName
            .Select(pCharacter => Path.GetInvalidFileNameChars().Contains(pCharacter) ? '_' : pCharacter)
            .ToArray());
        return $"{pSafe}.json";
    }

    private static string PSectionHexFormat(Color pColor) => $"#{pColor.R:X2}{pColor.G:X2}{pColor.B:X2}";

    private static Color? PSectionHexParse(string pHex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(pHex.Trim());
        }
        catch (Exception)
        {
            return null;
        }
    }

    private sealed class PSectionPaletteRecord
    {
        [JsonPropertyName("Name")]
        public string PSectionPaletteName { get; set; } = string.Empty;

        [JsonPropertyName("Colors")]
        public string[] PSectionPaletteColors { get; set; } = Array.Empty<string>();
    }
}
