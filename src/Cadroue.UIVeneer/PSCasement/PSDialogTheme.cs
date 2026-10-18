using System.Windows.Media;

namespace Cadroue.UIVeneer;

internal readonly record struct PSDialogTheme(Brush PSDialogBandFill, int PSDialogDwmCaption)
{
    internal static readonly PSDialogTheme PSDialogThemeBlue =
        new(PSHeadline.PSHeadlineBandBlue, PSHeadline.PSHeadlineCaptionBlue);
    internal static readonly PSDialogTheme PSDialogThemeRed =
        new(PSHeadline.PSHeadlineBandRed, PSHeadline.PSHeadlineCaptionRed);
    internal static readonly PSDialogTheme PSDialogThemeOrange =
        new(PSHeadline.PSHeadlineBandOrange, PSHeadline.PSHeadlineCaptionOrange);
}
