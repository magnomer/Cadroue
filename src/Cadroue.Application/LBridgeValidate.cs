namespace Cadroue.Application;

public enum LBridgeReason
{
    LBridgeReasonCompatible,
    LBridgeReasonCodec,
    LBridgeReasonProfile,
    LBridgeReasonLevel,
    LBridgeReasonWidth,
    LBridgeReasonHeight,
    LBridgeReasonPixel,
    LBridgeReasonFramerate,
    LBridgeReasonTimebase,
    LBridgeReasonSar,
    LBridgeReasonDar,
    LBridgeReasonField,
    LBridgeReasonPrimaries,
    LBridgeReasonTransfer,
    LBridgeReasonMatrix,
    LBridgeReasonRange,
    LBridgeReasonExtradata,
    LBridgeReasonContainer
}

public sealed record LBridgeStream(
    string LBridgeCodec,
    string LBridgeProfile,
    int LBridgeLevel,
    int LBridgeWidth,
    int LBridgeHeight,
    string LBridgePixel,
    string LBridgeFramerate,
    string LBridgeTimebase,
    string LBridgeSampleAspect,
    string LBridgeDisplayAspect,
    string LBridgeFieldOrder,
    string LBridgeColorPrimaries,
    string LBridgeColorTransfer,
    string LBridgeColorMatrix,
    string LBridgeColorRange,
    byte[] LBridgeExtradata,
    string LBridgeContainer);

public sealed record LBridgeCompatibility(bool LBridgeCompatible, LBridgeReason LBridgeReason);

public static partial class LBridge
{
    public static LBridgeCompatibility LBridgeValidate(LBridgeStream lBridgeGenerated, LBridgeStream lBridgeSource)
    {
        if (!LBridgeTextMatch(lBridgeGenerated.LBridgeCodec, lBridgeSource.LBridgeCodec))
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonCodec);
        }

        if (!LBridgeTextMatch(lBridgeGenerated.LBridgeProfile, lBridgeSource.LBridgeProfile))
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonProfile);
        }

        if (lBridgeGenerated.LBridgeLevel != lBridgeSource.LBridgeLevel)
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonLevel);
        }

        if (lBridgeGenerated.LBridgeWidth != lBridgeSource.LBridgeWidth)
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonWidth);
        }

        if (lBridgeGenerated.LBridgeHeight != lBridgeSource.LBridgeHeight)
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonHeight);
        }

        if (!LBridgeTextMatch(lBridgeGenerated.LBridgePixel, lBridgeSource.LBridgePixel))
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonPixel);
        }

        if (!LBridgeTextMatch(lBridgeGenerated.LBridgeFramerate, lBridgeSource.LBridgeFramerate))
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonFramerate);
        }

        if (!LBridgeTextMatch(lBridgeGenerated.LBridgeTimebase, lBridgeSource.LBridgeTimebase))
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonTimebase);
        }

        if (!LBridgeTextMatch(lBridgeGenerated.LBridgeSampleAspect, lBridgeSource.LBridgeSampleAspect))
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonSar);
        }

        if (!LBridgeTextMatch(lBridgeGenerated.LBridgeDisplayAspect, lBridgeSource.LBridgeDisplayAspect))
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonDar);
        }

        if (!LBridgeTextMatch(lBridgeGenerated.LBridgeFieldOrder, lBridgeSource.LBridgeFieldOrder))
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonField);
        }

        if (!LBridgeTextMatch(lBridgeGenerated.LBridgeColorPrimaries, lBridgeSource.LBridgeColorPrimaries))
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonPrimaries);
        }

        if (!LBridgeTextMatch(lBridgeGenerated.LBridgeColorTransfer, lBridgeSource.LBridgeColorTransfer))
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonTransfer);
        }

        if (!LBridgeTextMatch(lBridgeGenerated.LBridgeColorMatrix, lBridgeSource.LBridgeColorMatrix))
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonMatrix);
        }

        if (!LBridgeTextMatch(lBridgeGenerated.LBridgeColorRange, lBridgeSource.LBridgeColorRange))
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonRange);
        }

        if (!LBridgeExtradataMatch(lBridgeGenerated.LBridgeExtradata, lBridgeSource.LBridgeExtradata))
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonExtradata);
        }

        if (!LBridgeTextMatch(lBridgeGenerated.LBridgeContainer, lBridgeSource.LBridgeContainer))
        {
            return LBridgeIncompatibleCreate(LBridgeReason.LBridgeReasonContainer);
        }

        return new LBridgeCompatibility(true, LBridgeReason.LBridgeReasonCompatible);
    }

    private static LBridgeCompatibility LBridgeIncompatibleCreate(LBridgeReason lBridgeReason) =>
        new(false, lBridgeReason);

    private static bool LBridgeTextMatch(string lBridgeLeft, string lBridgeRight) =>
        string.Equals(lBridgeLeft ?? string.Empty, lBridgeRight ?? string.Empty, StringComparison.Ordinal);

    private static bool LBridgeExtradataMatch(byte[] lBridgeLeft, byte[] lBridgeRight) =>
        (lBridgeLeft ?? Array.Empty<byte>()).AsSpan().SequenceEqual(lBridgeRight ?? Array.Empty<byte>());
}
