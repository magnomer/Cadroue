namespace Cadroue.ShellEngine;

// One checkup result for the clinic gate, read as plain values.
public interface LCheckupPort
{
    (string LCheckupSource, string LCheckupKind, bool LCheckupScanning) LCheckupRead();

    string LCheckupFormat(IReadOnlyList<string> lWording);
}
