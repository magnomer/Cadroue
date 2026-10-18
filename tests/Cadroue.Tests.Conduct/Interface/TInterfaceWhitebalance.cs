using Cadroue.ShellEngine;

namespace Cadroue.Tests.Conduct;

internal static partial class TInterface
{
    internal static LWhitebalanceAnswer TWhitebalanceAnswerCreate(
        bool active,
        string token,
        double saturation,
        double red,
        double green,
        double blue,
        int sampleRed,
        int sampleGreen,
        int sampleBlue) => new(active, token, saturation, red, green, blue, sampleRed, sampleGreen, sampleBlue);
}
