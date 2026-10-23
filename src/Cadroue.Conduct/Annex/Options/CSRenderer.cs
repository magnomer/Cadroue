using System;

namespace Cadroue.Conduct;

public enum CSRendererState
{
    CSRendererStateBlank,
    CSRendererStateReady,
    CSRendererStateProgram,
    CSRendererStateLibrary,
    CSRendererStateMissing
}

public sealed record CSRendererSnapshot(
    string CSRendererSnapshotPath,
    CSRendererState CSRendererSnapshotState);

public sealed class CSRenderer
{
    private readonly CSRendererPort _csRendererPort;

    public CSRenderer(CSRendererPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _csRendererPort = port;
    }

    public CSRendererSnapshot CSRendererRead(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new CSRendererSnapshot(path, CSRendererState.CSRendererStateBlank);
        }

        bool program = _csRendererPort.CSRendererProgramCheck(path);
        bool library = _csRendererPort.CSRendererLibraryCheck(path);
        CSRendererState state = (program, library) switch
        {
            (true, true) => CSRendererState.CSRendererStateReady,
            (true, false) => CSRendererState.CSRendererStateProgram,
            (false, true) => CSRendererState.CSRendererStateLibrary,
            _ => CSRendererState.CSRendererStateMissing
        };
        return new CSRendererSnapshot(path, state);
    }
}
