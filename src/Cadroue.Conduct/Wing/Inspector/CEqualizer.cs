using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The equalizer tool decisions: the switch, bands and preset it holds, the preset rows, and when a change is told.
// Bands are (frequency, gain) pairs; the engine holds each edit inside its ranges and names the preset it matches.
// The token is the preset last chosen, kept while edits drift from it; a step without a match keeps it.
public sealed class CEqualizer
{
    private const double _cEqualizerGainOrigin = 0;
    private const string _cEqualizerCustomToken = "Custom";
    private const string _cEqualizerCustomKey = "Inspector.Common.Custom";

    private readonly LEqualizerPort _cEqualizerPort;
    private readonly IReadOnlyList<string> _cEqualizerTokens;
    private readonly double _cEqualizerFrequencyOrigin;
    private bool _cEqualizerActive;
    private IReadOnlyList<(double CEqualizerFrequency, double CEqualizerGain)> _cEqualizerBands;
    private string? _cEqualizerPreset;
    private string? _cEqualizerToken;
    private bool _cEqualizerPersistent;

    internal CEqualizer(LEqualizerPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cEqualizerPort = port;
        _cEqualizerTokens = port.LEqualizerTokensRead();
        _cEqualizerFrequencyOrigin = port.LEqualizerFrequencyOrigin;
        (_cEqualizerBands, _cEqualizerPreset) = port.LEqualizerOriginRead();
        _cEqualizerToken = _cEqualizerPreset;
    }

    public event Action? CEqualizerChanged;

    public event Action? CEqualizerRowsChanged;

    public bool CEqualizerActive => _cEqualizerActive;

    public bool CEqualizerPersistent => _cEqualizerPersistent;

    public IReadOnlyList<(double CEqualizerFrequency, double CEqualizerGain)> CEqualizerBands => _cEqualizerBands;

    public IReadOnlyList<(
        int CEqualizerRowIndex,
        string CEqualizerRowFrequency,
        double CEqualizerRowGain)> CEqualizerRows => _cEqualizerBands
        .Select((band, index) => (
            index,
            CInspectorField.CInspectorValueFormat(string.Empty, band.CEqualizerFrequency, "0.###"),
            band.CEqualizerGain))
        .ToList();

    public string? CEqualizerToken => _cEqualizerToken;

    public string? CEqualizerPreset => _cEqualizerPreset;

    public double CEqualizerGainLeast => _cEqualizerPort.LEqualizerGainLeast;

    public double CEqualizerGainMost => _cEqualizerPort.LEqualizerGainMost;

    public double CEqualizerGainOrigin => _cEqualizerGainOrigin;

    // One row per preset and a custom row last. The custom row shows the preset it drifted from, when there is one.
    public IReadOnlyList<CChoice> CEqualizerChoices =>
        CChoicePreset.CChoicePresetRead(_cEqualizerTokens, _cEqualizerPreset, _cEqualizerToken, CEqualizerKeyRead);

    public double CEqualizerGainRead(int index) =>
        index >= 0 && index < _cEqualizerBands.Count ? _cEqualizerBands[index].CEqualizerGain : 0;

    public string CEqualizerFrequencyRead(int index) => CInspectorField.CInspectorValueFormat(
        string.Empty,
        index >= 0 && index < _cEqualizerBands.Count
            ? _cEqualizerBands[index].CEqualizerFrequency
            : _cEqualizerFrequencyOrigin,
        "0.###");

    // A plan's step; it carries its own match, and a step without one drops the chosen token.
    internal void CEqualizerStepSet(bool active, IReadOnlyList<(double, double)> bands, string? preset)
    {
        ArgumentNullException.ThrowIfNull(bands);
        CEqualizerApply(active, bands, preset, null);
    }

    public void CEqualizerActiveSet(bool active)
    {
        if (_cEqualizerActive == active)
        {
            return;
        }

        _cEqualizerActive = active;
        CEqualizerChanged?.Invoke();
    }

    public void CEqualizerPersistentSet(bool persistent)
    {
        if (_cEqualizerPersistent == persistent)
        {
            return;
        }

        _cEqualizerPersistent = persistent;
        CEqualizerChanged?.Invoke();
    }

