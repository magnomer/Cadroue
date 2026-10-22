using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed record QExportEntry(
    string QExportEntryName,
    string? QExportEntryGroup,
    bool QExportEntryHeader,
    bool QExportEntryUser,
    bool QExportEntryCollapsed);

public sealed class QExportList(
    Func<IEnumerable<string>> names,
    Func<string, bool> native,
    Func<string, string?> group,
    Func<string, bool, bool> foldRead,
    Action<string, bool, bool> foldSet)
{
    private const string QExportListUser = "$User";

    public QExportList() : this(
        () => LPresetTable.LPresetNames,
        LPresetTable.LPresetNativeCheck,
        LPresetTable.LPresetGroupRead,
        (name, fallback) => LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(name, fallback),
        LPreference.LPreferenceFoldSet)
    {
    }

    public IReadOnlyList<QExportEntry> QExportListRead()
    {
        var entries = new List<QExportEntry>();
        string? nativeGroupCurrent = null;
        bool userHeaderAdded = false;
        foreach (string name in names())
        {
            bool presetNative = native(name);
            string? groupName = presetNative ? group(name) : null;
            bool groupHeader = presetNative
                && groupName is not null
                && !string.Equals(nativeGroupCurrent, groupName, StringComparison.OrdinalIgnoreCase);
            bool userHeader = !presetNative && !userHeaderAdded;
            nativeGroupCurrent = groupHeader ? groupName : nativeGroupCurrent;
            userHeaderAdded = userHeaderAdded || userHeader;
            bool collapsed = presetNative && groupName is not null
                ? foldRead(groupName, true)
                : foldRead(QExportListUser, false);
            entries.Add(new(name, groupName, groupHeader || userHeader, !presetNative, collapsed));
        }

        return entries;
    }

    public void QExportListToggle(QExportEntry entry, Action rebuild)
    {
        string? groupName = entry.QExportEntryUser ? QExportListUser : entry.QExportEntryGroup;
        if (groupName is null)
        {
            return;
        }

        bool fallback = !entry.QExportEntryUser;
        foldSet(groupName, !foldRead(groupName, fallback), fallback);
        rebuild();
    }

    public bool QExportListShow(string name, Action rebuild)
    {
        bool presetNative = native(name);
        string? groupName = presetNative ? group(name) : QExportListUser;
        bool fallback = presetNative;
        if (groupName is null || !foldRead(groupName, fallback))
        {
            return false;
        }

        foldSet(groupName, false, fallback);
        rebuild();
        return true;
    }
}
