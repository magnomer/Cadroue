using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cadroue.Core;

public static class LCustody
{
    private static readonly object lCustodyGate = new();
    private static IntPtr lCustodyJob = IntPtr.Zero;
    private static bool lCustodyUnavailable;

    public static void LCustodyAttach(Process lCustodyProcess)
    {
        if (lCustodyUnavailable || !OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            IntPtr lCustodyHandle = LCustodyJobRead();
            if (lCustodyHandle == IntPtr.Zero)
            {
                return;
            }

            if (!AssignProcessToJobObject(lCustodyHandle, lCustodyProcess.Handle))
            {
                _ = Marshal.GetLastWin32Error();
            }
        }
        catch (Exception lCustodyException)
            when (lCustodyException is InvalidOperationException or Win32Exception)
        {
        }
    }

    public static void LCustodyPriorityApply(Process lCustodyProcess)
    {
        try
        {
            lCustodyProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
            if (OperatingSystem.IsWindows())
            {
                int lCustodyLevel = LCustodyPriorityLow;
                _ = NtSetInformationProcess(
                    lCustodyProcess.Handle, LCustodyPriorityClass, ref lCustodyLevel, sizeof(int));
            }
        }
        catch (Exception lCustodyException)
            when (lCustodyException is InvalidOperationException or Win32Exception)
        {
        }
    }

    private static IntPtr LCustodyJobRead()
    {
        if (lCustodyJob != IntPtr.Zero)
        {
            return lCustodyJob;
        }

        lock (lCustodyGate)
        {
            if (lCustodyJob != IntPtr.Zero || lCustodyUnavailable)
            {
                return lCustodyJob;
            }

            IntPtr lCustodyHandle = CreateJobObject(IntPtr.Zero, null);
            if (lCustodyHandle == IntPtr.Zero || !LCustodyLimitApply(lCustodyHandle))
            {
                lCustodyUnavailable = true;
                if (lCustodyHandle != IntPtr.Zero)
                {
                    CloseHandle(lCustodyHandle);
                }

                return IntPtr.Zero;
            }

            lCustodyJob = lCustodyHandle;
            return lCustodyJob;
        }
    }

    private static bool LCustodyLimitApply(IntPtr lCustodyHandle)
    {
        var lCustodyLimit = new LCustodyExtendedLimit
        {
            LCustodyBasic = new LCustodyBasicLimit
            {
                LCustodyLimitFlags = LCustodyKillFlag
            }
        };

        int lCustodyLength = Marshal.SizeOf<LCustodyExtendedLimit>();
        IntPtr lCustodyBuffer = Marshal.AllocHGlobal(lCustodyLength);
        try
        {
            Marshal.StructureToPtr(lCustodyLimit, lCustodyBuffer, false);
            return SetInformationJobObject(
                lCustodyHandle,
                LCustodyInfoClass,
                lCustodyBuffer,
                (uint)lCustodyLength);
        }
        finally
        {
            Marshal.FreeHGlobal(lCustodyBuffer);
        }
    }

    private const int LCustodyInfoClass = 9;
    private const uint LCustodyKillFlag = 0x2000;
    private const int LCustodyPriorityClass = 33;
    private const int LCustodyPriorityLow = 0;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(
        IntPtr hJob, int jobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("ntdll.dll")]
    private static extern int NtSetInformationProcess(
        IntPtr processHandle, int processInformationClass, ref int processInformation, int processInformationLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct LCustodyBasicLimit
    {
        public long LCustodyProcessTime;
        public long LCustodyJobTime;
        public uint LCustodyLimitFlags;
        public UIntPtr LCustodyMinimum;
        public UIntPtr LCustodyMaximum;
        public uint LCustodyProcessCount;
        public UIntPtr LCustodyAffinity;
        public uint LCustodyPriority;
        public uint LCustodyScheduling;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LCustodyIoCounters
    {
        public ulong LCustodyReadOps;
        public ulong LCustodyWriteOps;
        public ulong LCustodyOtherOps;
        public ulong LCustodyReadBytes;
        public ulong LCustodyWriteBytes;
        public ulong LCustodyOtherBytes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LCustodyExtendedLimit
    {
        public LCustodyBasicLimit LCustodyBasic;
        public LCustodyIoCounters LCustodyIoInfo;
        public UIntPtr LCustodyProcessMemory;
        public UIntPtr LCustodyJobMemory;
        public UIntPtr LCustodyPeakProcess;
        public UIntPtr LCustodyPeakJob;
    }
}