    // An index out of range or a band equal to the held one after its normal form is not told.
    public void CEqualizerBandSet(int index, double frequency, double gain)
    {
        if (index < 0 || index >= _cEqualizerBands.Count)
        {
            return;
        }

        (IReadOnlyList<(double, double)> bands, string? preset) =
            _cEqualizerPort.LEqualizerBandSet(_cEqualizerBands, index, frequency, gain);
        if (bands[index] == _cEqualizerBands[index])
        {
            return;
        }

        CEqualizerApply(_cEqualizerActive, bands, preset, _cEqualizerToken);
    }

    public void CEqualizerGainSet(int index, double gain) => CEqualizerBandSet(
        index,
        index >= 0 && index < _cEqualizerBands.Count
            ? _cEqualizerBands[index].CEqualizerFrequency
            : _cEqualizerFrequencyOrigin,
        gain);

    // Text that is not a number keeps the held frequency. The answer is the band's frequency after the commit.
    public string CEqualizerFrequencyCommit(int index, string text)
    {
        double current = index >= 0 && index < _cEqualizerBands.Count
            ? _cEqualizerBands[index].CEqualizerFrequency
            : _cEqualizerFrequencyOrigin;
        double frequency = CInspectorField.CInspectorValueCommit(text, current, null, null);
        CEqualizerBandSet(index, frequency, CEqualizerGainRead(index));
        double committed = index >= 0 && index < _cEqualizerBands.Count
            ? _cEqualizerBands[index].CEqualizerFrequency
            : _cEqualizerFrequencyOrigin;
        return CInspectorField.CInspectorValueFormat(text, committed, "0.###");
    }

    public void CEqualizerBandAdd()
    {
        (IReadOnlyList<(double, double)> bands, string? preset) = _cEqualizerPort.LEqualizerBandAdd(_cEqualizerBands);
        CEqualizerApply(_cEqualizerActive, bands, preset, _cEqualizerToken);
    }

    public void CEqualizerBandRemove(int index)
    {
        if (index < 0 || index >= _cEqualizerBands.Count)
        {
            return;
        }

        (IReadOnlyList<(double, double)> bands, string? preset) =
            _cEqualizerPort.LEqualizerBandRemove(_cEqualizerBands, index);
        CEqualizerApply(_cEqualizerActive, bands, preset, _cEqualizerToken);
    }

    // An unknown preset is ignored.
    public void CEqualizerPresetSelect(string token)
    {
        if (_cEqualizerPort.LEqualizerPresetRead(token) is not { } answer)
        {
            return;
        }

        CEqualizerApply(_cEqualizerActive, answer.LEqualizerBands, answer.LEqualizerPreset, token);
    }

    // The custom row, an index out of range and the preset already chosen and matched are ignored.
    public void CEqualizerChoiceSelect(int index)
    {
        if (index < 0 || index >= _cEqualizerTokens.Count)
        {
            return;
        }

        string selected = _cEqualizerTokens[index];
        if (selected == _cEqualizerCustomToken || (selected == _cEqualizerToken && _cEqualizerPreset == selected))
        {
            return;
        }

        CEqualizerPresetSelect(selected);
    }

    // The rows change is told only when the band count changes; it comes before the change.
    private void CEqualizerApply(
        bool active, IReadOnlyList<(double, double)> bands, string? preset, string? fallback)
    {
        string? token = preset ?? fallback;
        if (_cEqualizerActive == active && _cEqualizerBands.SequenceEqual(bands) && _cEqualizerToken == token)
        {
            return;
        }

        bool rows = _cEqualizerBands.Count != bands.Count;
        _cEqualizerActive = active;
        _cEqualizerBands = bands;
        _cEqualizerPreset = preset;
        _cEqualizerToken = token;
        if (rows)
        {
            CEqualizerRowsChanged?.Invoke();
        }

        CEqualizerChanged?.Invoke();
    }

    private static string CEqualizerKeyRead(string token) => token switch
    {
        "Flat" => "Inspector.Equalizer.Preset.Flat",
        "Bass boost" => "Inspector.Equalizer.Preset.BassBoost",
        "Bright" => "Inspector.Equalizer.Preset.Bright",
        "Warm" => "Inspector.Equalizer.Preset.Warm",
        "Loudness" => "Inspector.Equalizer.Preset.Loudness",
        "Vocal" => "Inspector.Equalizer.Preset.Vocal",
        "De-ess" => "Inspector.Equalizer.Preset.Deess",
        "Podcast" => "Inspector.Equalizer.Preset.Podcast",
        "Telephone" => "Inspector.Equalizer.Preset.Telephone",
        _ => _cEqualizerCustomKey
    };
}
