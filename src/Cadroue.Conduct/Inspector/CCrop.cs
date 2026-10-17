using System;
using System.Globalization;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The crop tool decisions over one inspector's crop record: its switches and edges, its ratio and preset, the size it
// leaves, which rotation choice and flips it shows, and the edit each input asks for. Sides run 0 to 3: left, top,
// right, bottom. Rotation choices run 0 to 3: 0, 90, 180 and 270 degrees. Preset choices run 0 to 6, 0 being a custom
// ratio. The record arrives once through CCropAccept, from the owner that holds it; its changes are told on.
public sealed class CCrop
{
    private static readonly int[] _cCropRotations = { 0, 90, 180, 270 };

    private static readonly (int, int)[] _cCropPresets =
    {
        (0, 0), (16, 9), (9, 16), (4, 3), (3, 4), (1, 1), (21, 9)
    };

    private LCropPort? _cCropPort;

    internal CCrop()
    {
    }

    public event Action? CCropChanged;

    public bool CCropActive => CCropPortRead().LCropStateRead().LCropActive;

    public bool CCropPersistent => CCropPortRead().LCropStateRead().LCropPersistent;

    public bool CCropRatioFixed => CCropPortRead().LCropStateRead().LCropFixed;

    public bool CCropRatioLenient => CCropPortRead().LCropStateRead().LCropLenient;

    // A free ratio, or a fixed one off the presets, shows the custom choice.
    public int CCropPresetIndex
    {
        get
        {
            var lState = CCropPortRead().LCropStateRead();
            return lState.LCropFixed ? CCropPresetResolve(lState.LCropWidth, lState.LCropHeight) : 0;
        }
    }

    public bool CCropCustomShown => CCropPresetIndex == 0;

    public bool CCropRectShown => CCropPortRead().LCropRectCheck();

    // The extent the held edges leave of the source.
    public double CCropWidth
    {
        get
        {
            LCropPort lPort = CCropPortRead();
            var lEdges = lPort.LCropEdgesRead();
            return lPort.LCropSourceRead().LCropWidth - lEdges.LCropLeft - lEdges.LCropRight;
        }
    }

    public double CCropHeight
    {
        get
        {
            LCropPort lPort = CCropPortRead();
            var lEdges = lPort.LCropEdgesRead();
            return lPort.LCropSourceRead().LCropHeight - lEdges.LCropTop - lEdges.LCropBottom;
        }
    }

    // The whole size the applied crop leaves; none without a source or without a size.
    public (double CCropWidth, double CCropHeight)? CCropResolution
    {
        get
        {
            LCropPort lPort = CCropPortRead();
            (bool lPresent, double lSourceWidth, double lSourceHeight) = lPort.LCropSourceRead();
            if (!lPresent)
            {
                return null;
            }

            var lEdges = lPort.LCropNormalRead();
            double lWidth = lSourceWidth - lEdges.LCropLeft - lEdges.LCropRight;
            double lHeight = lSourceHeight - lEdges.LCropTop - lEdges.LCropBottom;
            return lWidth > 0 && lHeight > 0 ? (Math.Round(lWidth), Math.Round(lHeight)) : null;
        }
    }

    // A rotation outside the choices shows the first one.
    public int CCropRotateIndex =>
        Math.Max(0, Array.IndexOf(_cCropRotations, CCropPortRead().LCropOrientationRead().LCropRotation));

    public bool CCropFlipHorizontal => CCropPortRead().LCropOrientationRead().LCropHorizontal;

    public bool CCropFlipVertical => CCropPortRead().LCropOrientationRead().LCropVertical;

    public void CCropAccept(LCropPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cCropPort = port;
        port.LCropChanged += CCropRaise;
    }

    // A side past bottom reads as bottom.
    public int CCropEdgeRead(int side)
    {
        var lEdges = CCropPortRead().LCropEdgesRead();
        return side switch
        {
            0 => lEdges.LCropLeft,
            1 => lEdges.LCropTop,
            2 => lEdges.LCropRight,
            _ => lEdges.LCropBottom
        };
    }

    // The ratio part a field shows: the held one, or with no ratio held the reduced ratio of the drawn crop.
    public int CCropRatioRead(bool width)
    {
        LCropPort lPort = CCropPortRead();
        var lState = lPort.LCropStateRead();
        int lShown = width ? lState.LCropWidth : lState.LCropHeight;
        if (lPort.LCropSourceRead().LCropPresent && !lState.LCropFixed
            && lState.LCropWidth == 0 && lState.LCropHeight == 0
            && lPort.LCropRectCheck())
        {
            (int lWidth, int lHeight) =
                lPort.LCropRatioNormalize((int)Math.Round(CCropWidth), (int)Math.Round(CCropHeight));
            lShown = width ? lWidth : lHeight;
        }

        return lShown;
    }

