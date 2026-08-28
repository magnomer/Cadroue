using System.Globalization;
using System.Windows.Data;
using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public sealed class PRosterStateLabel : IValueConverter
{
    internal static string PRosterStateFormat(LWorkState pWorkState) => pWorkState switch
    {
        LWorkState.LWorkStatePending => LLocalization.LLocalizationTextRead("Roster.State.Pending"),
        LWorkState.LWorkStateRunning => LLocalization.LLocalizationTextRead("Roster.State.Running"),
        LWorkState.LWorkStateDone => LLocalization.LLocalizationTextRead("Roster.State.Done"),
        LWorkState.LWorkStateFailed => LLocalization.LLocalizationTextRead("Roster.State.Failed"),
        LWorkState.LWorkStateUnresolved => LLocalization.LLocalizationTextRead("Roster.State.Unresolved"),
        LWorkState.LWorkStatePartial => LLocalization.LLocalizationTextRead("Roster.State.Partial"),
        LWorkState.LWorkStateBlocked => LLocalization.LLocalizationTextRead("Roster.State.Blocked"),
        LWorkState.LWorkStateCancelled => LLocalization.LLocalizationTextRead("Roster.State.Cancelled"),
        _ => pWorkState.ToString()
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is LWorkState pWorkState ? PRosterStateFormat(pWorkState) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
