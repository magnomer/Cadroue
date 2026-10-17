using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The white balance tool decisions: the step, its persistence, the editor and preview facts, the automatic estimate,
// the picker's arming and target, and the status the last pick left, with when each change is told. The method rows
// run in the order of CWhitebalanceTokens; Manual is the sampled method. The engine keeps the step's rule and defaults.
public sealed class CWhitebalance
{
    private const string CWhitebalanceManualToken = "Manual";

    private static readonly string[] _cWhitebalanceMethods =
        { "Average", "Minmax", "Median", CWhitebalanceManualToken };

    private static readonly IReadOnlyList<string> _cWhitebalanceTokens = Array.AsReadOnly(_cWhitebalanceMethods);

    // Each method row's wording key, in the order of the tokens; Manual shows as Custom.
    private static readonly IReadOnlyList<string> _cWhitebalanceKeys = Array.AsReadOnly(new[]
    {
        "Inspector.Video.WhitebalanceMethodAverage",
        "Inspector.Video.WhitebalanceMethodMinmax",
        "Inspector.Video.WhitebalanceMethodMedian",
        "Inspector.Video.WhitebalanceMethodCustom"
    });

    private readonly LWhitebalancePort _cWhitebalancePort;
    private LWhitebalanceAnswer _cWhitebalanceAnswer;
    private bool _cWhitebalancePersistent;
    private bool _cWhitebalanceCapable;
    private bool _cWhitebalancePreview;
    private (double CWhitebalanceX, double CWhitebalanceY, bool CWhitebalancePresent) _cWhitebalanceEstimate =
        (0, 0, false);
    private bool _cWhitebalanceArmed;
    private bool _cWhitebalancePending;
    private bool _cWhitebalanceWhite;
    private string _cWhitebalanceStatus = string.Empty;

    internal CWhitebalance(LWhitebalancePort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cWhitebalancePort = port;
        _cWhitebalanceAnswer = port.LWhitebalanceCreate(false);
        CWhitebalanceSaturationOrigin = _cWhitebalanceAnswer.LWhitebalanceSaturation;
    }

    public event Action? CWhitebalanceChanged;

    // A method other than Manual took over: the estimate is cleared and the owner starts one for the token.
    public event Action<string>? CWhitebalanceEstimateChanged;

    // The picker armed, disarmed or changed target: armed, then whether the target is white.
    public event Action<bool, bool>? CWhitebalanceToolChanged;

    // The step a plan keeps; the engine turns it into the video step.
    public LWhitebalanceAnswer CWhitebalanceAnswer => _cWhitebalanceAnswer;

    public bool CWhitebalanceActive => _cWhitebalanceAnswer.LWhitebalanceActive;

    public string CWhitebalanceToken => _cWhitebalanceAnswer.LWhitebalanceToken;

    public double CWhitebalanceSaturation => _cWhitebalanceAnswer.LWhitebalanceSaturation;

    public double CWhitebalanceSaturationLeast => 0;

    public double CWhitebalanceSaturationMost => 300;

    // The Core default saturation, where the field resets to.
    public double CWhitebalanceSaturationOrigin { get; }

    public IReadOnlyList<string> CWhitebalanceTokens => _cWhitebalanceTokens;

    public IReadOnlyList<string> CWhitebalanceMethodKeys => _cWhitebalanceKeys;

    public int CWhitebalanceMethodIndex => Array.IndexOf(_cWhitebalanceMethods, CWhitebalanceToken);

    public bool CWhitebalanceManual => CWhitebalanceToken == CWhitebalanceManualToken;

    public bool CWhitebalancePersistent => _cWhitebalancePersistent;

    public bool CWhitebalanceCapable => _cWhitebalanceCapable;

    public bool CWhitebalancePreview => _cWhitebalancePreview;

    public bool CWhitebalanceArmed => _cWhitebalanceArmed;

    public bool CWhitebalanceWhite => _cWhitebalanceWhite;

    public bool CWhitebalanceGreyArmed => _cWhitebalanceArmed && !_cWhitebalanceWhite;

    public bool CWhitebalanceWhiteArmed => _cWhitebalanceArmed && _cWhitebalanceWhite;

    // The picker's guide while it is armed, otherwise the status the last pick left: a localization key, or empty.
    public string CWhitebalanceGuideKey => _cWhitebalanceArmed
        ? _cWhitebalanceWhite ? "Inspector.Video.WhitebalanceGuideWhite" : "Inspector.Video.WhitebalanceGuide"
        : _cWhitebalanceStatus;

    public bool CWhitebalanceGuideShown => CWhitebalanceGuideKey.Length > 0;

