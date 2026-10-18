using System.Globalization;
using Cadroue.Application;

namespace Cadroue.UIDeportment;

public readonly record struct QSEncoderTier(string QSEncoderTierLabel, int QSEncoderTierWidth, int QSEncoderTierHeight);

// The encoder window's output size: a named tier or a custom width and height, kept as one consistent triple.
public sealed class QSEncoderSize
{
    public const int QSEncoderDimensionMost = 7680;

    private static readonly QSEncoderTier[] qsEncoderTiers =
    [
        new("Source", 0, 0),
        new("480p", 854, 480),
        new("720p", 1280, 720),
        new("1080p", 1920, 1080),
        new("1440p", 2560, 1440),
        new("4K", 3840, 2160),
        new("8K", 7680, 4320)
    ];

    private int qsEncoderSizeTier;
    private int qsEncoderWidth;
    private int qsEncoderHeight;

    internal QSEncoderSize(LPreset lDraft)
    {
        if (QSEncoderSizeParse(lDraft.LPresetVideo.LPresetSize) is (int lWidth, int lHeight))
        {
            qsEncoderWidth = lWidth;
            qsEncoderHeight = lHeight;
            qsEncoderSizeTier = QSEncoderTierResolve(lWidth, lHeight);
        }
    }

    public event Action? QSEncoderSizeChanged;

    public static IReadOnlyList<QSEncoderTier> QSEncoderTiers => qsEncoderTiers;

    public int QSEncoderSizeTier => qsEncoderSizeTier;

    public int QSEncoderWidth => qsEncoderWidth;

    public int QSEncoderHeight => qsEncoderHeight;

    private static (int, int)? QSEncoderSizeParse(string lSize)
    {
        string[] lParts = lSize.Split(
            ['x', 'X', '×'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lParts.Length == 2
            && int.TryParse(lParts[0], out int lWidth) && lWidth > 0
            && int.TryParse(lParts[1], out int lHeight) && lHeight > 0)
        {
            return (lWidth, lHeight);
        }

        return null;
    }

    public static int QSEncoderTierResolve(int lWidth, int lHeight)
    {
        for (int lAt = 1; lAt < qsEncoderTiers.Length; lAt++)
        {
            (_, int lTierWidth, int lTierHeight) = qsEncoderTiers[lAt];
            if ((lTierWidth == lWidth && lTierHeight == lHeight) || (lTierWidth == lHeight && lTierHeight == lWidth))
            {
                return lAt;
            }
        }

        return -1;
    }

    public bool QSEncoderTierSelect(int lTier)
    {
        lTier = QSEncoderTierClamp(lTier);
        if (lTier == qsEncoderSizeTier)
        {
            return false;
        }

        qsEncoderSizeTier = lTier;
        qsEncoderWidth = qsEncoderTiers[lTier].QSEncoderTierWidth;
        qsEncoderHeight = qsEncoderTiers[lTier].QSEncoderTierHeight;
        QSEncoderSizeChanged?.Invoke();
        return true;
    }

    private static int QSEncoderTierClamp(int lTier) => Math.Clamp(lTier, 0, qsEncoderTiers.Length - 1);

    private static (int, int, int) QSEncoderSizeNormalize(int lWidth, int lHeight)
    {
        lWidth = Math.Max(0, lWidth);
        lHeight = Math.Max(0, lHeight);
        int lTier = lWidth == 0 && lHeight == 0
            ? 0
            : lWidth > 0 && lHeight > 0 ? QSEncoderTierResolve(lWidth, lHeight) : -1;
        return (lWidth, lHeight, lTier);
    }

    public bool QSEncoderSizeSet(int lWidth, int lHeight)
    {
        (lWidth, lHeight, int lTier) = QSEncoderSizeNormalize(lWidth, lHeight);
        if (lWidth == qsEncoderWidth && lHeight == qsEncoderHeight && lTier == qsEncoderSizeTier)
        {
            return false;
        }

        qsEncoderWidth = lWidth;
        qsEncoderHeight = lHeight;
        qsEncoderSizeTier = lTier;
        QSEncoderSizeChanged?.Invoke();
        return true;
    }

    public string QSEncoderSizeFormat() =>
        qsEncoderSizeTier != 0 && qsEncoderWidth > 0 && qsEncoderHeight > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{qsEncoderWidth} × {qsEncoderHeight}")
            : QSEncoder.QSEncoderSourceMode;
}
