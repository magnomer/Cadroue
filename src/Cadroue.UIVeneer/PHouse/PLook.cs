using System.Collections.Generic;
using System.Windows;

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
}
