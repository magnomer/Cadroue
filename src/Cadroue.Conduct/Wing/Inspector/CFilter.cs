using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// One pass filter's decisions, high or low: the switch, values, poles and persistence it holds, the preset and pole
// rows, and when a change is told. Slots run 0 to 2 in display order: frequency, stages, resonance. The token is the
// preset last chosen, kept while edits drift from it; the engine names the match.
public sealed class CFilter
{
    private const string _cFilterCustomToken = "Custom";
    private const string _cFilterCustomKey = "Inspector.Common.Custom";

    // The pole rows in display order; each label stands as its own wording key, so it reads unchanged.
    private static readonly string[] _cFilterPoleKeys = { "1 (6 dB)", "2 (12 dB)" };

    private readonly LFilterPort _cFilterPort;
    private readonly bool _cFilterHigh;
    private readonly IReadOnlyList<string> _cFilterTokens;
    private bool _cFilterActive;
    private IReadOnlyList<double> _cFilterValues;
    private int _cFilterPoles;
    private string? _cFilterToken;
    private bool _cFilterPersistent;

    internal CFilter(LFilterPort port, bool high)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cFilterPort = port;
        _cFilterHigh = high;
        _cFilterTokens = port.LFilterTokensRead(high);
        (_cFilterValues, _cFilterPoles, _cFilterToken) = port.LFilterOriginRead(high);
    }

    public event Action? CFilterChanged;

    public bool CFilterActive => _cFilterActive;

    public bool CFilterPersistent => _cFilterPersistent;

    public IReadOnlyList<double> CFilterValues => _cFilterValues;

    public int CFilterPoles => _cFilterPoles;

    public string? CFilterToken => _cFilterToken;

    // The preset the held values and poles match, or null.
    public string? CFilterPreset => _cFilterPort.LFilterMatchFind(_cFilterHigh, _cFilterValues, _cFilterPoles);

    public IReadOnlyList<double> CFilterLeasts => _cFilterPort.LFilterLeastsRead(_cFilterHigh);

    public IReadOnlyList<double> CFilterMosts => _cFilterPort.LFilterMostsRead(_cFilterHigh);

    // Resonance applies only to two poles.
    public bool CFilterResonanceActive => _cFilterPoles == 2;

    // One row per preset and a custom row last. The custom row shows the preset it drifted from, when there is one.
    public IReadOnlyList<CChoice> CFilterChoices =>
        CChoicePreset.CChoicePresetRead(_cFilterTokens, CFilterPreset, _cFilterToken, CFilterKeyRead);

    // One row per pole count, one pole first; the held count chosen.
    public IReadOnlyList<CChoice> CFilterPoleChoices =>
        _cFilterPoleKeys
            .Select((key, index) => new CChoice(
                (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                key,
                null,
                index == (_cFilterPoles == 1 ? 0 : 1)))
            .ToList();

    // A slot out of range reads as the frequency.
    public double CFilterValueRead(int slot) => _cFilterValues[CFilterSlotResolve(slot)];

    // The value a reset returns to: the chosen preset's, or the default without one.
    public double CFilterDefaultRead(int slot) =>
        _cFilterPort.LFilterDefaultRead(_cFilterHigh, _cFilterToken, _cFilterValues[0])[CFilterSlotResolve(slot)];

    // A plan's step; its match becomes the token.
    internal void CFilterStepSet(bool active, IReadOnlyList<double> values, int poles, string? preset)
    {
        ArgumentNullException.ThrowIfNull(values);
        CFilterApply(active, values, poles, preset);
    }

    public void CFilterActiveSet(bool active) => CFilterApply(active, _cFilterValues, _cFilterPoles, _cFilterToken);

    // A slot out of range changes no value. The token is kept while the values drift from it.
    public void CFilterValueSet(int slot, double value)
    {
        List<double> values = _cFilterValues.ToList();
        if (slot >= 0 && slot < values.Count)
        {
            values[slot] = value;
        }

        CFilterNormalApply(values, _cFilterPoles);
    }

    // An index out of range is ignored.
    public void CFilterPolesSelect(int index)
    {
        if (index < 0 || index >= _cFilterPoleKeys.Length)
        {
            return;
        }

        CFilterNormalApply(_cFilterValues, index + 1);
    }

    // An unknown preset is ignored; the switch stays.
    public void CFilterPresetSelect(string token)
    {
        if (_cFilterPort.LFilterPresetRead(_cFilterHigh, token) is not { } answer)
        {
            return;
        }

        CFilterApply(_cFilterActive, answer.LFilterValues, answer.LFilterPoles, token);
    }

    // The custom row, an index out of range and the preset already chosen and matched are ignored.
    public void CFilterChoiceSelect(int index)
    {
        if (index < 0 || index >= _cFilterTokens.Count)
        {
            return;
        }

        string selected = _cFilterTokens[index];
        if (selected == _cFilterCustomToken || (selected == _cFilterToken && CFilterPreset == selected))
        {
            return;
        }

        CFilterPresetSelect(selected);
    }

    public void CFilterPersistentSet(bool persistent)
    {
        if (_cFilterPersistent == persistent)
        {
            return;
        }

        _cFilterPersistent = persistent;
        CFilterChanged?.Invoke();
    }

    private int CFilterSlotResolve(int slot) => slot >= 0 && slot < _cFilterValues.Count ? slot : 0;

    private void CFilterNormalApply(IReadOnlyList<double> values, int poles)
    {
        (IReadOnlyList<double> normal, int normalPoles) = _cFilterPort.LFilterNormalize(_cFilterHigh, values, poles);
        CFilterApply(_cFilterActive, normal, normalPoles, _cFilterToken);
    }

    private void CFilterApply(bool active, IReadOnlyList<double> values, int poles, string? token)
    {
        if (_cFilterActive == active
            && _cFilterValues.SequenceEqual(values)
            && _cFilterPoles == poles
            && _cFilterToken == token)
        {
            return;
        }

        _cFilterActive = active;
        _cFilterValues = values;
        _cFilterPoles = poles;
        _cFilterToken = token;
        CFilterChanged?.Invoke();
    }

    // An unknown token reads as custom.
    private static string CFilterKeyRead(string token) => token switch
    {
        "Rumble" => "Inspector.Pass.Preset.Rumble",
        "Wind" => "Inspector.Pass.Preset.Wind",
        "Voice" => "Inspector.Pass.Preset.Voice",
        "Speech (tight)" => "Inspector.Pass.Preset.SpeechTight",
        "Tighten" => "Inspector.Pass.Preset.Tighten",
        "Air tame" => "Inspector.Pass.Preset.Airtame",
        "Soften" => "Inspector.Pass.Preset.Soften",
        "Warm" => "Inspector.Pass.Preset.Warm",
        "AM radio" => "Inspector.Pass.Preset.AmRadio",
        "Telephone" => "Inspector.Pass.Preset.Telephone",
        _ => _cFilterCustomKey
    };
}
