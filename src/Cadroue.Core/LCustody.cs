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
        var lCustodyLimit = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            }
        };

        int lCustodyLength = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        IntPtr lCustodyBuffer = Marshal.AllocHGlobal(lCustodyLength);
        try
        {
            Marshal.StructureToPtr(lCustodyLimit, lCustodyBuffer, false);
            return SetInformationJobObject(
                lCustodyHandle,
                JobObjectExtendedLimitInformation,
                lCustodyBuffer,
                (uint)lCustodyLength);
        }
        finally
        {
            Marshal.FreeHGlobal(lCustodyBuffer);
        }
    }

    private const int JobObjectExtendedLimitInformation = 9;
    private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(
        IntPtr hJob, int jobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }
}
