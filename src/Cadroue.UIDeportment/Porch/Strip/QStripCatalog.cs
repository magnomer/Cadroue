namespace Cadroue.UIDeportment;

// The tab kinds a strip can open, in their menu order; an unknown kind opens as the default one.
public static class QStripCatalog
{
    private const string QStripDefaultKey = "Split";
    private static readonly string[] QStripKeys =
        ["Split", "Edit", "Fix", "Audio", "Convert", "Merge", "Funnel", "Worklist"];

    public static IReadOnlyList<string> QStripKeysRead() => QStripKeys;

    public static string QStripKeyResolve(string lKey) =>
        QStripKeys.Contains(lKey, StringComparer.Ordinal) ? lKey : QStripDefaultKey;
}
