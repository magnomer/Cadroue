using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The clinic's flaw kinds by name, from one explicit table in plan order; an unknown kind or name throws.
internal static class LClinicFlaw
{
    private static readonly IReadOnlyList<(LFlawKind LClinicKind, string LClinicName)> _lClinicFlawKinds =
    [
        (LFlawKind.LFlawKindContainer, "Container"),
        (LFlawKind.LFlawKindTruncation, "Truncation"),
        (LFlawKind.LFlawKindTransport, "Transport"),
        (LFlawKind.LFlawKindMetadata, "Metadata"),
        (LFlawKind.LFlawKindIndex, "Index"),
        (LFlawKind.LFlawKindFraming, "Framing"),
        (LFlawKind.LFlawKindConfig, "Config"),
        (LFlawKind.LFlawKindTiming, "Timing"),
        (LFlawKind.LFlawKindSecondary, "Secondary"),
        (LFlawKind.LFlawKindCoded, "Coded"),
        (LFlawKind.LFlawKindFfvone, "Ffvone")
    ];

    // Every kind the table names, in plan order.
    internal static IEnumerable<LFlawKind> LClinicFlawOrder => _lClinicFlawKinds.Select(lEntry => lEntry.LClinicKind);

    internal static string LClinicNameRead(LFlawKind lKind) =>
        _lClinicFlawKinds.Where(lEntry => lEntry.LClinicKind == lKind).Select(lEntry => lEntry.LClinicName).Single();

    internal static LFlawKind LClinicKindRead(string lName) =>
        _lClinicFlawKinds.Where(lEntry => lEntry.LClinicName == lName).Select(lEntry => lEntry.LClinicKind).Single();
}
