using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Cadroue.Conduct;

internal sealed record CSEncoderTier(string CSEncoderTierLabel, int CSEncoderTierWidth, int CSEncoderTierHeight);

public sealed record CSEncoderDimension(
    int CSEncoderDimensionTier,
    int CSEncoderDimensionWidth,
    int CSEncoderDimensionHeight)
{
    public const int CSEncoderDimensionMost = 7680;

    public bool CSEncoderDimensionNative => CSEncoderDimensionTier == 0;

    public bool CSEncoderDimensionCustom => CSEncoderDimensionTier < 0;

    public int? CSEncoderDimensionNotch => CSEncoderDimensionTier >= 0 ? CSEncoderDimensionTier : null;

    public (int, int) CSEncoderDimensionKnobs =>
        (Math.Min(CSEncoderDimensionWidth, CSEncoderDimensionMost),
            Math.Min(CSEncoderDimensionHeight, CSEncoderDimensionMost));

    public (string, string) CSEncoderDimensionShown =>
        (CSEncoderDimensionFormat(CSEncoderDimensionWidth), CSEncoderDimensionFormat(CSEncoderDimensionHeight));

    public string CSEncoderDimensionResolve(string source, string custom) =>
        CSEncoderDimensionCustom
            ? custom
            : CSEncoderDimensionNative
                ? source
                : CSEncoderSize.CSEncoderTierLabels[CSEncoderDimensionTier];

    public string? CSEncoderDimensionSync(string typed, double knob)
    {
        string text = CSEncoderDimensionFormat((int)Math.Round(knob));
        return string.Equals(typed.Trim(), text, StringComparison.Ordinal)
            || (CSEncoderDimensionNative && text.Length == 0)
                ? null
                : text;
    }

    public static (string, string) CSEncoderDimensionDescribe(bool? reactive)
    {
        bool turned = reactive == true;
        return (
            turned ? "Encoder.Video.Field.AxisX" : "Encoder.Video.Field.Width",
            turned ? "Encoder.Video.Field.AxisY" : "Encoder.Video.Field.Height");
    }

    internal string CSEncoderDimensionFormat()
    {
        (int tier, int width, int height) = this;
        return tier != 0 && width > 0 && height > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{width} × {height}")
            : CSEncoderDraft.CSEncoderDraftSource;
    }

    private static string CSEncoderDimensionFormat(int value) =>
        value > 0 ? value.ToString(CultureInfo.InvariantCulture) : string.Empty;
}

public sealed class CSEncoderSize
{
    private static readonly CSEncoderTier[] _csEncoderTierValues =
    [
        new("Source", 0, 0),
        new("480p", 854, 480),
        new("720p", 1280, 720),
        new("1080p", 1920, 1080),
        new("1440p", 2560, 1440),
        new("4K", 3840, 2160),
        new("8K", 7680, 4320)
    ];

    private CSEncoderDimension _csEncoderSizeValue;

    internal CSEncoderSize(string size)
    {
        _csEncoderSizeValue = CSEncoderSizeParse(size) is (int width, int height)
            ? new CSEncoderDimension(CSEncoderTierResolve(width, height), width, height)
            : new CSEncoderDimension(0, 0, 0);
    }

    public event Action<CSEncoderDimension>? CSEncoderSizeChanged;

    public static IReadOnlyList<string> CSEncoderTierLabels =>
        _csEncoderTierValues.Select(tier => tier.CSEncoderTierLabel).ToArray();

    public static int CSEncoderTierMost => _csEncoderTierValues.Length - 1;

    public CSEncoderDimension CSEncoderSizeValue => _csEncoderSizeValue;

    public bool CSEncoderSizeSelect(double knob)
    {
        int tier = CSEncoderSizeClamp((int)Math.Round(knob));
        if (tier == _csEncoderSizeValue.CSEncoderDimensionTier)
        {
            return false;
        }

        CSEncoderTier picked = _csEncoderTierValues[tier];
        var value = new CSEncoderDimension(tier, picked.CSEncoderTierWidth, picked.CSEncoderTierHeight);
        _csEncoderSizeValue = value;
        CSEncoderSizeChanged?.Invoke(value);
        return true;
    }

    public bool CSEncoderSizeSet(string width, string height)
    {
        CSEncoderDimension value = CSEncoderSizeNormalize(CSEncoderSizeRead(width), CSEncoderSizeRead(height));
        if (value == _csEncoderSizeValue)
        {
            return false;
        }

        _csEncoderSizeValue = value;
        CSEncoderSizeChanged?.Invoke(value);
        return true;
    }

    internal string CSEncoderSizeFormat() => _csEncoderSizeValue.CSEncoderDimensionFormat();

    private static (int, int)? CSEncoderSizeParse(string size)
    {
        string[] parts = size.Split(
            ['x', 'X', '×'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2
            && int.TryParse(parts[0], out int width) && width > 0
            && int.TryParse(parts[1], out int height) && height > 0)
        {
            return (width, height);
        }

        return null;
    }

    private static int CSEncoderSizeRead(string typed) =>
        int.TryParse(typed.Trim(), out int value) && value > 0 ? value : 0;

    private static int CSEncoderTierResolve(int width, int height)
    {
        for (int at = 1; at < _csEncoderTierValues.Length; at++)
        {
            (_, int tierWidth, int tierHeight) = _csEncoderTierValues[at];
            if ((tierWidth == width && tierHeight == height) || (tierWidth == height && tierHeight == width))
            {
                return at;
            }
        }

        return -1;
    }

    private static int CSEncoderSizeClamp(int tier) => Math.Clamp(tier, 0, CSEncoderTierMost);

    private static CSEncoderDimension CSEncoderSizeNormalize(int width, int height)
    {
        width = Math.Max(0, width);
        height = Math.Max(0, height);
        int tier = width == 0 && height == 0
            ? 0
            : width > 0 && height > 0 ? CSEncoderTierResolve(width, height) : -1;
        return new CSEncoderDimension(tier, width, height);
    }
}