    // The wheel point: the sample's colour under Manual, the last estimate otherwise.
    public (double CWhitebalanceX, double CWhitebalanceY, bool CWhitebalancePresent) CWhitebalanceWheelRead() =>
        CWhitebalanceManual
            ? _cWhitebalancePort.LWhitebalanceWheelRead(
                _cWhitebalanceAnswer.LWhitebalanceSampleRed,
                _cWhitebalanceAnswer.LWhitebalanceSampleGreen,
                _cWhitebalanceAnswer.LWhitebalanceSampleBlue)
            : _cWhitebalanceEstimate;

    // Where the wheel dot of the given size sits on a wheel of the given size, and whether it shows.
    public (double CWhitebalanceLeft, double CWhitebalanceTop, bool CWhitebalancePresent) CWhitebalanceDotRead(
        double size, double dot)
    {
        (double x, double y, bool present) = CWhitebalanceWheelRead();
        return _cWhitebalancePort.LWhitebalanceDotRead(x, y, present, size, dot);
    }

    // The sampled colour the readout shows; sampled only under Manual with a colour picked.
    public (bool CWhitebalanceSampled, int CWhitebalanceRed, int CWhitebalanceGreen, int CWhitebalanceBlue)
        CWhitebalanceReadoutRead() =>
        _cWhitebalancePort.LWhitebalanceDisplayRead(
            CWhitebalanceManual,
            _cWhitebalanceAnswer.LWhitebalanceSampleRed,
            _cWhitebalanceAnswer.LWhitebalanceSampleGreen,
            _cWhitebalanceAnswer.LWhitebalanceSampleBlue);

    // The colour wheel's pixels at the given size and brightness, rows of four bytes a pixel.
    public (int CWhitebalanceSize, int CWhitebalanceStride, byte[] CWhitebalancePixels) CWhitebalanceBitmapRead(
        int size, double value) =>
        _cWhitebalancePort.LWhitebalanceBitmapRead(size, value);

    internal void CWhitebalanceStepSet(LWhitebalanceAnswer answer)
    {
        ArgumentNullException.ThrowIfNull(answer);
        CWhitebalanceRaise(CWhitebalanceApply(_cWhitebalancePort.LWhitebalanceNormalize(answer)));
    }

    // Switching off also disarms the picker.
    public void CWhitebalanceActiveSet(bool active)
    {
        bool changed = CWhitebalanceApply(_cWhitebalanceAnswer with { LWhitebalanceActive = active });
        if (!active)
        {
            changed |= CWhitebalanceToolApply(false);
        }

        CWhitebalanceRaise(changed);
    }

    // An index outside the method rows is ignored.
    public void CWhitebalanceMethodSelect(int index)
    {
        if (index < 0 || index >= _cWhitebalanceTokens.Count)
        {
            return;
        }

        CWhitebalanceRaise(CWhitebalanceApply(_cWhitebalancePort.LWhitebalanceNormalize(
            _cWhitebalanceAnswer with { LWhitebalanceToken = _cWhitebalanceTokens[index] })));
    }

    public void CWhitebalanceSaturationSet(double saturation) =>
        CWhitebalanceRaise(CWhitebalanceApply(_cWhitebalancePort.LWhitebalanceNormalize(
            _cWhitebalanceAnswer with { LWhitebalanceSaturation = saturation })));

    // The step returns to the Core default, keeping its switch, and the picker disarms.
    public void CWhitebalanceReset()
    {
        bool changed = CWhitebalanceToolApply(false);
        changed |= CWhitebalanceApply(_cWhitebalancePort.LWhitebalanceCreate(_cWhitebalanceAnswer.LWhitebalanceActive));
        CWhitebalanceRaise(changed);
    }

    // An estimate counts only while the method is not Manual.
    public void CWhitebalanceEstimateSet(double x, double y, bool present)
    {
        if (CWhitebalanceManual || _cWhitebalanceEstimate == (x, y, present))
        {
            return;
        }

        _cWhitebalanceEstimate = (x, y, present);
        CWhitebalanceChanged?.Invoke();
    }

    // A new frame asks for a fresh estimate, unless the method is Manual.
    public void CWhitebalanceEstimateStart()
    {
        if (CWhitebalanceManual)
        {
            return;
        }

        CWhitebalanceEstimateChanged?.Invoke(CWhitebalanceToken);
    }

    // The picker disarms, keeping its target.
    public void CWhitebalanceToolCancel() => CWhitebalanceRaise(CWhitebalanceToolApply(false));

    // The picker arms only while the editor can apply white balance.
    public void CWhitebalanceToolSet(bool armed, bool white) =>
        CWhitebalanceRaise(CWhitebalanceToolApply(armed && _cWhitebalanceCapable, white));

    // A picker button's check arms its target; its uncheck disarms only while that target is the armed one.
    public void CWhitebalanceToolToggle(bool white, bool armed)
    {
        if (!armed && !(_cWhitebalanceArmed && _cWhitebalanceWhite == white))
        {
            return;
        }

        CWhitebalanceToolSet(armed, white);
    }

