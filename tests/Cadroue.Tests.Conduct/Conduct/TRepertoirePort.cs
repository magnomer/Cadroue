using System.Collections.Generic;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// An empty repertoire: no entries, no known containers, no container takes an entry, no text names a token, and every
// token reads one codec with a single mode.
internal sealed class TRepertoirePort : LSRepertoirePort
{
    public IReadOnlyList<LRepertoireEncoder> LSRepertoireEncodersRead() => [];

    public IReadOnlyList<string> LSRepertoireContainerNames { get; } = [];

    public bool LSRepertoireContainerCheck(string lText, string lContainer) => false;

    public string? LSRepertoireTokenResolve(string lText) => null;

    public LCapabilityCodec LSRepertoireCodecRead(string? lEncoder) => new(lEncoder ?? string.Empty, [new("Default")]);
}
