using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

using Cadroue.Core;

namespace Cadroue.Media;

public sealed record LMediaFrame(int LMediaFrameWidth, int LMediaFrameHeight, byte[] LMediaFramePixels);

public static partial class LMedia
{
    // Decode a single RGBA frame from the stored source at the given position, without
    // subtitles, overlays, rotation metadata, or any preview correction. The output
    // carries raw stored-orientation pixels so callers map display coordinates back
    // through their own transform chain.
    public static LMediaFrame? LMediaFrameRead(
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

        double lMediaSeconds = Math.Max(0, position.TotalSeconds);
        var psi = new ProcessStartInfo(LTool.LToolFfmpegRead())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-hide_banner");
        psi.ArgumentList.Add("-nostats");
        psi.ArgumentList.Add("-noautorotate");
        psi.ArgumentList.Add("-ss");
        psi.ArgumentList.Add(lMediaSeconds.ToString("0.###", CultureInfo.InvariantCulture));
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(sourcePath);
        psi.ArgumentList.Add("-map");
        psi.ArgumentList.Add("0:v:0");
        psi.ArgumentList.Add("-frames:v");
        psi.ArgumentList.Add("1");
        psi.ArgumentList.Add("-pix_fmt");
        psi.ArgumentList.Add("rgba");
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("rawvideo");
        psi.ArgumentList.Add("-");

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                return null;
            }

            LCustody.LCustodyAttach(process);
            using var lMediaBuffer = new MemoryStream();
            Task lMediaCopy = process.StandardOutput.BaseStream.CopyToAsync(lMediaBuffer, lMediaToken);
            Task<string> lMediaError = process.StandardError.ReadToEndAsync(lMediaToken);
            try
            {
                lMediaCopy.GetAwaiter().GetResult();
                process.WaitForExitAsync(lMediaToken).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw;
            }

            _ = lMediaError.GetAwaiter().GetResult();
            if (process.ExitCode != 0)
            {
                return null;
            }

            byte[] lMediaPixels = lMediaBuffer.ToArray();
            long lMediaExpected = (long)width * height * 4;
            if (lMediaPixels.LongLength < lMediaExpected)
            {
                return null;
            }

            if (lMediaPixels.LongLength > lMediaExpected)
            {
                Array.Resize(ref lMediaPixels, (int)lMediaExpected);
            }

            return new LMediaFrame(width, height, lMediaPixels);
        }
        catch (Exception lMediaException) when (
            lMediaException is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return null;
        }
    }
}
