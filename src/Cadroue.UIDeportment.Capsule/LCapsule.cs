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

    public void LCapsuleSave(string? root, string key, LPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        var placements = new Dictionary<string, LPlacement>(LCapsuleRead(root), StringComparer.Ordinal)
        {
            [key] = placement
        };

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
            return new Dictionary<string, LPlacement>(StringComparer.Ordinal);
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
