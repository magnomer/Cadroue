using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Cadroue.UIDeportment.Capsule;

public sealed class LCapsule
{
    private const string LCapsuleName = "placement.json";

    private static readonly JsonSerializerOptions LCapsuleOptions = new()
    {
        WriteIndented = true,
    };

    private static string LCapsulePathRead(string? root) => Path.Combine(root ?? string.Empty, LCapsuleName);

    public LPlacement? LCapsuleFind(string? root, string key) =>
        LCapsuleRead(root).TryGetValue(key, out LPlacement? placement) ? placement : null;

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

        Dictionary<string, LPlacement> placements = LCapsuleCatalogCreate(LCapsuleRead(root), key, placement);
        LCapsuleFileSave(root, placements);
    }

    private static Dictionary<string, LPlacement> LCapsuleCatalogCreate(
        IReadOnlyDictionary<string, LPlacement> current,
        string key,
        LPlacement placement) =>
        new(current, StringComparer.Ordinal)
        {
            [key] = placement
        };

    private static void LCapsuleFileSave(string? root, Dictionary<string, LPlacement> placements)
    {
        string path = LCapsulePathRead(root);
        string pending = path + ".tmp";
        try
        {
            string? folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(pending, JsonSerializer.Serialize(placements, LCapsuleOptions));
            if (File.Exists(path))
            {
                File.Replace(pending, path, null);
            }
            else
            {
                File.Move(pending, path);
            }
        }
        catch
        {
            LCapsulePendingDelete(pending);
            throw;
        }
    }

    private IReadOnlyDictionary<string, LPlacement> LCapsuleRead(string? root)
    {
        string path = LCapsulePathRead(root);
        if (string.IsNullOrWhiteSpace(root) || !File.Exists(path))
        {
            return LCapsuleEmptyCreate();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, LPlacement>>(File.ReadAllText(path))
                ?? throw new JsonException($"Placement file holds no catalogue: {path}");
        }
        catch (Exception exception)
        {
            LCapsuleQuarantineRun(path);
            throw new InvalidDataException(path, exception);
        }
    }

    private static Dictionary<string, LPlacement> LCapsuleEmptyCreate() => new(StringComparer.Ordinal);

    private static void LCapsuleQuarantineRun(string path)
    {
        try
        {
            string corrupt = path + ".corrupt";
            if (File.Exists(corrupt))
            {
                File.Delete(corrupt);
            }

            File.Move(path, corrupt);
        }
        catch
        {
        }
    }

    private static void LCapsulePendingDelete(string pending)
    {
        try
        {
            if (File.Exists(pending))
            {
                File.Delete(pending);
            }
        }
        catch
        {
        }
    }
}
