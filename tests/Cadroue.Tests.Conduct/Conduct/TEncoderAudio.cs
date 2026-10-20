using System;
using System.Collections.Generic;

using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

public sealed class TEncoderAudio : LSRepertoirePort
{
    [Fact]
    public void AudioDetail_ChoiceChange_DoesNotReusePriorCodec()
    {
        LSEncoderPort draft = Interface.TInterface.TEncoderOpenerCreate()(string.Empty);
        var audio = new CSEncoderAudio("Prior", "Target bitrate", new TTrialPort(), this, draft);
        _ = audio.CSEncoderAudioRead((string?)null, (double?)null);

        Assert.True(audio.CSEncoderAudioSet("Current", "Target bitrate"));
        audio.CSEncoderAudioSave("Include", "Encode", string.Empty, 1, [], null, null);

        CSEncoderSound saved = audio.CSEncoderAudioRead((string?)null, (double?)null);
        Assert.Equal("One", saved.CSEncoderSoundCodec.CSEncoderCodecSpeed?.CSEncoderSpeedShown);
    }

    public IReadOnlyList<LRepertoireEncoder> LSRepertoireEncodersRead() => [];

    public IReadOnlyList<string> LSRepertoireContainerNames { get; } = [];

    public bool LSRepertoireContainerCheck(string lText, string lContainer) => false;

    public string? LSRepertoireTokenResolve(string lText) => lText;

    public LCapabilityCodec LSRepertoireCodecRead(string? lEncoder) =>
        new(lEncoder ?? string.Empty, [new("Target bitrate")]);

    public IReadOnlyList<LRepertoireAudio> LSRepertoireAudioCandidates { get; } = [];

    public bool LSRepertoireAudioCheck(string lName, string lContainer) => false;

    public string? LSRepertoireAudioResolve(string lText) => lText;

    public LCapabilityCodec LSRepertoireAudioRead(string lText)
    {
        bool prior = lText != "Current";
        string stem = prior ? "prior" : "current";
        return new(
            lText,
            [new("Target bitrate")],
            new(
                "Speed",
                "-speed",
                $"{stem}-zero",
                [new($"{stem}-zero", "Zero"), new($"{stem}-one", "One")]));
    }

    public (LCapabilityCodec, LCapabilityMode) LSRepertoireAudioRead(string lText, string lRate)
    {
        LCapabilityCodec codec = LSRepertoireAudioRead(lText);
        return (codec, codec.LCapabilityModes[0]);
    }
}
