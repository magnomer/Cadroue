// Generated file. Do not edit by hand: every line is overwritten when it is rebuilt.
//
// AUDITNAMES GENERATION 26 - name registry for Cadroue.
// One of the generated files in the convention-test project. Together they carry the
// registered bases, verbs and exemptions and nothing else; every audit setting is hand-written
// tracked test code and must never be added here.
//
// Order: regenerate it first, then run the convention tests. The tests compile against this file as
// it is on disk, so a newly registered name is invisible to them until it is regenerated.

namespace Convention.Tests;

internal static class TAuditExemptRegistry
{
    // AUDIT:SIDECAR:EXEMPT:START
    public static readonly Dictionary<string, string[]> TAuditExempt = new(StringComparer.Ordinal)
    {
        ["PART_ContentHost"] = ["*"],
        ["PART_EditableTextBox"] = ["*"],
        ["PART_Indicator"] = ["*"],
        ["PART_Popup"] = ["*"],
        ["PART_Track"] = ["*"],
    };
    // AUDIT:SIDECAR:EXEMPT:END
}
