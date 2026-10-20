using System;
using System.Collections.Generic;

using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

public sealed class TEncoderVideo : LSRepertoirePort
{
    [Fact]
    public void VideoMode_EmptyChoices_DoesNotThrow()
    {
        LSEncoderPort draft = Interface.TInterface.TEncoderOpenerCreate()(string.Empty);

        var video = new CSEncoderVideo("Empty", string.Empty, new TTrialPort(), this, draft, false);

        Assert.Empty(video.CSEncoderVideoState.CSEncoderStateCodec.CSEncoderCodecModes);
    }

    public IReadOnlyList<LRepertoireEncoder> LSRepertoireEncodersRead() => [];

    public IReadOnlyList<string> LSRepertoireContainerNames { get; } = [];

    public bool LSRepertoireContainerCheck(string lText, string lContainer) => false;

    public string? LSRepertoireTokenResolve(string lText) => lText;

    public LCapabilityCodec LSRepertoireCodecRead(string? lEncoder) => new(lEncoder ?? string.Empty, []);

    public IReadOnlyList<LRepertoireAudio> LSRepertoireAudioCandidates { get; } = [];

    public bool LSRepertoireAudioCheck(string lName, string lContainer) => false;

    public string? LSRepertoireAudioResolve(string lText) => null;

    public LCapabilityCodec LSRepertoireAudioRead(string lText) => new(lText, [new("Default")]);

    public (LCapabilityCodec, LCapabilityMode) LSRepertoireAudioRead(string lText, string lRate)
    {
        LCapabilityCodec codec = LSRepertoireAudioRead(lText);
        return (codec, codec.LCapabilityModes[0]);
    }
}
