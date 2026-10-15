namespace Cadroue.ShellEngine;

public interface LPlacementPort
{
    IEnumerable<(string, double, double, double, double)> LPlacementPreferenceRead(
        IReadOnlyList<(string, string)> lEntries);

    void LPlacementUnreadableRecord(string lReason);

    void LPlacementBlockedRecord(string lKey);

    void LPlacementUnsavedRecord(string lKey);

    void LPlacementCarriedRecord(string lKey);

    void LPlacementUncarriedRecord(Exception lFailure);
}
