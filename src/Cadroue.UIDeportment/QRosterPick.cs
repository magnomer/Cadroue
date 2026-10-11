namespace Cadroue.UIDeportment;

internal sealed record QRosterPick(Guid LRosterPickCurrent, Guid LRosterPickCard, IReadOnlySet<Guid> LRosterPickIds);
