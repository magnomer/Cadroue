using System.Globalization;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public enum LWindowDropEffect
{
    LWindowDropNone,
    LWindowDropFile,
}

public sealed record LWindowDrag(
    string[]? LWindowDragPaths,
    bool LWindowDragGroup,
    string? LWindowDragSource,
    bool LWindowDragCopyable,
    object? LWindowDragData);

public sealed class LWindowDrop
{
    private const int LWindowDropIndent = 14;
    private readonly LWindow lWindow;
    private readonly List<string> lWindowDropTrace = [];
    private object? lWindowDropData;
    private LWindowDropEffect? lWindowDropLast;
    private LWindowDropEffect lWindowDropEffect;
    private bool lWindowDropHandled;

    public LWindowDrop(LWindow lOwner)
    {
        lWindow = lOwner;
    }

    public event Action<IReadOnlyList<string>>? LWindowListAdd;
    public event Action<string>? LWindowViewerOpen;

    public LWindowDropEffect LWindowDropEffect => lWindowDropEffect;

    public bool LWindowDropHandled => lWindowDropHandled;

    public void LWindowEnterHandle(LWindowDrag lDrag)
    {
        if (!lDrag.LWindowDragGroup)
        {
            LWindowDropEffect lEffect = LWindowDropResolve(lDrag, out _);
            LWindowDropAppend(
                lDrag,
                "Drag entered window: "
                + (lEffect == LWindowDropEffect.LWindowDropNone
                    ? "will REFUSE (forbidden cursor)"
                    : $"will accept ({lEffect})"),
                $"originalSource={lDrag.LWindowDragSource ?? "null"}, "
                + $"list={(lWindow.LWindowListPresent ? "present" : "NULL")}, "
                + $"viewer={(lWindow.LWindowViewerPresent ? "present" : "NULL")}, "
                + $"audioTab={lWindow.LWindowAudioAllowed}, groupAncestor={lDrag.LWindowDragGroup}");
        }

        LWindowOverHandle(lDrag);
    }

    public void LWindowOverHandle(LWindowDrag lDrag)
    {
        if (lDrag.LWindowDragGroup)
        {
            lWindowDropLast = null;
            LWindowDropSet(LWindowDropEffect.LWindowDropNone, false);
            return;
        }

        LWindowDropStart(lDrag);
        LWindowDropEffect lEffect = LWindowDropResolve(lDrag, out string lReason);
        if (lEffect != lWindowDropLast)
        {
            lWindowDropLast = lEffect;
            LWindowDropAppend(
                lDrag,
                "Drag over: "
                + (lEffect == LWindowDropEffect.LWindowDropNone ? "REFUSED (forbidden cursor)" : lEffect.ToString()),
                lReason);
        }

        LWindowDropSet(lEffect, true);
    }

    public void LWindowDropHandle(LWindowDrag lDrag)
    {
        if (lDrag.LWindowDragGroup)
        {
            LWindowDropSet(LWindowDropEffect.LWindowDropNone, false);
            return;
        }

        lWindowDropLast = null;
        LWindowDropEffect lEffect = LWindowDropResolve(lDrag, out string lReason);
        LWindowDropSet(lEffect, true);

        string lTarget = lWindow.LWindowListPresent ? "list" : lWindow.LWindowViewerPresent ? "viewer" : "none";
        LWindowDropAppend(
            lDrag,
            $"Drop released: {(lEffect == LWindowDropEffect.LWindowDropNone ? "REFUSED" : "accepted")} onto {lTarget}",
            lReason);

        if (lEffect == LWindowDropEffect.LWindowDropNone)
        {
            LWindowDropRecord($"File drag refused onto {lTarget}");
            return;
        }

        IReadOnlyList<string> lPaths = LWindowPathsRead(lDrag);
        if (lWindow.LWindowListPresent)
        {
            LWindowListAdd?.Invoke(lPaths);
            LWindowDropAppend(lDrag, $"Drop into list: {lPaths.Count} path(s) handed to the list scan");
            LWindowDropRecord($"File drag accepted onto list ({lEffect})");
            return;
        }

        if (!lWindow.LWindowViewerPresent)
        {
            LWindowDropRecord($"File drag accepted onto {lTarget} ({lEffect})");
            return;
        }

        string? lSourcePath = LWindowPathRead(lDrag);
        if (lSourcePath is null)
        {
            LWindowDropSet(LWindowDropEffect.LWindowDropNone, true);
            LWindowDropAppend(lDrag, "Drop into viewer refused: no existing file in payload");
            LWindowDropRecord("File drag refused onto viewer", true);
            return;
        }

        LWindowViewerOpen?.Invoke(lSourcePath);
        LWindowDropAppend(lDrag, $"Drop into viewer: opened {LUsher.LUsherNameRead(lSourcePath)}");
        LWindowDropRecord($"File drag accepted onto viewer ({lEffect})");
    }

