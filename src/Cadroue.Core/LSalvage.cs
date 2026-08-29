using System;

namespace Cadroue.Core;

public enum LSalvageMode
{
    LSalvageModeRejoin,
    LSalvageModeSeparate
}

public enum LSalvageBasis
{
    LSalvageBasisSource,
    LSalvageBasisFixed
}

public readonly record struct LSalvageSpan(TimeSpan LSalvageSpanOrigin, TimeSpan LSalvageSpanLimit);

public sealed record LSalvageOutput(string LSalvageOutputName, LSalvageSpan LSalvageOutputSpan);
