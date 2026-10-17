using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The leveling tool decisions: the switch, mode, values, two-pass and persistence it holds, the mode and preset rows,
// and when a change is told. Values run in one slot order: target, peak, range for the loudness mode, then frame,
// smoothness, maximum gain and compression for the dynamic mode; a mode's row index counts within its own slots. The
// token is the preset last chosen, kept while edits drift from it; the engine names the match.
public sealed class CLoudness
{
    private const string _cLoudnessCustomToken = "Custom";
    private const string _cLoudnessCustomKey = "Inspector.Common.Custom";
    private const string _cLoudnessCustomBased = "Inspector.Common.PresetCustom";
    private const int _cLoudnessDynamicFirst = 3;
    private const int _cLoudnessLevelCount = 3;
    private const int _cLoudnessDynamicCount = 4;

    // The mode rows in display order: loudness, then dynamic.
    private static readonly string[] _cLoudnessModeTokens = { "Loudness", "Dynamic" };

    private static readonly string[] _cLoudnessModeKeys =
    {
        "Inspector.Normalize.Loudness", "Inspector.Normalize.Dynamic"
    };

    private readonly LLoudnessPort _cLoudnessPort;
    private readonly IReadOnlyList<string> _cLoudnessLevelTokens;
    private readonly IReadOnlyList<string> _cLoudnessDynamicTokens;
    private bool _cLoudnessActive;
    private bool _cLoudnessDynamic;
    private IReadOnlyList<double> _cLoudnessValues;
    private bool _cLoudnessTwoPass;
    private string? _cLoudnessToken;
    private bool _cLoudnessPersistent;

    internal CLoudness(LLoudnessPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cLoudnessPort = port;
        _cLoudnessLevelTokens = port.LLoudnessTokensRead(false);
        _cLoudnessDynamicTokens = port.LLoudnessTokensRead(true);
        (_cLoudnessDynamic, _cLoudnessValues, _cLoudnessTwoPass, _cLoudnessToken) = port.LLoudnessOriginRead();
    }

    public event Action? CLoudnessChanged;

    public bool CLoudnessActive => _cLoudnessActive;

    public bool CLoudnessDynamic => _cLoudnessDynamic;

    public IReadOnlyList<double> CLoudnessValues => _cLoudnessValues;

    public bool CLoudnessTwoPass => _cLoudnessTwoPass;

    public bool CLoudnessPersistent => _cLoudnessPersistent;

    public string? CLoudnessToken => _cLoudnessToken;

    // The preset of the held mode the held values match, or null.
    public string? CLoudnessPreset => _cLoudnessPort.LLoudnessMatchFind(_cLoudnessDynamic, _cLoudnessValues);

    // One row per mode, the held mode chosen.
    public IReadOnlyList<CChoice> CLoudnessModes =>
        _cLoudnessModeTokens
            .Select((token, index) => new CChoice(token, _cLoudnessModeKeys[index], null, index == CLoudnessModeIndex))
            .ToList();

    // Each mode's row ranges, in mode order.
    public IReadOnlyList<IReadOnlyList<double>> CLoudnessLeasts =>
        [_cLoudnessPort.LLoudnessLeastsRead(false), _cLoudnessPort.LLoudnessLeastsRead(true)];

    public IReadOnlyList<IReadOnlyList<double>> CLoudnessMosts =>
        [_cLoudnessPort.LLoudnessMostsRead(false), _cLoudnessPort.LLoudnessMostsRead(true)];

    // Each mode's row values, in mode order.
    public IReadOnlyList<IReadOnlyList<double>> CLoudnessRows =>
    [
        _cLoudnessValues.Take(_cLoudnessLevelCount).ToList(),
        _cLoudnessValues.Skip(_cLoudnessDynamicFirst).Take(_cLoudnessDynamicCount).ToList()
    ];

    // Each mode's preset rows, in mode order.
    public IReadOnlyList<IReadOnlyList<CChoice>> CLoudnessChoices =>
        [CLoudnessChoicesRead(false), CLoudnessChoicesRead(true)];

    // One row per preset of the mode and a custom row last, each mode matched on its own slots whatever the held
    // mode. The custom row shows the preset it drifted from, when there is one.
    public IReadOnlyList<CChoice> CLoudnessChoicesRead(bool dynamic)
    {
        string? preset = _cLoudnessPort.LLoudnessMatchFind(dynamic, _cLoudnessValues);
        List<CChoice> rows = CLoudnessTokensRead(dynamic)
            .Select(token => new CChoice(token, CLoudnessKeyRead(dynamic, token), null, token == preset))
            .ToList();
        bool drifted = preset is null && _cLoudnessToken is not null;
        rows.Add(new CChoice(
            null,
            drifted ? _cLoudnessCustomBased : _cLoudnessCustomKey,
            drifted ? CLoudnessKeyRead(dynamic, _cLoudnessToken!) : null,
            !rows.Any(row => row.CChoiceChosen)));
        return rows;
    }

    // A row out of range reads as the mode's first row.
    public double CLoudnessValueRead(bool dynamic, int index) =>
        _cLoudnessValues[(dynamic ? _cLoudnessDynamicFirst : 0) + CLoudnessRowResolve(dynamic, index)];

    // The value a reset returns to: the token's preset of the mode, or the default without one.
    public double CLoudnessDefaultRead(bool dynamic, int index) =>
        _cLoudnessPort.LLoudnessDefaultRead(dynamic, _cLoudnessToken)[CLoudnessRowResolve(dynamic, index)];

