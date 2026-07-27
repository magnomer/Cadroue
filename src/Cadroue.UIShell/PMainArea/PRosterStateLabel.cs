using System.Globalization;
using System.Windows.Data;
using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

/// <summary>Renders an <see cref="LWorkState"/> as the label shown in the job list.</summary>
public sealed class PRosterStateLabel : IValueConverter
{
    internal static string PRosterStateFormat(LWorkState pWorkState) => pWorkState switch
    {
        LWorkState.LWorkStatePending => "Pending",
        LWorkState.LWorkStateRunning => "Running",
        LWorkState.LWorkStateDone => "Done",
        LWorkState.LWorkStateFailed => "Failed",
        LWorkState.LWorkStateCancelled => "Cancelled",
        _ => pWorkState.ToString()
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is LWorkState pWorkState ? PRosterStateFormat(pWorkState) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
