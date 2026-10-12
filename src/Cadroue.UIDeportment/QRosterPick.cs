namespace Cadroue.UIDeportment;

internal sealed record QRosterPick(Guid QRosterPickCurrent, Guid QRosterPickCard, IReadOnlySet<Guid> QRosterPickIds);
