using System;
using System.Collections.Generic;
using System.IO;

namespace Cadroue.UIDeportment.Capsule;

public sealed class LCapsule
{
    public LPlacement? LCapsuleFind(string? root, string key) =>
        LCapsuleFile.LCapsuleFileRead(root).TryGetValue(key, out LPlacement? placement) ? placement : null;

    // The store tells its failures apart: saved answers (true, null), an unreadable catalogue (false, its
    // message), any other failure (false, null).
    public (bool, string?) LCapsuleSave(string? root, string key, LPlacement placement)
    {
        try
        {
            LCapsuleEntrySave(root, key, placement);
            return (true, null);
        }
        catch (InvalidDataException unreadable)
        {
            return (false, unreadable.Message);
        }
        catch (Exception)
        {
            return (false, null);
        }
    }

    private void LCapsuleEntrySave(string? root, string key, LPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        Dictionary<string, LPlacement> placements =
            LCapsuleCatalogCreate(LCapsuleFile.LCapsuleFileRead(root), key, placement);
        LCapsuleFile.LCapsuleFileSave(root, placements);
    }

    private static Dictionary<string, LPlacement> LCapsuleCatalogCreate(
        IReadOnlyDictionary<string, LPlacement> current,
        string key,
        LPlacement placement) =>
        new(current, StringComparer.Ordinal)
        {
            [key] = placement
        };
}
