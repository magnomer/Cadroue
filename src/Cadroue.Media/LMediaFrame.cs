using System.Globalization;
using System.IO;
using System.Threading;

using Cadroue.Core;

namespace Cadroue.Media;

public sealed record LMediaFrame(int LMediaFrameWidth, int LMediaFrameHeight, byte[] LMediaFramePixels);

public static partial class LMedia
{
    public static Task<LMediaFrame?> LMediaFrameStart(
        string sourcePath,
        TimeSpan position,
        int width,
        int height) =>
        Task.Run(() => LMediaFrameRead(sourcePath, position, width, height));

    public static async Task<LMediaFrame?> LMediaFrameRead(
        string sourcePath,
        TimeSpan position,
        int width,
        int height,
        CancellationToken lMediaToken = default)
    {
        lMediaToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sourcePath)
            || !File.Exists(sourcePath)
            || width <= 0
            || height <= 0)
        {
            return null;
        }

        long lMediaExpectedLong = (long)width * height * 4;
        if (lMediaExpectedLong > int.MaxValue)
        {
            return null;
        }

        double lMediaSeconds = Math.Max(0, position.TotalSeconds);
        string[] lMediaArguments =
        [
            "-hide_banner", "-nostats",
            "-ss", lMediaSeconds.ToString("0.###", CultureInfo.InvariantCulture),
            "-i", sourcePath,
            "-map", "0:v:0",
            "-frames:v", "1",
            "-pix_fmt", "rgba",
            "-f", "rawvideo",
            "-"
        ];

        byte[] lMediaPixels = new byte[(int)lMediaExpectedLong];
        int lMediaRead = 0;
        try
        {
            var lMediaEmployer = new LEmployer(LTool.LToolFfmpegRead())
            {
                LEmployerFamily = LCustodyFamily.LCustodyFamilyBackground
            };
            LEmployerResult lMediaResult = await lMediaEmployer.LEmployerStreamRun(
                lMediaArguments,
                lMediaToken,
                async (lMediaStream, lMediaCancel) =>
                {
                    int lMediaChunk;
                    while (lMediaRead < lMediaPixels.Length
                        && (lMediaChunk = await lMediaStream
                            .ReadAsync(lMediaPixels.AsMemory(lMediaRead), lMediaCancel)
                            .ConfigureAwait(false)) > 0)
                    {
                        lMediaRead += lMediaChunk;
                    }
                }).ConfigureAwait(false);
            return lMediaResult.LEmployerExit == 0 && lMediaRead == lMediaPixels.Length
                ? new LMediaFrame(width, height, lMediaPixels)
                : null;
        }
        catch (Exception lMediaException) when (
            lMediaException is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return null;
        }
    }
}
