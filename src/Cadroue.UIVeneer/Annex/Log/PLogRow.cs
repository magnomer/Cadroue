using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PLogRow : INotifyPropertyChanged
{
    internal PLogRow(QLogRow lRow)
    {
        QLogRow = lRow;
        PLogBadge pLogBadge = PLogBadge.PLogBadgeRead(lRow.QLogRowKey);
        PLogBadgeFill = pLogBadge.PLogBadgeFill;
        PLogBadgeText = pLogBadge.PLogBadgeInk;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal QLogRow QLogRow { get; }

    public string PLogRowTime => QLogRow.QLogRowTime;

    public string PLogRowKind => QLogRow.QLogRowKind;

    public string PLogRowSummary => QLogRow.QLogRowSummary;

    public string PLogRowDetail => QLogRow.QLogRowDetail;

    public string PLogRowSpan => QLogRow.QLogRowSpan;

    public Brush PLogBadgeFill { get; }

    public Brush PLogBadgeText { get; }

    internal void PLogRowUpdate()
    {
        PLogChangeRaise(nameof(PLogDetailShown));
        PLogChangeRaise(nameof(PLogChipText));
    }

    public Visibility PLogDetailShown => PLook.PLookVisible[QLogRow.QLogRowExpanded];

    public Visibility PLogChipShown => PLook.PLookVisible[QLogRow.QLogRowDetailed];

    public string PLogChipText => QLogRow.QLogRowChip;

    private void PLogChangeRaise(string pLogPropertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pLogPropertyName));
}
