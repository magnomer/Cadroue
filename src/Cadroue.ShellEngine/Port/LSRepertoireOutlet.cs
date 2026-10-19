using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The video encoder repertoire over the reads the host hands in: the entry list, the known container names, the
// container check, the token resolver and the capability read.
public sealed class LSRepertoireOutlet : LSRepertoirePort
{
    private readonly Func<IReadOnlyList<LRepertoireEncoder>> _lsRepertoireReader;
    private readonly IReadOnlyList<string> _lsRepertoireContainers;
    private readonly Func<string, string, bool> _lsRepertoireFilter;
    private readonly Func<string, string?> _lsRepertoireToken;
    private readonly Func<string?, LCapabilityCodec> _lsRepertoireCapability;

    public LSRepertoireOutlet(
        Func<IReadOnlyList<LRepertoireEncoder>> lRepertoire,
        IReadOnlyList<string> lMedia,
        Func<string, string, bool> lFilter,
        Func<string, string?> lToken,
        Func<string?, LCapabilityCodec> lCapability)
    {
        ArgumentNullException.ThrowIfNull(lRepertoire);
        ArgumentNullException.ThrowIfNull(lMedia);
        ArgumentNullException.ThrowIfNull(lFilter);
        ArgumentNullException.ThrowIfNull(lToken);
        ArgumentNullException.ThrowIfNull(lCapability);
        _lsRepertoireReader = lRepertoire;
        _lsRepertoireContainers = lMedia;
        _lsRepertoireFilter = lFilter;
        _lsRepertoireToken = lToken;
        _lsRepertoireCapability = lCapability;
    }

    public IReadOnlyList<LRepertoireEncoder> LSRepertoireEncodersRead() => _lsRepertoireReader();

    public IReadOnlyList<string> LSRepertoireContainerNames => _lsRepertoireContainers;

    public bool LSRepertoireContainerCheck(string lText, string lContainer) => _lsRepertoireFilter(lText, lContainer);

    public string? LSRepertoireTokenResolve(string lText) => _lsRepertoireToken(lText);

    public LCapabilityCodec LSRepertoireCodecRead(string? lEncoder) => _lsRepertoireCapability(lEncoder);
}