    // A plan's step; its match becomes the token.
    internal void CLoudnessStepSet(
        bool active, bool dynamic, IReadOnlyList<double> values, bool twoPass, string? preset)
    {
        ArgumentNullException.ThrowIfNull(values);
        CLoudnessApply(active, dynamic, values, twoPass, preset);
    }

    public void CLoudnessActiveSet(bool active) =>
        CLoudnessApply(active, _cLoudnessDynamic, _cLoudnessValues, _cLoudnessTwoPass, _cLoudnessToken);

    public void CLoudnessTwopassSet(bool twoPass) =>
        CLoudnessApply(_cLoudnessActive, _cLoudnessDynamic, _cLoudnessValues, twoPass, _cLoudnessToken);

    // An index out of range is ignored. The new mode's match becomes the token.
    public void CLoudnessModeSelect(int index)
    {
        if (index < 0 || index >= _cLoudnessModeTokens.Length)
        {
            return;
        }

        bool dynamic = index == 1;
        CLoudnessApply(
            _cLoudnessActive,
            dynamic,
            _cLoudnessValues,
            _cLoudnessTwoPass,
            _cLoudnessPort.LLoudnessMatchFind(dynamic, _cLoudnessValues));
    }

    // Either mode's rows take values whatever the held mode; a row out of range changes no value. The token is kept
    // while the values drift from it.
    public void CLoudnessValueSet(bool dynamic, int index, double value)
    {
        double[] values = _cLoudnessValues.ToArray();
        if (CLoudnessRowCheck(dynamic, index))
        {
            values[(dynamic ? _cLoudnessDynamicFirst : 0) + index] = value;
        }

        CLoudnessApply(
            _cLoudnessActive,
            _cLoudnessDynamic,
            _cLoudnessPort.LLoudnessNormalize(values),
            _cLoudnessTwoPass,
            _cLoudnessToken);
    }

    // An unknown preset of the held mode is ignored; the switch stays.
    public void CLoudnessPresetSelect(string token)
    {
        if (_cLoudnessPort.LLoudnessPresetRead(_cLoudnessDynamic, token, _cLoudnessValues) is not { } values)
        {
            return;
        }

        CLoudnessApply(_cLoudnessActive, _cLoudnessDynamic, values, _cLoudnessTwoPass, token);
    }

    // Only the held mode's list selects. The custom row, an index out of range and the preset already chosen and
    // matched are ignored.
    public void CLoudnessChoiceSelect(bool dynamic, int index)
    {
        if (dynamic != _cLoudnessDynamic)
        {
            return;
        }

        IReadOnlyList<string> tokens = CLoudnessTokensRead(dynamic);
        if (index < 0 || index >= tokens.Count)
        {
            return;
        }

        string selected = tokens[index];
        if (selected == _cLoudnessCustomToken || (selected == _cLoudnessToken && CLoudnessPreset == selected))
        {
            return;
        }

        CLoudnessPresetSelect(selected);
    }

    public void CLoudnessPersistentSet(bool persistent)
    {
        if (_cLoudnessPersistent == persistent)
        {
            return;
        }

        _cLoudnessPersistent = persistent;
        CLoudnessChanged?.Invoke();
    }

    private int CLoudnessModeIndex => _cLoudnessDynamic ? 1 : 0;

    private IReadOnlyList<string> CLoudnessTokensRead(bool dynamic) =>
        dynamic ? _cLoudnessDynamicTokens : _cLoudnessLevelTokens;

    private static bool CLoudnessRowCheck(bool dynamic, int index) =>
        index >= 0 && index < (dynamic ? _cLoudnessDynamicCount : _cLoudnessLevelCount);

    private static int CLoudnessRowResolve(bool dynamic, int index) => CLoudnessRowCheck(dynamic, index) ? index : 0;

    private void CLoudnessApply(
        bool active, bool dynamic, IReadOnlyList<double> values, bool twoPass, string? token)
    {
        if (_cLoudnessActive == active
            && _cLoudnessDynamic == dynamic
            && _cLoudnessValues.SequenceEqual(values)
            && _cLoudnessTwoPass == twoPass
            && _cLoudnessToken == token)
        {
            return;
        }

        _cLoudnessActive = active;
        _cLoudnessDynamic = dynamic;
        _cLoudnessValues = values;
        _cLoudnessTwoPass = twoPass;
        _cLoudnessToken = token;
        CLoudnessChanged?.Invoke();
    }

    // Each mode names only its own presets; an unknown token reads as custom.
    private static string CLoudnessKeyRead(bool dynamic, string token) => dynamic
        ? token switch
        {
            "Gentle" => "Inspector.Dynamic.Gentle",
            "Leveler" => "Inspector.Dynamic.Leveler",
            "Voice" => "Inspector.Dynamic.Voice",
            "Aggressive" => "Inspector.Dynamic.Aggressive",
            "Music" => "Inspector.Dynamic.Music",
            _ => _cLoudnessCustomKey
        }
        : token switch
        {
            "Loud" => "Inspector.Normalize.Loud",
            "Streaming" => "Inspector.Normalize.Streaming",
            "Podcast" => "Inspector.Normalize.Podcast",
            "Dialogue" => "Inspector.Normalize.Dialogue",
            "Audiobook" => "Inspector.Normalize.Audiobook",
            "Broadcast" => "Inspector.Normalize.Broadcast",
            "TV" => "Inspector.Normalize.TV",
            "Film" => "Inspector.Normalize.Film",
            _ => _cLoudnessCustomKey
        };
}