    // A press or drag on the wheel, in canvas pixels, samples its colour while the editor can apply white balance.
    public void CWhitebalanceWheelHandle(bool pressed, double x, double y, double size)
    {
        if (!pressed || !_cWhitebalanceCapable)
        {
            return;
        }

        (double redGain, double greenGain, double blueGain, int red, int green, int blue) =
            _cWhitebalancePort.LWhitebalanceCanvasRead(x, y, size);
        CWhitebalanceSampleApply(redGain, greenGain, blueGain, red, green, blue);
    }

    // A pick from the viewer: a valid one applies as a wheel sample does, a failed one leaves its status.
    public void CWhitebalanceSampleAccept(LWhitebalanceSamplePort sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        (string status, double redGain, double greenGain, double blueGain, int red, int green, int blue) =
            sample.LWhitebalanceSampleRead();
        switch (status)
        {
            case "Valid":
                CWhitebalanceSampleApply(redGain, greenGain, blueGain, red, green, blue);
                return;
            case "Decode":
                CWhitebalanceStatusSet("Inspector.Video.WhitebalanceDecode");
                return;
            case "Invalid":
                CWhitebalanceStatusSet("Inspector.Video.WhitebalanceInvalid");
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(sample), status, "Unknown white balance pick status.");
        }
    }

    // The status is a localization key, or empty.
    public void CWhitebalanceStatusSet(string statusKey) =>
        CWhitebalanceRaise(CWhitebalanceStatusApply(statusKey ?? string.Empty));

    public void CWhitebalancePersistentSet(bool persistent)
    {
        if (_cWhitebalancePersistent == persistent)
        {
            return;
        }

        _cWhitebalancePersistent = persistent;
        CWhitebalanceChanged?.Invoke();
    }

    // The preview notice holds only while the editor can apply white balance; losing it disarms the picker.
    public void CWhitebalanceCapableSet(bool capable, bool preview)
    {
        bool shown = capable && preview;
        bool changed = _cWhitebalanceCapable != capable || _cWhitebalancePreview != shown;
        _cWhitebalanceCapable = capable;
        _cWhitebalancePreview = shown;
        if (!capable)
        {
            changed |= CWhitebalanceToolApply(false);
        }

        CWhitebalanceRaise(changed);
    }

    // A picked colour switches the step on under Manual with its gains, disarms the picker and clears the status.
    private void CWhitebalanceSampleApply(
        double redGain, double greenGain, double blueGain, int red, int green, int blue)
    {
        bool changed = CWhitebalanceToolApply(false);
        changed |= CWhitebalanceStatusApply(string.Empty);
        changed |= CWhitebalanceApply(_cWhitebalancePort.LWhitebalanceNormalize(new LWhitebalanceAnswer(
            true,
            CWhitebalanceManualToken,
            _cWhitebalanceAnswer.LWhitebalanceSaturation,
            redGain,
            greenGain,
            blueGain,
            red,
            green,
            blue)));
        CWhitebalanceRaise(changed);
    }

    private bool CWhitebalanceApply(LWhitebalanceAnswer normal)
    {
        string previous = CWhitebalanceToken;
        if (_cWhitebalanceAnswer == normal)
        {
            return false;
        }

        _cWhitebalanceAnswer = normal;
        if (previous != CWhitebalanceToken && !CWhitebalanceManual)
        {
            CWhitebalanceEstimateReset();
        }

        return true;
    }

    private void CWhitebalanceEstimateReset()
    {
        _cWhitebalanceEstimate = (0, 0, false);
        CWhitebalanceEstimateChanged?.Invoke(CWhitebalanceToken);
    }

    // Disarming clears the status; a target left out keeps the held one.
    private bool CWhitebalanceToolApply(bool armed, bool? white = null)
    {
        bool next = white ?? _cWhitebalanceWhite;
        if (_cWhitebalanceArmed == armed && (!armed || _cWhitebalanceWhite == next))
        {
            return false;
        }

        _cWhitebalanceArmed = armed;
        _cWhitebalanceWhite = next;
        _cWhitebalancePending = true;
        if (!armed)
        {
            CWhitebalanceStatusApply(string.Empty);
        }

        return true;
    }

    private bool CWhitebalanceStatusApply(string statusKey)
    {
        if (_cWhitebalanceStatus == statusKey)
        {
            return false;
        }

        _cWhitebalanceStatus = statusKey;
        return true;
    }

    private void CWhitebalanceRaise(bool changed)
    {
        if (changed)
        {
            CWhitebalanceChanged?.Invoke();
        }

        if (!_cWhitebalancePending)
        {
            return;
        }

        _cWhitebalancePending = false;
        CWhitebalanceToolChanged?.Invoke(_cWhitebalanceArmed, _cWhitebalanceWhite);
    }
}
