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
}
