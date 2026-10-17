using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cadroue.Core;

public enum LCustodyFamily
{
    LCustodyFamilyBackground,
    LCustodyFamilyEncode,
    LCustodyFamilyKeyframe,
    LCustodyFamilyWaveform
}

public static class LCustody
{
    private static readonly string[] lCustodyTokens = ["Low", "Normal", "High"];

    private static readonly ProcessPriorityClass[] lCustodyClasses =
        [ProcessPriorityClass.BelowNormal, ProcessPriorityClass.Normal, ProcessPriorityClass.AboveNormal];

    private static readonly int[] lCustodyDiskLevels = [0, 2, 2];

    private static readonly int[] lCustodyFamilyLevels = [0, 0, 1, 0];

    public static int LCustodyLevelResolve(string lCustodyToken, int lCustodyFallback)
    {
        int lCustodyIndex = Array.IndexOf(lCustodyTokens, lCustodyToken);
        return lCustodyIndex < 0 ? lCustodyFallback : lCustodyIndex;
    }

    public static string LCustodyLevelFormat(int lCustodyLevel) =>
        lCustodyTokens[Math.Clamp(lCustodyLevel, 0, lCustodyTokens.Length - 1)];

    public static void LCustodyPreferenceApply(LPreferenceState lCustodyPreference)
    {
        foreach (LCustodyFamily lCustodyFamily in Enum.GetValues<LCustodyFamily>())
        {
            lCustodyFamilyLevels[(int)lCustodyFamily] = lCustodyPreference.LPreferenceLevelRead(lCustodyFamily);
        }
    }

    public static int LCustodyLevelRead(LCustodyFamily lCustodyFamily) =>
        lCustodyFamilyLevels[(int)lCustodyFamily];

    public static void LCustodyPriorityApply(Process lCustodyProcess, int lCustodyLevel)
    {
        lCustodyLevel = Math.Clamp(lCustodyLevel, 0, lCustodyClasses.Length - 1);
        try
        {
            lCustodyProcess.PriorityClass = lCustodyClasses[lCustodyLevel];
            if (OperatingSystem.IsWindows())
            {
                int lCustodyDisk = lCustodyDiskLevels[lCustodyLevel];
                _ = NtSetInformationProcess(
                    lCustodyProcess.Handle, LCustodyPriorityClass, ref lCustodyDisk, sizeof(int));
            }
        }
        catch (Exception lCustodyException)
            when (lCustodyException is InvalidOperationException or Win32Exception)
        {
        }
    }

    private const int LCustodyPriorityClass = 33;

    [DllImport("ntdll.dll")]
    private static extern int NtSetInformationProcess(
        IntPtr processHandle, int processInformationClass, ref int processInformation, int processInformationLength);
}
