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
    private readonly IReadOnlyList<LRepertoireAudio> _lsRepertoireAudioCandidates;
    private readonly Func<string, string, bool> _lsRepertoireAudioFilter;
    private readonly Func<string, string?> _lsRepertoireAudioToken;
    private readonly Func<string, LCapabilityCodec> _lsRepertoireAudioCapability;

    public LSRepertoireOutlet(
        Func<IReadOnlyList<LRepertoireEncoder>> lRepertoire,
        IReadOnlyList<string> lMedia,
        Func<string, string, bool> lFilter,
        Func<string, string?> lToken,
        Func<string?, LCapabilityCodec> lCapability,
        IReadOnlyList<LRepertoireAudio> lAudio,
        Func<string, string, bool> lAudioFilter,
        Func<string, string?> lAudioToken,
        Func<string, LCapabilityCodec> lAudioCapability)
    {
        ArgumentNullException.ThrowIfNull(lRepertoire);
        ArgumentNullException.ThrowIfNull(lMedia);
        ArgumentNullException.ThrowIfNull(lFilter);
        ArgumentNullException.ThrowIfNull(lToken);
        ArgumentNullException.ThrowIfNull(lCapability);
        ArgumentNullException.ThrowIfNull(lAudio);
        ArgumentNullException.ThrowIfNull(lAudioFilter);
        ArgumentNullException.ThrowIfNull(lAudioToken);
        ArgumentNullException.ThrowIfNull(lAudioCapability);
        _lsRepertoireReader = lRepertoire;
        _lsRepertoireContainers = lMedia;
        _lsRepertoireFilter = lFilter;
        _lsRepertoireToken = lToken;
        _lsRepertoireCapability = lCapability;
        _lsRepertoireAudioCandidates = lAudio;
        _lsRepertoireAudioFilter = lAudioFilter;
        _lsRepertoireAudioToken = lAudioToken;
        _lsRepertoireAudioCapability = lAudioCapability;
    }

    public IReadOnlyList<LRepertoireEncoder> LSRepertoireEncodersRead() => _lsRepertoireReader();

    public IReadOnlyList<string> LSRepertoireContainerNames => _lsRepertoireContainers;

    public bool LSRepertoireContainerCheck(string lText, string lContainer) => _lsRepertoireFilter(lText, lContainer);

    public string? LSRepertoireTokenResolve(string lText) => _lsRepertoireToken(lText);

    public LCapabilityCodec LSRepertoireCodecRead(string? lEncoder) => _lsRepertoireCapability(lEncoder);

    public IReadOnlyList<LRepertoireAudio> LSRepertoireAudioCandidates => _lsRepertoireAudioCandidates;

    public bool LSRepertoireAudioCheck(string lName, string lContainer) => _lsRepertoireAudioFilter(lName, lContainer);

    public string? LSRepertoireAudioResolve(string lText) => _lsRepertoireAudioToken(lText);

    public LCapabilityCodec LSRepertoireAudioRead(string lText) => _lsRepertoireAudioCapability(lText);
}