    private void LWindowDropSet(LWindowDropEffect lEffect, bool lHandled)
    {
        lWindowDropEffect = lEffect;
        lWindowDropHandled = lHandled;
    }

    private LWindowDropEffect LWindowDropResolve(LWindowDrag lDrag, out string lReason)
    {
        IReadOnlyList<string> lPaths = LWindowPathsRead(lDrag);
        string lPayload = lPaths.Count == 0
            ? "no FileDrop payload"
            : $"{lPaths.Count} path(s): {string.Join(", ", lPaths.Select(LUsher.LUsherNameRead))}";

        if (lWindow.LWindowListPresent)
        {
            bool lListMatch = LWindowMediaCheck(lPaths);
            lReason = lListMatch
                ? $"target=list, {lPayload}"
                : $"target=list, none are media/folders — {lPayload}";
            return lListMatch ? LWindowAllowedRead(lDrag) : LWindowDropEffect.LWindowDropNone;
        }

        if (!lWindow.LWindowViewerPresent)
        {
            lReason = $"no active list or viewer (active tab has no drop target); {lPayload}";
            return LWindowDropEffect.LWindowDropNone;
        }

        string? lSourcePath = LWindowPathRead(lDrag);
        if (lSourcePath is null)
        {
            lReason = $"target=viewer, no existing file in payload — {lPayload}";
            return LWindowDropEffect.LWindowDropNone;
        }

        if (LMedia.LMediaAudioCheck(lSourcePath) && !lWindow.LWindowAudioAllowed)
        {
            lReason = "target=viewer, audio-only file on a video-only tab — "
                + $"{LUsher.LUsherNameRead(lSourcePath)}";
            return LWindowDropEffect.LWindowDropNone;
        }

        lReason = $"target=viewer, {LUsher.LUsherNameRead(lSourcePath)}";
        return LWindowAllowedRead(lDrag);
    }

    private static LWindowDropEffect LWindowAllowedRead(LWindowDrag lDrag) =>
        lDrag.LWindowDragCopyable ? LWindowDropEffect.LWindowDropFile : LWindowDropEffect.LWindowDropNone;

    private static IReadOnlyList<string> LWindowPathsRead(LWindowDrag lDrag) =>
        lDrag.LWindowDragPaths ?? [];

    private static string? LWindowPathRead(LWindowDrag lDrag) => LWindowFileFind(LWindowPathsRead(lDrag));

    internal static string? LWindowFileFind(IReadOnlyList<string> lPaths) =>
        lPaths.FirstOrDefault(LUsher.LUsherFileExist);

    internal static bool LWindowMediaCheck(IReadOnlyList<string> lPaths) =>
        lPaths.Any(lPath => LUsher.LUsherFolderExist(lPath) || LMedia.LMediaCheck(lPath));

    private void LWindowDropAppend(LWindowDrag lDrag, string lSummary, string? lDetail = null)
    {
        LWindowDropStart(lDrag);
        string lTime = DateTimeOffset.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        lWindowDropTrace.Add($"{lTime}  {lSummary}");
        if (!string.IsNullOrWhiteSpace(lDetail))
        {
            lWindowDropTrace.Add($"{new string(' ', LWindowDropIndent)}{lDetail}");
        }
    }

    private void LWindowDropStart(LWindowDrag lDrag)
    {
        if (ReferenceEquals(lWindowDropData, lDrag.LWindowDragData))
        {
            return;
        }

        lWindowDropData = lDrag.LWindowDragData;
        lWindowDropTrace.Clear();
        lWindowDropLast = null;
    }

    private void LWindowDropRecord(string lSummary, bool lWarning = false)
    {
        string? lDetail = lWindowDropTrace.Count == 0
            ? null
            : string.Join(Environment.NewLine, lWindowDropTrace);
        if (lWarning)
        {
            LTraceLog.LTraceWarningRecord(lSummary, lDetail);
        }
        else
        {
            LTraceLog.LTraceInfoRecord(lSummary, lDetail);
        }

        lWindowDropData = null;
        lWindowDropTrace.Clear();
        lWindowDropLast = null;
    }
}
