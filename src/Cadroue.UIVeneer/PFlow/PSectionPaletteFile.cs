using Cadroue.Infrastructure;
using System.Windows.Media;

namespace Cadroue.UIVeneer.PFlow;

internal static partial class PSectionPalette
{
    private static void PSectionHiddenLoad(string pPaletteFolder)
    {
        pSectionHidden.Clear();
        foreach (string pName in LSectionPalette.LSectionHiddenLoad(pPaletteFolder))
        {
            if (!PSectionFixedCheck(pName))
            {
                pSectionHidden.Add(pName);
            }
        }
    }

    private static PSectionSwatch? PSectionFileRead(string pFilePath) =>
        LSectionPalette.LSectionPaletteRead(pFilePath) is { } pFile ? PSectionSwatchResolve(pFile) : null;

    private static PSectionSwatch? PSectionSwatchResolve(LSectionPaletteFile pFile)
    {
        var pColors = new List<Color>();
        foreach (string pHex in pFile.LSectionPaletteColors)
        {
            if (PSectionHexParse(pHex) is { } pColor)
            {
                pColors.Add(pColor);
            }
        }

        return pColors.Count == 0
            ? null
            : new PSectionSwatch(pFile.LSectionPaletteName, pColors.ToArray(), pFile.LSectionPalettePath);
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
}
