using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The video encoder repertoire and capability table as the encoder window's gates read them: the entries, the
// containers it knows, which entries a container takes, and the codec each encoder token reads.
public interface LSRepertoirePort
{
    // Every repertoire entry, in repertoire order.
    IReadOnlyList<LRepertoireEncoder> LSRepertoireEncodersRead();

    // The containers the repertoire knows by name.
    IReadOnlyList<string> LSRepertoireContainerNames { get; }

    // Whether a container takes the entry with this text.
    bool LSRepertoireContainerCheck(string lText, string lContainer);

    // The encoder token an entry's text names; null when no entry has the text.
    string? LSRepertoireTokenResolve(string lText);

    // The codec an encoder token reads; an unknown token reads the fallback codec.
    LCapabilityCodec LSRepertoireCodecRead(string? lEncoder);

    // Every audio repertoire entry, in repertoire order.
    IReadOnlyList<LRepertoireAudio> LSRepertoireAudioCandidates { get; }

    // Whether a container takes the audio codec with this name.
    bool LSRepertoireAudioCheck(string lName, string lContainer);

    // The audio codec name an entry's text names; null when no audio entry has the text.
    string? LSRepertoireAudioResolve(string lText);

    // The audio codec an encoder text reads; an unknown text reads the fallback audio codec.
    LCapabilityCodec LSRepertoireAudioRead(string lText);
}
