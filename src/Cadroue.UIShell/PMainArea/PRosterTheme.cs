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

    public static readonly Brush PRosterCardBrush = PRosterBrushCreate(0xEA, 0xF2, 0xFC);
    public static readonly Brush PRosterHoverBrush = PRosterBrushCreate(0xDF, 0xEB, 0xFA);
    public static readonly Brush PRosterControlHover = PRosterBrushCreate(0xDC, 0xE9, 0xF9);
    public static readonly Brush PRosterSelectCard = PRosterBrushCreate(0x4C, 0x86, 0xF7);
    public static readonly Brush PRosterSelectText = Brushes.White;
    public static readonly Brush PRosterBodyBrush = Brushes.White;
    public static readonly Brush PRosterSelectBody = PRosterBrushCreate(0xF4, 0xF8, 0xFE);
    public static readonly Brush PRosterCardLine = PRosterBrushCreate(0xCA, 0xD9, 0xEB);
    public static readonly Brush PRosterSelectLine = PRosterBrushCreate(0x3F, 0x73, 0xD8);
    public static readonly Brush PRosterOuterLine = PRosterBrushCreate(0x9F, 0xBE, 0xF2);
    public static readonly Brush PRosterDoneCard = PRosterBrushCreate(0xE7, 0xEB, 0xF0);
    public static readonly Brush PRosterDoneHover = PRosterBrushCreate(0xDD, 0xE2, 0xE8);
    public static readonly Brush PRosterDoneBody = PRosterBrushCreate(0xF5, 0xF6, 0xF8);
    public static readonly Brush PRosterDoneLine = PRosterBrushCreate(0xCD, 0xD3, 0xDC);

    public static readonly Brush PRosterAccentBrush = PRosterBrushCreate(0x4C, 0x86, 0xF7);

    public static readonly Brush PRosterRunBrush = PRosterBrushCreate(0x3A, 0x8B, 0xE0);
    public static readonly Brush PRosterDoneBrush = PRosterBrushCreate(0x2F, 0x9E, 0x64);
    public static readonly Brush PRosterFailBrush = PRosterBrushCreate(0xD6, 0x45, 0x45);
    public static readonly Brush PRosterUnresolvedBrush = PRosterBrushCreate(0xE0, 0x8A, 0x2E);
    public static readonly Brush PRosterPartialBrush = PRosterBrushCreate(0xC7, 0x9A, 0x22);
    public static readonly Brush PRosterBlockedBrush = PRosterBrushCreate(0x7A, 0x5A, 0x9E);

    public static Brush PRosterStateRead(LWorkState pWorkState) => pWorkState switch
    {
        LWorkState.LWorkStateRunning => PRosterRunBrush,
        LWorkState.LWorkStateDone => PRosterDoneBrush,
        LWorkState.LWorkStateFailed => PRosterFailBrush,
        LWorkState.LWorkStateUnresolved => PRosterUnresolvedBrush,
        LWorkState.LWorkStatePartial => PRosterPartialBrush,
        LWorkState.LWorkStateBlocked => PRosterBlockedBrush,
        _ => PRosterMutedBrush
    };

    private static Brush PRosterBrushCreate(byte pRed, byte pGreen, byte pBlue)
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(pRed, pGreen, pBlue));
        pBrush.Freeze();
        return pBrush;
    }
}
