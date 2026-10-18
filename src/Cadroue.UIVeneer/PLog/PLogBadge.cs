using System.Windows.Media;

namespace Cadroue.UIVeneer;

internal sealed record PLogBadge(Brush PLogBadgeFill, Brush PLogBadgeInk)
{
    private static readonly PLogBadge PLogBadgeInfo =
        new(PLogBrushCreate(0xE7, 0xEE, 0xF9), PLogBrushCreate(0x2B, 0x34, 0x43));

    private static readonly IReadOnlyDictionary<string, PLogBadge> PLogBadges = new Dictionary<string, PLogBadge>
    {
        ["Log.Category.Loading"] = new(PLogBrushCreate(0xEC, 0xF3, 0xE9), PLogBrushCreate(0x3A, 0x5A, 0x2E)),
        ["Log.Category.Warning"] = new(PLogBrushCreate(0xFD, 0xF3, 0xDA), PLogBrushCreate(0x8A, 0x60, 0x0A)),
        ["Log.Category.Error"] = new(PLogBrushCreate(0xFB, 0xE3, 0xE3), PLogBrushCreate(0x8C, 0x1D, 0x1D)),
        ["Log.Category.Interaction"] = new(PLogBrushCreate(0xE4, 0xF4, 0xEE), PLogBrushCreate(0x1D, 0x66, 0x4A)),
        ["Log.Category.Interface"] = new(PLogBrushCreate(0xED, 0xE8, 0xF7), PLogBrushCreate(0x46, 0x30, 0x8A)),
        ["Log.Category.Work"] = new(PLogBrushCreate(0xE3, 0xF0, 0xFB), PLogBrushCreate(0x14, 0x52, 0x7E)),
        ["Log.Category.Ffmpeg"] = new(PLogBrushCreate(0xFD, 0xF0, 0xE1), PLogBrushCreate(0x8A, 0x4B, 0x0A)),
        ["Log.Category.Info"] = PLogBadgeInfo,
    };

    internal static PLogBadge PLogBadgeRead(string pLogKey) => PLogBadges.GetValueOrDefault(pLogKey, PLogBadgeInfo);

    private static Brush PLogBrushCreate(byte pLogRed, byte pLogGreen, byte pLogBlue)
    {
        var pLogBrush = new SolidColorBrush(Color.FromRgb(pLogRed, pLogGreen, pLogBlue));
        pLogBrush.Freeze();
        return pLogBrush;
    }
}
