using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Cadroue.UIDeportment.Capsule;

internal static class LCapsuleFile
{
    private const string LCapsuleFileName = "placement.json";

    private static readonly JsonSerializerOptions LCapsuleFileOptions = new()
    {
        WriteIndented = true,
    };

    private static string LCapsulePathRead(string? root) => Path.Combine(root ?? string.Empty, LCapsuleFileName);

    internal static void LCapsuleFileSave(string? root, Dictionary<string, LPlacement> placements)
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

            File.WriteAllText(pending, JsonSerializer.Serialize(placements, LCapsuleFileOptions));
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

    internal static IReadOnlyDictionary<string, LPlacement> LCapsuleFileRead(string? root)
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
