using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LPlacementRecord
{
    public double LPlacementLeft { get; set; }

    public double LPlacementTop { get; set; }

    public double LPlacementWidth { get; set; }

    public double LPlacementHeight { get; set; }
}

public static class LPlacement
{
    private const string LPlacementFileName = "placement.json";

    private static readonly object lPlacementLock = new();

    public static LPlacementRecord? LPlacementRead(string lPlacementKey)
    {
        lock (lPlacementLock)
        {
            return LPlacementAllRead().TryGetValue(lPlacementKey, out LPlacementRecord? lPlacement)
                ? lPlacement
                : null;
        }
    }

    public static void LPlacementSave(string lPlacementKey, double lLeft, double lTop, double lWidth, double lHeight)
    {
        if (double.IsNaN(lLeft) || double.IsNaN(lTop) || lWidth <= 0 || lHeight <= 0)
        {
            return;
        }

        lock (lPlacementLock)
        {
            Dictionary<string, LPlacementRecord> lPlacements = LPlacementAllRead();
            lPlacements[lPlacementKey] = new LPlacementRecord
            {
                LPlacementLeft = lLeft,
                LPlacementTop = lTop,
                LPlacementWidth = lWidth,
                LPlacementHeight = lHeight
            };

            try
            {
                File.WriteAllText(
                    LPlacementPathRead(),
                    JsonSerializer.Serialize(lPlacements, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    public static bool LPlacementExist(string lPlacementKey) => LPlacementRead(lPlacementKey) is not null;

    private static string LPlacementPathRead() => Path.Combine(LDepot.LDepotRootRead(), LPlacementFileName);

    private static Dictionary<string, LPlacementRecord> LPlacementAllRead()
    {
        try
        {
            string lPlacementPath = LPlacementPathRead();
            if (!File.Exists(lPlacementPath))
            {
                return new Dictionary<string, LPlacementRecord>(StringComparer.Ordinal);
            }

            return JsonSerializer.Deserialize<Dictionary<string, LPlacementRecord>>(File.ReadAllText(lPlacementPath))
                ?? new Dictionary<string, LPlacementRecord>(StringComparer.Ordinal);
        }
        catch (Exception)
        {
            return new Dictionary<string, LPlacementRecord>(StringComparer.Ordinal);
        }
    }
}
