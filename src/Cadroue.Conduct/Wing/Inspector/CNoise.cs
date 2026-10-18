using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The noise reduction tool decisions: the switch, values, tracking, type and persistence it holds, the preset and
// type rows, and when a change is told. Slots run 0 to 4 in display order: reduction, smoothing, floor, residual,
// adaptivity. The token is the preset last chosen, kept while edits drift from it; the engine names the match.
public sealed class CNoise
{
    private const string _cNoiseCustomToken = "Custom";
    private const string _cNoiseCustomKey = "Inspector.Common.Custom";

    private readonly LNoisePort _cNoisePort;
    private readonly IReadOnlyList<string> _cNoiseTokens;
    private readonly IReadOnlyList<string> _cNoiseTypes;
    private bool _cNoiseActive;
    private IReadOnlyList<double> _cNoiseValues;
    private bool _cNoiseTrack;
    private int _cNoiseType;
    private string? _cNoiseToken;
    private bool _cNoisePersistent;

    internal CNoise(LNoisePort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cNoisePort = port;
        _cNoiseTokens = port.LNoiseTokensRead();
        _cNoiseTypes = port.LNoiseTypesRead();
        (_cNoiseValues, _cNoiseType, _cNoiseToken) = port.LNoiseOriginRead();
    }

    public event Action? CNoiseChanged;

    public bool CNoiseActive => _cNoiseActive;

    public bool CNoisePersistent => _cNoisePersistent;

    public bool CNoiseTrack => _cNoiseTrack;

    public int CNoiseType => _cNoiseType;

    public IReadOnlyList<double> CNoiseValues => _cNoiseValues;

    public string? CNoiseToken => _cNoiseToken;

    // The preset the held values and type match, or null.
    public string? CNoisePreset => _cNoisePort.LNoiseMatchFind(_cNoiseValues, _cNoiseType);

    public IReadOnlyList<double> CNoiseLeasts => _cNoisePort.LNoiseLeasts;

    public IReadOnlyList<double> CNoiseMosts => _cNoisePort.LNoiseMosts;

    // One row per preset and a custom row last. The custom row shows the preset it drifted from, when there is one.
    public IReadOnlyList<CChoice> CNoiseChoices =>
        CChoicePreset.CChoicePresetRead(_cNoiseTokens, CNoisePreset, _cNoiseToken, CNoiseKeyRead);

    // One row per noise type, the held type chosen.
    public IReadOnlyList<CChoice> CNoiseTypes =>
        _cNoiseTypes.Select((token, index) => new CChoice(token, CNoiseKeyRead(token), null, index == _cNoiseType))
            .ToList();

    // A slot out of range reads as the reduction.
    public double CNoiseValueRead(int slot) => _cNoiseValues[CNoiseSlotResolve(slot)];

    // The value a reset returns to: the chosen preset's, or the default without one.
    public double CNoiseDefaultRead(int slot) => _cNoisePort.LNoiseDefaultRead(_cNoiseToken)[CNoiseSlotResolve(slot)];

    // A plan's step; its match becomes the token.
    internal void CNoiseStepSet(bool active, IReadOnlyList<double> values, bool track, int type, string? preset)
    {
        ArgumentNullException.ThrowIfNull(values);
        CNoiseApply(active, values, track, type, preset);
    }

    public void CNoiseActiveSet(bool active) =>
        CNoiseApply(active, _cNoiseValues, _cNoiseTrack, _cNoiseType, _cNoiseToken);

    public void CNoiseTrackSet(bool track) =>
        CNoiseApply(_cNoiseActive, _cNoiseValues, track, _cNoiseType, _cNoiseToken);

    // A slot out of range changes no value. The token is kept while the values drift from it.
    public void CNoiseValueSet(int slot, double value)
    {
        List<double> values = _cNoiseValues.ToList();
        if (slot >= 0 && slot < values.Count)
        {
            values[slot] = value;
        }

        CNoiseApply(_cNoiseActive, _cNoisePort.LNoiseNormalize(values), _cNoiseTrack, _cNoiseType, _cNoiseToken);
    }

    // An index out of range is ignored.
    public void CNoiseTypeSelect(int index)
    {
        if (index < 0 || index >= _cNoiseTypes.Count)
        {
            return;
        }

        CNoiseApply(_cNoiseActive, _cNoiseValues, _cNoiseTrack, index, _cNoiseToken);
    }

    // An unknown preset is ignored; the switch and tracking stay.
    public void CNoisePresetSelect(string token)
    {
        if (_cNoisePort.LNoisePresetRead(token) is not { } answer)
        {
            return;
        }

        CNoiseApply(_cNoiseActive, answer.LNoiseValues, _cNoiseTrack, answer.LNoiseType, token);
    }

    // The custom row, an index out of range and the preset already chosen and matched are ignored.
    public void CNoiseChoiceSelect(int index)
    {
        if (index < 0 || index >= _cNoiseTokens.Count)
        {
            return;
        }

        string selected = _cNoiseTokens[index];
        if (selected == _cNoiseCustomToken || (selected == _cNoiseToken && CNoisePreset == selected))
        {
            return;
        }

        CNoisePresetSelect(selected);
    }

    public void CNoisePersistentSet(bool persistent)
    {
        if (_cNoisePersistent == persistent)
        {
            return;
        }

        _cNoisePersistent = persistent;
        CNoiseChanged?.Invoke();
    }

    private int CNoiseSlotResolve(int slot) => slot >= 0 && slot < _cNoiseValues.Count ? slot : 0;

    private void CNoiseApply(bool active, IReadOnlyList<double> values, bool track, int type, string? token)
    {
        if (_cNoiseActive == active
            && _cNoiseValues.SequenceEqual(values)
            && _cNoiseTrack == track
            && _cNoiseType == type
            && _cNoiseToken == token)
        {
            return;
        }

        _cNoiseActive = active;
        _cNoiseValues = values;
        _cNoiseTrack = track;
        _cNoiseType = type;
        _cNoiseToken = token;
        CNoiseChanged?.Invoke();
    }

    // Preset and type tokens share one wording; an unknown token reads as custom.
    private static string CNoiseKeyRead(string token) => token switch
    {
        "Light" => "Inspector.Noise.Light",
        "Medium" => "Inspector.Noise.Medium",
        "Strong" => "Inspector.Noise.Strong",
        "Dialogue" => "Inspector.Noise.Dialogue",
        "White" => "Inspector.Noise.White",
        "Vinyl" => "Inspector.Noise.Vinyl",
        "Shellac" => "Inspector.Noise.Shellac",
        _ => _cNoiseCustomKey
    };
}