    // Only a typed edge commits; text locks its side, an empty field frees it, and a negative number reads as 0.
    public void CCropEdgeCommit(int side, string text, bool typed)
    {
        if (!typed)
        {
            return;
        }

        CCropPortRead().LCropEdgeSet(side, Math.Max(0, CCropNumberParse(text)), !string.IsNullOrWhiteSpace(text));
    }

    public void CCropEdgeReset() => CCropPortRead().LCropEdgeReset();

    // Turning an applied crop off drops its ratio as well.
    public void CCropApplySet(bool apply)
    {
        LCropPort lPort = CCropPortRead();
        lPort.LCropApplySet(apply, lPort.LCropStateRead().LCropActive && !apply);
    }

    public void CCropPersistentSet(bool persistent) => CCropPortRead().LCropPersistentSet(persistent);

    // A persistent crop survives the reset; the answer tells whether the crop was reset.
    public bool CCropReset()
    {
        LCropPort lPort = CCropPortRead();
        if (lPort.LCropStateRead().LCropPersistent)
        {
            return false;
        }

        lPort.LCropReset();
        return true;
    }

    // Only typed text commits; the ratio keeps its switches.
    public void CCropRatioCommit(string widthText, string heightText, bool typed)
    {
        if (!typed)
        {
            return;
        }

        LCropPort lPort = CCropPortRead();
        var lState = lPort.LCropStateRead();
        lPort.LCropRatioSet(
            lState.LCropFixed,
            lState.LCropLenient,
            (int)Math.Round(CCropNumberParse(widthText)),
            (int)Math.Round(CCropNumberParse(heightText)),
            false);
    }

    public void CCropFixedSet(bool ratioFixed) => CCropRatioChange(ratioFixed, null);

    public void CCropLenientSet(bool lenient) => CCropRatioChange(null, lenient);

    // A ratio without both parts is neither fixed nor lenient; a lenient ratio is fixed.
    public void CCropRatioApply(bool ratioFixed, bool lenient, int width, int height)
    {
        bool lValid = width > 0 && height > 0;
        CCropPortRead().LCropRatioSet(ratioFixed && lValid, lenient && ratioFixed && lValid, width, height, false);
    }

    // No choice asks for nothing. The custom choice drops a preset ratio; a preset fits the crop to its ratio, unless
    // it is the fixed ratio already or there is no source.
    public void CCropPresetSelect(int index)
    {
        if (index < 0)
        {
            return;
        }

        LCropPort lPort = CCropPortRead();
        var lState = lPort.LCropStateRead();
        if (index == 0)
        {
            if (lState.LCropFixed && CCropPresetResolve(lState.LCropWidth, lState.LCropHeight) != 0)
            {
                lPort.LCropRatioReset();
            }

            return;
        }

        (int lWidth, int lHeight) = _cCropPresets[index];
        if (lWidth == lState.LCropWidth && lHeight == lState.LCropHeight && lState.LCropFixed)
        {
            return;
        }

        if (!lPort.LCropSourceRead().LCropPresent)
        {
            return;
        }

        lPort.LCropPresetSet(lWidth, lHeight);
    }

    // No choice selected (a negative index) asks for nothing.
    public void CCropRotateSelect(int index)
    {
        if (index < 0)
        {
            return;
        }

        CCropPortRead().LCropRotateSet(_cCropRotations[index]);
    }

    public void CCropFlipSet(bool horizontal, bool flipped) => CCropPortRead().LCropFlipSet(horizontal, flipped);

    private static int CCropPresetResolve(int width, int height) =>
        Math.Max(0, Array.IndexOf(_cCropPresets, (width, height)));

    private static double CCropNumberParse(string text) =>
        double.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out double number) ? number : 0;

    // A switch left out keeps its held state; leniency needs a fixed ratio. The edge lock is cleared.
    private void CCropRatioChange(bool? ratioFixed, bool? lenient)
    {
        LCropPort lPort = CCropPortRead();
        var lState = lPort.LCropStateRead();
        bool lFixed = ratioFixed ?? lState.LCropFixed;
        lPort.LCropRatioSet(
            lFixed, lFixed && (lenient ?? lState.LCropLenient), lState.LCropWidth, lState.LCropHeight, true);
    }

    private void CCropRaise() => CCropChanged?.Invoke();

    private LCropPort CCropPortRead() =>
        _cCropPort ?? throw new InvalidOperationException("The crop gate has no crop record.");
}
