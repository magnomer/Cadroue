using System.Collections.Generic;

namespace Cadroue.Conduct;

public sealed record CSSpectrumRow(
    string CSSpectrumRowName,
    IReadOnlyList<string> CSSpectrumRowColors,
    bool CSSpectrumRowNative,
    string? CSSpectrumRowPath);

public sealed record CSSpectrumSnapshot(IReadOnlyList<CSSpectrumRow> CSSpectrumSnapshotRows);
