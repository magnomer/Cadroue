using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed class CSEncoderOutput
{
    private readonly LSEncoderPort _csEncoderOutputPort;

    internal CSEncoderOutput(LSEncoderPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _csEncoderOutputPort = port;
    }

    public (string, string, string, string, string) CSEncoderOutputRead() => _csEncoderOutputPort.LSEncoderOutputRead();

    public IReadOnlyList<string> CSEncoderOutputRead(string container) =>
        _csEncoderOutputPort.LSEncoderExtensionRead(container);

    public void CSEncoderOutputSet(
        string display,
        string container,
        string extension,
        string collision,
        string suffix,
        string location,
        string folder)
    {
        _csEncoderOutputPort.LSEncoderOutputSet(display, container, extension, collision, location);
        _csEncoderOutputPort.LSEncoderSuffixSet(collision, suffix.Trim());
        _csEncoderOutputPort.LSEncoderLocationSet(location, folder.Trim());
    }
}
