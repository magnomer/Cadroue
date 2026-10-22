using System;
using System.Collections.Generic;

namespace Cadroue.Conduct;

// Inspector-wide frame decisions: selected section, minimized state, crop-tool coordination, and display answers.
public sealed class CInspectorFrame
{
    private static readonly IReadOnlyDictionary<string, string> _cInspectorTitleKeys =
        new Dictionary<string, string>
        {
            ["Crop"] = "Inspector.Step.Crop",
            ["Brightness"] = "Inspector.Step.Brightness",
            ["Contrast"] = "Inspector.Step.Contrast",
            ["Saturation"] = "Inspector.Step.Saturation",
            ["Gamma"] = "Inspector.Step.Gamma",
            ["Exposure"] = "Inspector.Step.Exposure",
            ["Curve"] = "Inspector.Step.Curve",
            ["Whitebalance"] = "Inspector.Step.Whitebalance",
            ["Volume"] = "Inspector.Step.Volume",
            ["Normalize"] = "Inspector.Step.Normalize",
            ["Noise Reduction"] = "Inspector.Step.NoiseReduction",
            ["High Pass"] = "Inspector.Step.HighPass",
            ["Low Pass"] = "Inspector.Step.LowPass",
            ["Equalizer"] = "Inspector.Step.Equalizer",
            ["No Processing"] = "Inspector.Step.NoProcessing"
        };

    private static readonly IReadOnlyDictionary<string, string> _cInspectorSensorKeys =
        new Dictionary<string, string>
        {
            ["Blank"] = "Inspector.Step.Blank",
            ["Scene"] = "Inspector.Step.Scene",
            ["Still Image"] = "Inspector.Step.Still",
            ["Luminance"] = "Inspector.Step.Luminance",
            ["Silence"] = "Inspector.Step.Silence",
            ["Volume"] = "Inspector.Step.Volume"
        };

    private readonly CInspector _cInspector;
    private bool _cInspectorMinimized;
    private string? _cInspectorStep;
    private bool _cInspectorToolArmed;

    internal CInspectorFrame(CInspector inspector)
    {
        _cInspector = inspector;
        _cInspector.CInspectorCrop.CCropChanged += CInspectorCropHandle;
        _cInspector.CInspectorWhitebalance.CWhitebalanceToolChanged += CInspectorNeutralHandle;
    }

    public event Action? CInspectorChanged;

    public event Action<bool>? CInspectorToolChanged;

    public event Action<bool>? CInspectorMinimizeChanged;

    public bool CInspectorMinimized => _cInspectorMinimized;

    public bool CInspectorToolArmed => _cInspectorToolArmed;

    public bool CInspectorPersistentShown =>
        _cInspectorStep is { } step
        && _cInspectorTitleKeys.ContainsKey(step)
        && !_cInspectorSensorKeys.ContainsKey(step);

    public bool CInspectorEmptyShown =>
        !CInspectorPersistentShown && (_cInspectorStep is null || !_cInspectorSensorKeys.ContainsKey(_cInspectorStep));

    public string CInspectorTitleKey =>
        _cInspectorStep is { } step && _cInspectorSensorKeys.TryGetValue(step, out string? sensorKey)
            ? sensorKey
            : _cInspectorTitleKeys.GetValueOrDefault(_cInspectorStep ?? string.Empty, "Inspector.Header.Title");

    public bool CInspectorSectionCheck(string key) =>
        _cInspectorStep == key && !_cInspectorSensorKeys.ContainsKey(_cInspectorStep);

    public bool CInspectorSensorCheck(string key) => _cInspectorStep == key;

    public void CInspectorMinimizedSet(bool minimized)
    {
        if (_cInspectorMinimized == minimized)
        {
            return;
        }

        _cInspectorMinimized = minimized;
        CInspectorToolsNormalize();
        CInspectorChanged?.Invoke();
        CInspectorMinimizeChanged?.Invoke(minimized);
    }

    public void CInspectorStepSet(string? step)
    {
        if (_cInspectorStep == step)
        {
            return;
        }

        _cInspectorStep = step;
        CInspectorToolsNormalize();
        CInspectorChanged?.Invoke();
    }

    public void CInspectorToolSet(bool armed)
    {
        if (armed)
        {
            _cInspector.CInspectorWhitebalance.CWhitebalanceToolCancel();
        }

        if (_cInspectorToolArmed == armed)
        {
            return;
        }

        _cInspectorToolArmed = armed;
        CInspectorChanged?.Invoke();
        CInspectorToolChanged?.Invoke(armed);
    }

    public void CInspectorCropReset()
    {
        if (_cInspector.CInspectorCrop.CCropReset())
        {
            CInspectorToolSet(false);
        }
    }

    private void CInspectorToolsNormalize()
    {
        if (_cInspectorMinimized || _cInspectorStep != "Whitebalance")
        {
            _cInspector.CInspectorWhitebalance.CWhitebalanceToolCancel();
        }

        if (_cInspectorStep != "Crop")
        {
            CInspectorToolSet(false);
        }
    }

    private void CInspectorCropHandle()
    {
        if (!_cInspector.CInspectorCrop.CCropActive)
        {
            CInspectorToolSet(false);
        }
    }

    private void CInspectorNeutralHandle(bool armed, bool white)
    {
        if (armed)
        {
            CInspectorToolSet(false);
        }
    }
}
