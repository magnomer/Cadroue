namespace Cadroue.ShellEngine;

public interface LTonePort
{
    (bool LToneFlyleaf, bool LToneMpv) LTonePreviewRead();
}
