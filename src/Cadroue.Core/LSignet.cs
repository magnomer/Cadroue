namespace Cadroue.Core;

public static class LSignet
{
    private static readonly object lSignetGate = new();
    private static Guid? lSignetCurrent;

    public static Guid LSignetCurrent
    {
        get
        {
            if (lSignetCurrent is Guid lSignetExisting)
            {
                return lSignetExisting;
            }

            lock (lSignetGate)
            {
                lSignetCurrent ??= Guid.NewGuid();
                return lSignetCurrent.Value;
            }
        }
    }
}
