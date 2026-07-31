using System.Security.Cryptography;
using System.Text;

namespace Cadroue.Media;

public sealed record LKeyframeSourceIdentity
{
    private const int LKeyframeHashSize = 1024 * 1024;

    private LKeyframeSourceIdentity(
        string sourcePath,
        long sourceLength,
        long sourceLastWriteUtcTicks,
        long sourceDurationMilliseconds,
        string sourcePartialHash)
    {
        LKeyframeSourcePath = sourcePath;
        LKeyframeSourceLength = sourceLength;
        LKeyframeWriteTicks = sourceLastWriteUtcTicks;
        LKeyframeSourceDuration = sourceDurationMilliseconds;
        LKeyframePartialHash = sourcePartialHash;
        LKeyframeCacheKey = LKeyframeKeyCreate();
    }

    public string LKeyframeSourcePath { get; }

    public long LKeyframeSourceLength { get; }

    public long LKeyframeWriteTicks { get; }

    public long LKeyframeSourceDuration { get; }

    public string LKeyframePartialHash { get; }

    public string LKeyframeCacheKey { get; }

    public static LKeyframeSourceIdentity LKeyframeIdentityCreate(string sourcePath, TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        }

        string fullPath = Path.GetFullPath(sourcePath);
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("Source file does not exist.", fullPath);
        }

        return new LKeyframeSourceIdentity(
            fullPath,
            fileInfo.Length,
            fileInfo.LastWriteTimeUtc.Ticks,
            (long)Math.Round(duration.TotalMilliseconds),
            LKeyframeHashCreate(fileInfo));
    }

    private static string LKeyframeHashCreate(FileInfo fileInfo)
    {
        using var sha256 = SHA256.Create();
        using var stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        byte[] buffer = new byte[LKeyframeHashSize];

        int firstRead = stream.Read(buffer, 0, buffer.Length);
        if (firstRead > 0)
        {
            sha256.TransformBlock(buffer, 0, firstRead, null, 0);
        }

        if (stream.Length > LKeyframeHashSize)
        {
            stream.Position = Math.Max(0, stream.Length - LKeyframeHashSize);
            int lastRead = stream.Read(buffer, 0, buffer.Length);
            if (lastRead > 0)
            {
                sha256.TransformBlock(buffer, 0, lastRead, null, 0);
            }
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha256.Hash ?? Array.Empty<byte>());
    }

    private string LKeyframeKeyCreate()
    {
        string rawKey = string.Join(
            "|",
            LKeyframeSourcePath.ToUpperInvariant(),
            LKeyframeSourceLength,
            LKeyframeWriteTicks,
            LKeyframeSourceDuration,
            LKeyframePartialHash);
        byte[] rawBytes = Encoding.UTF8.GetBytes(rawKey);
        return Convert.ToHexString(SHA256.HashData(rawBytes));
    }
}
