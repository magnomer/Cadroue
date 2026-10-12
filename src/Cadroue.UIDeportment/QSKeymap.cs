using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QSKeymapChord
{
    public QSKeymapChord(string lToken, string lGesture)
    {
        QSKeymapChordToken = lToken;
        QSKeymapChordGesture = lGesture;
    }

    public string QSKeymapChordToken { get; }

    public string QSKeymapChordGesture { get; internal set; }

    public string QSKeymapChordPending { get; internal set; } = string.Empty;

    public bool QSKeymapChordActive { get; internal set; }
}

public sealed class QSKeymap
{
    private readonly Dictionary<string, QSKeymapChord> qsKeymapChords = new(StringComparer.Ordinal);

    public event Action<QSKeymapChord>? QSKeymapChordChanged;

    public QSKeymap()
        : this(LBinding.LBindingCurrent)
    {
    }

    public QSKeymap(List<LBindingEntry>? lRecords)
    {
        List<LBindingEntry> lDraft = LBinding.LBindingNormalize(lRecords);
        foreach (LBindingCommand lCommand in LBinding.LBindingCatalogRead())
        {
            string lToken = lCommand.LBindingCommandToken;
            qsKeymapChords[lToken] = new QSKeymapChord(lToken, LBinding.LBindingGestureRead(lDraft, lToken));
        }
    }

    public IReadOnlyCollection<QSKeymapChord> QSKeymapChords => qsKeymapChords.Values;

    public QSKeymapChord QSKeymapChordRead(string lToken) => qsKeymapChords[lToken];

    public void QSKeymapChordSet(QSKeymapChord lChord, string lGesture)
    {
        lChord.QSKeymapChordGesture = lGesture;
        lChord.QSKeymapChordPending = string.Empty;
        lChord.QSKeymapChordActive = false;
        QSKeymapChordChanged?.Invoke(lChord);
    }

    public void QSKeymapChordStart(QSKeymapChord lChord)
    {
        lChord.QSKeymapChordActive = true;
        lChord.QSKeymapChordPending = string.Empty;
        QSKeymapChordChanged?.Invoke(lChord);
    }

    public void QSKeymapPendingSet(QSKeymapChord lChord, string lGesture)
    {
        if (!lChord.QSKeymapChordActive || lGesture.Length == 0)
        {
            return;
        }

        lChord.QSKeymapChordPending = lGesture;
        QSKeymapChordChanged?.Invoke(lChord);
    }

    public void QSKeymapChordCancel(QSKeymapChord lChord)
    {
        lChord.QSKeymapChordActive = false;
        lChord.QSKeymapChordPending = string.Empty;
        QSKeymapChordChanged?.Invoke(lChord);
    }

    public void QSKeymapChordCommit(QSKeymapChord lChord)
    {
        lChord.QSKeymapChordActive = false;
        string lPending = lChord.QSKeymapChordPending;
        if (lPending.Length == 0)
        {
            QSKeymapChordChanged?.Invoke(lChord);
            return;
        }

        lChord.QSKeymapChordGesture = lPending;
        lChord.QSKeymapChordPending = string.Empty;
        QSKeymapChordChanged?.Invoke(lChord);
        foreach (QSKeymapChord lOther in qsKeymapChords.Values)
        {
            if (!ReferenceEquals(lOther, lChord)
                && string.Equals(lOther.QSKeymapChordGesture, lPending, StringComparison.OrdinalIgnoreCase))
            {
                QSKeymapChordSet(lOther, string.Empty);
            }
        }
    }

    public void QSKeymapDefaultApply()
    {
        foreach (QSKeymapChord lChord in qsKeymapChords.Values)
        {
            QSKeymapChordSet(lChord, LBinding.LBindingDefaultRead(lChord.QSKeymapChordToken));
        }
    }

    public List<LBindingEntry> QSKeymapRecordsRead() =>
        qsKeymapChords.Values
            .Select(lChord => new LBindingEntry
            {
                LBindingRecordToken = lChord.QSKeymapChordToken,
                LBindingRecordGesture = lChord.QSKeymapChordGesture
            })
            .ToList();

    public void QSKeymapApply() => LBinding.LBindingSet(QSKeymapRecordsRead());
}
