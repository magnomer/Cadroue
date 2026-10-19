using Cadroue.Core;

namespace Cadroue.Application;

public static class LSCodec
{
    public static LCapabilityCodec LSCodecAudioRead(string text) =>
        LCapability.LCapabilityAudioRead(LCapability.LCapabilityNameRead(text));

    public static (LCapabilityCodec, LCapabilityMode) LSCodecAudioRead(string text, string rate)
    {
        LCapabilityCodec codec = LSCodecAudioRead(text);
        return (codec, codec.LCapabilityModeFind(rate));
    }
}
