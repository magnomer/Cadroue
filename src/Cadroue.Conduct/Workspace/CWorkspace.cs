using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed record CWorkspaceIntent(
    string CWorkspaceIntentPath,
    TimeSpan CWorkspaceIntentPosition,
    bool? CWorkspaceIntentPlaying);

// The single home of the viewer tenure and its load lifecycle: the drivers ask it and keep no copy.
public sealed class CWorkspace
{
    private readonly LWorkspacePort _cWorkspacePort;

    private string? _cWorkspaceRequest;

    // The intent the last commit consumed, spent once when the player takes the committed media.
    private CWorkspaceIntent? _cWorkspaceCommitIntent;

    internal CWorkspace(LWorkspacePort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cWorkspacePort = port;
    }

    // A path asked and not held: the drivers open it.
    public event Action<string>? CWorkspaceOpened;

    // A load asked again, pending or held: the drivers load it as given.
    public event Action<string>? CWorkspaceRestored;

    // No load is pending: the player plays, pauses or seeks now.
    public event Action? CWorkspacePlayed;

    public event Action? CWorkspacePaused;

    public event Action<TimeSpan>? CWorkspaceSought;

    // A load is pending: the seek waits in the intent, and the drivers show its place.
    public event Action<TimeSpan>? CWorkspaceIntentSought;

    // The player took the committed media: it starts at the given place, playing or paused.
    public event Action<TimeSpan>? CWorkspaceCommitPlayed;

    public event Action<TimeSpan>? CWorkspaceCommitPaused;

    public string? CWorkspaceSource { get; private set; }

    public CWorkspaceIntent? CWorkspaceIntent { get; private set; }

    public bool CWorkspaceMedia { get; private set; }

    private bool CWorkspaceLoaded => CWorkspaceSource is not null || CWorkspaceMedia || CWorkspaceIntent is not null;

    // A committed media consumes the pending intent; CWorkspaceCommitApply spends it.
    public void CWorkspaceMediaSet(string? source, bool present)
    {
        CWorkspaceSource = source;
        _cWorkspaceCommitIntent = CWorkspaceIntent;
        CWorkspaceIntent = null;
        CWorkspaceMedia = present;
    }

    // The player takes the committed media: the intent its commit consumed is raised once, at its place
    // and play state, or at the start with the given default play state when none was pending.
    public void CWorkspaceCommitApply(bool autoplay)
    {
        CWorkspaceIntent? committed = _cWorkspaceCommitIntent;
        _cWorkspaceCommitIntent = null;
        TimeSpan position = committed?.CWorkspaceIntentPosition ?? TimeSpan.Zero;
        bool playing = committed?.CWorkspaceIntentPlaying ?? autoplay;
        (playing ? CWorkspaceCommitPlayed : CWorkspaceCommitPaused)?.Invoke(position);
    }

    public void CWorkspaceMediaClear()
    {
        _cWorkspaceRequest = null;
        CWorkspaceSource = null;
        CWorkspaceIntent = null;
        CWorkspaceMedia = false;
    }

    // The path the user asked for, before a sidecar resolves it to its media.
    public void CWorkspaceRequestSet(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        _cWorkspaceRequest = path;
    }

    // The one ask of load-unless-held: a blank path, or the path held or loading, opens nothing;
    // any other path is answered with CWorkspaceOpened, which the drivers apply as given.
    public void CWorkspaceOpen(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || CWorkspaceSourceMatch(path))
        {
            return;
        }

        CWorkspaceOpened?.Invoke(path);
    }

    // True when the asked path is the one held or loading, so a repeat ask opens nothing.
    private bool CWorkspaceSourceMatch(string path) =>
        string.Equals(_cWorkspaceRequest, path, StringComparison.OrdinalIgnoreCase)
        && (CWorkspaceMedia || CWorkspaceIntent is not null);

    // A new load replaces the pending intent whole, at the start and with the default play state.
    public void CWorkspaceStart(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        CWorkspaceIntent = new CWorkspaceIntent(_cWorkspacePort.LWorkspacePathResolve(path), TimeSpan.Zero, null);
    }

    // A pending load is kept; otherwise the held source loads again at the given place.
    // The path to load is answered with CWorkspaceRestored; false when nothing is pending or held.
    public bool CWorkspaceRestore(TimeSpan position, bool playing)
    {
        if (CWorkspaceIntent is not { } pending)
        {
            if (CWorkspaceSource is not { } source)
            {
                return false;
            }

            pending = new CWorkspaceIntent(_cWorkspacePort.LWorkspacePathResolve(source), position, playing);
            CWorkspaceIntent = pending;
        }

        CWorkspaceRestored?.Invoke(pending.CWorkspaceIntentPath);
        return true;
    }

    // Restore unless pending: a pending load is left to land and nothing is asked again;
    // otherwise as CWorkspaceRestore. False when nothing is pending or held.
    public bool CWorkspaceIdleRestore(TimeSpan position, bool playing) =>
        CWorkspaceIntent is not null || CWorkspaceRestore(position, playing);

    // A probe outcome lands only for the path still pending.
    public bool CWorkspaceIntentMatch(string path) =>
        CWorkspaceIntent is { } pending
        && string.Equals(pending.CWorkspaceIntentPath, path, StringComparison.OrdinalIgnoreCase);

    // Drops the pending load; false when none was pending.
    public bool CWorkspaceCancel()
    {
        if (CWorkspaceIntent is null)
        {
            return false;
        }

        CWorkspaceIntent = null;
        return true;
    }

    // Play, pause and seek are decided here: before the load lands they edit the pending intent,
    // otherwise they are raised for the player to apply now.
    public void CWorkspacePlay(bool playing)
    {
        if (CWorkspaceIntent is { } pending)
        {
            CWorkspaceIntent = pending with { CWorkspaceIntentPlaying = playing };
            return;
        }

        (playing ? CWorkspacePlayed : CWorkspacePaused)?.Invoke();
    }

    public void CWorkspaceSeek(TimeSpan position)
    {
        if (CWorkspaceIntent is not { } pending)
        {
            CWorkspaceSought?.Invoke(position);
            return;
        }

        CWorkspaceIntent = pending with { CWorkspaceIntentPosition = position };
        CWorkspaceIntentSought?.Invoke(position);
    }

    public (bool CWorkspaceLoadCancelled, bool CWorkspaceMediaClosed) CWorkspaceRemovalRead(
        IReadOnlyCollection<string> removed)
    {
        ArgumentNullException.ThrowIfNull(removed);
        return _cWorkspacePort.LWorkspaceRemovalRead(
            removed, CWorkspaceIntent?.CWorkspaceIntentPath, CWorkspaceSource);
    }

    public (bool CWorkspaceMediaClosed, bool CWorkspaceLoadCancelled, bool CWorkspaceMediaCleared) CWorkspaceClearRead(
        IReadOnlyCollection<string> kept)
    {
        ArgumentNullException.ThrowIfNull(kept);
        return _cWorkspacePort.LWorkspaceClearRead(
            kept, CWorkspaceSource, CWorkspaceIntent?.CWorkspaceIntentPath, CWorkspaceLoaded);
    }
}
