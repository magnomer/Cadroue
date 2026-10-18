using Cadroue.Conduct;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

// Paths dropped on the viewer: handed to the tab's list when one listens, else the first file opens here.
public sealed class QViewerDrop
{
    private readonly QViewer qViewer;
    private readonly CUsher qViewerUsher;
    private readonly Func<Action<IReadOnlyList<string>>?> qViewerPathsDrop;

    // The source owns the drop event; this piece reads its listeners at each drop and raises it.
    public QViewerDrop(QViewer lOwner, CUsher lUsher, Func<Action<IReadOnlyList<string>>?> lPathsDrop)
    {
        qViewer = lOwner;
        qViewerUsher = lUsher;
        qViewerPathsDrop = lPathsDrop;
    }

    public QWindowDropEffect QViewerDropResolve(string[]? lPaths, bool lCopyable)
    {
        IReadOnlyList<string> lList = lPaths ?? [];
        if (qViewerPathsDrop() is not null)
        {
            return QWindowDrop.QWindowMediaCheck(qViewerUsher, lList) && lCopyable
                ? QWindowDropEffect.QWindowDropFile
                : QWindowDropEffect.QWindowDropNone;
        }

        string? lSourcePath = QWindowDrop.QWindowFileFind(qViewerUsher, lList);
        if (lSourcePath is null || (LMedia.LMediaAudioCheck(lSourcePath) && !qViewer.QViewerAudioAllowed))
        {
            return QWindowDropEffect.QWindowDropNone;
        }

        return lCopyable ? QWindowDropEffect.QWindowDropFile : QWindowDropEffect.QWindowDropNone;
    }

    public QWindowDropEffect QViewerDropHandle(string[]? lPaths, bool lCopyable)
    {
        QWindowDropEffect lEffect = QViewerDropResolve(lPaths, lCopyable);
        if (lEffect == QWindowDropEffect.QWindowDropNone)
        {
            return lEffect;
        }

        IReadOnlyList<string> lList = lPaths ?? [];
        if (qViewerPathsDrop() is { } lPathsDrop)
        {
            lPathsDrop.Invoke(lList);
            return lEffect;
        }

        string? lSourcePath = QWindowDrop.QWindowFileFind(qViewerUsher, lList);
        if (lSourcePath is null)
        {
            return QWindowDropEffect.QWindowDropNone;
        }

        qViewer.QViewerSource.QViewerSourceOpen(lSourcePath);
        return lEffect;
    }
}
