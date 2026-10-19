using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed record CSEncoderChoice(string CSEncoderChoiceValue, string CSEncoderChoiceLabel);

public sealed class CSEncoderExtra
{
    private readonly LSEncoderPort _csEncoderExtraPort;
    private readonly string _csEncoderExtraDefault;

    internal CSEncoderExtra(LCapabilityExtra codec, LSEncoderPort draft)
    {
        _csEncoderExtraPort = draft;
        _csEncoderExtraDefault = codec.LCapabilityExtraDefault;
        CSEncoderExtraLabel = codec.LCapabilityExtraLabel;
        CSEncoderExtraOption = codec.LCapabilityExtraOption;
        CSEncoderExtraChoices = codec.LCapabilityExtraValues
            .Select(entry => new CSEncoderChoice(entry.LCapabilityChoiceValue, entry.LCapabilityChoiceLabel))
            .ToArray();
    }

    public string CSEncoderExtraLabel { get; }

    public string CSEncoderExtraOption { get; }

    public IReadOnlyList<CSEncoderChoice> CSEncoderExtraChoices { get; }

    public int CSEncoderExtraIndex
    {
        get
        {
            (_, _, _, IReadOnlyDictionary<string, string> extras) = _csEncoderExtraPort.LSEncoderRateRead();
            string? stored = extras.TryGetValue(CSEncoderExtraOption, out string? value) ? value : null;
            string[] values = CSEncoderExtraChoices.Select(entry => entry.CSEncoderChoiceValue).ToArray();
            string selected = stored is not null && values.Contains(stored) ? stored : _csEncoderExtraDefault;
            int index = Array.IndexOf(values, selected);
            return values.Length == 0 ? -1 : Math.Max(0, index);
        }
    }
}
