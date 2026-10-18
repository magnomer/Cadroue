using System.Globalization;
using System.Text;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LEncodeJoin
{
    internal static string LEncodeMergeSave(LWorkItem lWorkItem)
    {
        string lMergeListPath = Path.Combine(LDepotEngine.LDepotMergeSeam(), $"{lWorkItem.LWorkId:N}.txt");
        File.WriteAllText(lMergeListPath, LEncodeJoinFormat(lWorkItem.LWorkMergeSources));
        return lMergeListPath;
    }

    internal static string LEncodeJoinSave(LWorkItem lWorkItem, IReadOnlyList<string> lBridgeParts)
    {
        string lJoinPath = Path.Combine(LDepotEngine.LDepotBridgeSeam(), $"{lWorkItem.LWorkId:N}.concat.txt");
        File.WriteAllText(lJoinPath, LEncodeJoinFormat(lBridgeParts));
        return lJoinPath;
    }

    internal static void LEncodeBridgeClear(Guid lWorkId)
    {
        string lJoinPath = Path.Combine(LDepotEngine.LDepotBridgeSeam(), $"{lWorkId:N}.concat.txt");
        try
        {
            if (File.Exists(lJoinPath))
            {
                File.Delete(lJoinPath);
            }
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string LEncodeJoinFormat(IReadOnlyList<string> lJoinParts)
    {
        var lJoinList = new StringBuilder();
        foreach (string lPart in lJoinParts)
        {
            string lEscaped = lPart
                .Replace("\\", "/", StringComparison.Ordinal)
                .Replace("'", "'\\''", StringComparison.Ordinal);
            lJoinList.Append(CultureInfo.InvariantCulture, $"file '{lEscaped}'\n");
        }

        return lJoinList.ToString();
    }
}
