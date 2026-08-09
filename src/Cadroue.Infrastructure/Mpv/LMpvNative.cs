using System.Runtime.InteropServices;

namespace Cadroue.Infrastructure;

internal static class LMpvNative
{
    internal const string LMpvLibraryName = "libmpv-2";

    [DllImport(LMpvLibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint mpv_create();

    [DllImport(LMpvLibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mpv_initialize(nint lHandle);

    [DllImport(LMpvLibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mpv_set_option_string(
        nint lHandle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string lName,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string lData);

    [DllImport(LMpvLibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mpv_command(nint lHandle, nint lArguments);

    [DllImport(LMpvLibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mpv_set_property_string(
        nint lHandle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string lName,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string lData);

    [DllImport(LMpvLibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint mpv_wait_event(nint lHandle, double lTimeout);

    [DllImport(LMpvLibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void mpv_terminate_destroy(nint lHandle);

    [DllImport(LMpvLibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    internal static extern string mpv_error_string(int lError);
}
