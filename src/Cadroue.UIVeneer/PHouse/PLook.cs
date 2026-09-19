using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Cadroue.UIDeportment;

using FlyleafLib;

namespace Cadroue.UIVeneer.PHouse;

public static class PLook
{
    public static readonly IReadOnlyDictionary<bool, Visibility> PLookVisible = new Dictionary<bool, Visibility>
    {
        [true] = Visibility.Visible,
        [false] = Visibility.Collapsed,
    };

    public static readonly IReadOnlyDictionary<bool, Visibility> PLookHidden = new Dictionary<bool, Visibility>
    {
        [true] = Visibility.Visible,
        [false] = Visibility.Hidden,
    };

    public static readonly IReadOnlyDictionary<bool, bool?> PLookChecked = new Dictionary<bool, bool?>
    {
        [true] = true,
        [false] = false,
    };

    public static readonly IReadOnlyDictionary<bool, double> PLookOpacity = new Dictionary<bool, double>
    {
        [true] = 1.0,
        [false] = 0.4,
    };

    public static readonly IReadOnlyDictionary<bool, LogLevel> PLookFlyleafLog = new Dictionary<bool, LogLevel>
    {
        [true] = LogLevel.Debug,
        [false] = LogLevel.Quiet,
    };

    public static readonly IReadOnlyDictionary<int, Cursor?> PLookCursor = new Dictionary<int, Cursor?>
    {
        [LSash.LSashNone] = null,
        [LSash.LSashLeft] = Cursors.SizeWE,
        [LSash.LSashRight] = Cursors.SizeWE,
        [LSash.LSashTop] = Cursors.SizeNS,
        [LSash.LSashBottom] = Cursors.SizeNS,
        [LSash.LSashLeft | LSash.LSashTop] = Cursors.SizeNWSE,
        [LSash.LSashRight | LSash.LSashBottom] = Cursors.SizeNWSE,
        [LSash.LSashLeft | LSash.LSashBottom] = Cursors.SizeNESW,
        [LSash.LSashRight | LSash.LSashTop] = Cursors.SizeNESW,
    };

    public static readonly IReadOnlyDictionary<bool, RenderMode> PLookRender = new Dictionary<bool, RenderMode>
    {
        [true] = RenderMode.SoftwareOnly,
        [false] = RenderMode.Default,
    };

    public static readonly IReadOnlyDictionary<WindowState, bool> PLookNormal = new Dictionary<WindowState, bool>
    {
        [WindowState.Normal] = true,
        [WindowState.Minimized] = false,
        [WindowState.Maximized] = false,
    };

    public static readonly IReadOnlyDictionary<WindowState, bool> PLookMaximized = new Dictionary<WindowState, bool>
    {
        [WindowState.Normal] = false,
        [WindowState.Minimized] = false,
        [WindowState.Maximized] = true,
    };

    public static readonly IReadOnlyDictionary<WindowState, WindowState> PLookUnmaximized =
        new Dictionary<WindowState, WindowState>
        {
            [WindowState.Normal] = WindowState.Normal,
            [WindowState.Minimized] = WindowState.Minimized,
            [WindowState.Maximized] = WindowState.Normal,
        };

    public static readonly IReadOnlyDictionary<WindowStartupLocation, bool> PLookManual =
        new Dictionary<WindowStartupLocation, bool>
        {
            [WindowStartupLocation.Manual] = true,
            [WindowStartupLocation.CenterScreen] = false,
            [WindowStartupLocation.CenterOwner] = false,
        };

    public static readonly IReadOnlyDictionary<WindowState, WindowState> PLookUnminimized =
        new Dictionary<WindowState, WindowState>
        {
            [WindowState.Normal] = WindowState.Normal,
            [WindowState.Minimized] = WindowState.Normal,
            [WindowState.Maximized] = WindowState.Maximized,
        };

    public static readonly IReadOnlyDictionary<LWindowDropEffect, DragDropEffects> PLookDropEffect =
        new Dictionary<LWindowDropEffect, DragDropEffects>
        {
            [LWindowDropEffect.LWindowDropNone] = DragDropEffects.None,
            [LWindowDropEffect.LWindowDropFile] = DragDropEffects.Copy,
        };

    public static readonly IReadOnlyDictionary<bool, DragDropEffects> PLookCopyEffect =
        new Dictionary<bool, DragDropEffects>
        {
            [true] = DragDropEffects.Copy,
            [false] = DragDropEffects.None,
        };

    public static readonly IReadOnlyDictionary<bool, GridLength> PLookRailWidth = new Dictionary<bool, GridLength>
    {
        [true] = new GridLength(PPorch.PRail.PRailWidth),
        [false] = new GridLength(0),
    };
}
