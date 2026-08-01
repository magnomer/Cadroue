using System.Windows;
using System.Windows.Media;
using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

internal static class PRosterTheme
{
    public const double PRosterTitleSize = 12;
    public const double PRosterRowSize = 11;
    public const double PRosterRowHeight = 15;
    public const double PRosterLabelWidth = 92;
    public const double PRosterCorner = 9;
    public const double PRosterButtonSize = 58;
    public const double PRosterIconSize = 24;
    public const double PRosterDisabledOpacity = 0.35;

    public static readonly Thickness PRosterHeaderPadding = new(12, 10, 12, 10);
    public static readonly Thickness PRosterRowPadding = new(12, 7, 12, 7);

    public static readonly Brush PRosterLineBrush = PRosterBrushCreate(0xD9, 0xDE, 0xE7);
    public static readonly Brush PRosterTextBrush = PRosterBrushCreate(0x1D, 0x2A, 0x3D);
    public static readonly Brush PRosterMutedBrush = PRosterBrushCreate(0x62, 0x6F, 0x83);
    public static readonly Brush PRosterTitleBrush = PRosterBrushCreate(0x26, 0x36, 0x4A);
    public static readonly Brush PRosterHeaderBrush = PRosterBrushCreate(0xF3, 0xF5, 0xF8);
    public static readonly Brush PRosterSelectBrush = PRosterBrushCreate(0xEE, 0xF4, 0xFB);
    public static readonly Brush PRosterTrackBrush = PRosterBrushCreate(0xE8, 0xEE, 0xF6);
    public static readonly Brush PRosterStageBrush = PRosterBrushCreate(0xF7, 0xF8, 0xFA);
    public static readonly Brush PRosterTrunkBrush = PRosterBrushCreate(0xB4, 0xBF, 0xCE);

    public static readonly Brush PRosterAccentBrush = PRosterBrushCreate(0x4C, 0x86, 0xF7);

    public static readonly Brush PRosterRunBrush = PRosterBrushCreate(0x3A, 0x8B, 0xE0);
    public static readonly Brush PRosterDoneBrush = PRosterBrushCreate(0x2F, 0x9E, 0x64);
    public static readonly Brush PRosterFailBrush = PRosterBrushCreate(0xD6, 0x45, 0x45);

    public static Brush PRosterStateRead(LWorkState pWorkState) => pWorkState switch
    {
        LWorkState.LWorkStateRunning => PRosterRunBrush,
        LWorkState.LWorkStateDone => PRosterDoneBrush,
        LWorkState.LWorkStateFailed => PRosterFailBrush,
        _ => PRosterMutedBrush
    };

    private static Brush PRosterBrushCreate(byte pRed, byte pGreen, byte pBlue)
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(pRed, pGreen, pBlue));
        pBrush.Freeze();
        return pBrush;
    }
}
