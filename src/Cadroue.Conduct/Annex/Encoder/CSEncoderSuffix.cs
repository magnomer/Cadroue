using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed class CSEncoderSuffix
{
    private readonly LSEncoderPort _csEncoderSuffixPort;
    private string? _csEncoderSuffixMode;

    internal CSEncoderSuffix(LSEncoderPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _csEncoderSuffixPort = port;
    }

    public string CSEncoderSuffixRead(string policy) => _csEncoderSuffixPort.LSEncoderSuffixRead(policy);

    public static string? CSEncoderSuffixResolve(string policy) => policy switch
    {
        "Rename existing" => "Encoder.Field.Output.SuffixSource",
        "Rename output" => "Encoder.Field.Output.SuffixOutput",
        _ => null
    };

    public string CSEncoderSuffixSelect(string policy, string shown)
    {
        if (CSEncoderSuffixFind(policy) is { } previous)
        {
            _csEncoderSuffixPort.LSEncoderSuffixSet(previous, shown.Trim());
        }

        _csEncoderSuffixMode = policy;
        return CSEncoderSuffixResolve(policy) is null ? shown : _csEncoderSuffixPort.LSEncoderSuffixRead(policy);
    }

    public string CSEncoderSuffixNormalize(string policy, string shown)
    {
        if (!string.IsNullOrEmpty(shown.Trim()))
        {
            return shown;
        }

        _csEncoderSuffixPort.LSEncoderSuffixSet(policy, string.Empty);
        return _csEncoderSuffixPort.LSEncoderSuffixRead(policy);
    }

    private string? CSEncoderSuffixFind(string policy) =>
        _csEncoderSuffixMode is not null
        && CSEncoderSuffixResolve(_csEncoderSuffixMode) is not null
        && !string.Equals(_csEncoderSuffixMode, policy, StringComparison.Ordinal)
            ? _csEncoderSuffixMode
            : null;
}

public sealed class CSEncoderLocation
{
    private readonly LSEncoderPort _csEncoderLocationPort;
    private string? _csEncoderLocationMode;

    internal CSEncoderLocation(LSEncoderPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _csEncoderLocationPort = port;
    }

    public (bool, bool, string, string) CSEncoderLocationShown =>
        CSEncoderLocationResolve(_csEncoderLocationMode ?? string.Empty);

    public string CSEncoderLocationRead(string mode) => _csEncoderLocationPort.LSEncoderLocationRead(mode);

    private static (bool, bool, string, string) CSEncoderLocationResolve(string mode) =>
        (!string.Equals(mode, CSEncoderDraft.CSEncoderDraftSource, StringComparison.Ordinal),
            string.Equals(mode, "Custom location", StringComparison.Ordinal),
            mode switch
            {
                "Sibling" => "Encoder.Location.Sibling",
                "Custom location" => "Encoder.Location.Custom",
                _ => "Encoder.Location.Subfolder"
            },
            mode switch
            {
                "Subfolder" => "Encoder.Location.SubfolderStatus",
                "Sibling" => "Encoder.Location.SiblingStatus",
                "Custom location" => "Encoder.Location.CustomStatus",
                _ => "Encoder.Location.Source"
            });

    public string CSEncoderLocationSelect(string mode, string shown)
    {
        string folder = shown;
        if (CSEncoderLocationFind(mode) is { } previous)
        {
            _csEncoderLocationPort.LSEncoderLocationSet(previous, shown.Trim());
            folder = _csEncoderLocationPort.LSEncoderLocationRead(mode);
        }

        _csEncoderLocationMode = mode;
        return folder;
    }

    private string? CSEncoderLocationFind(string mode) =>
        _csEncoderLocationMode is not null
        && !string.Equals(_csEncoderLocationMode, mode, StringComparison.Ordinal)
            ? _csEncoderLocationMode
            : null;
}
