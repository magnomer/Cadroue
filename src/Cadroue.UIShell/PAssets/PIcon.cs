using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.IO;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace Cadroue.UIShell.PAssets;

public static class PIcon
{
    private static readonly WpfDrawingSettings pIconSettings = new()
    {
        IncludeRuntime = false,
        TextAsGeometry = false,
        IgnoreRootViewbox = false,
        EnsureViewboxSize = true,
        EnsureViewboxPosition = true
    };

    public static ImageSource PIconRead(string pIconPath)
    {
        return PIconRead(pIconPath, null);
    }

    public static ImageSource PIconRead(string pIconPath, Brush? pTintBrush)
    {
        Uri pIconUri = PIconUriCreate(pIconPath);
        if (pIconPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return PIconSvgRead(pIconUri, pTintBrush);
        }

        var pIconBitmap = new BitmapImage(pIconUri);
        pIconBitmap.Freeze();
        return pIconBitmap;
    }

    private static ImageSource PIconSvgRead(Uri pIconUri, Brush? pTintBrush)
    {
        using Stream pIconStream = PIconStreamRead(pIconUri);
        using var pIconReader = new FileSvgReader(pIconSettings);
        DrawingGroup? pIconDrawing = pIconReader.Read(pIconStream);
        if (pIconDrawing is null)
        {
            throw new InvalidOperationException($"Icon asset could not be rendered: {pIconUri}");
        }

        if (pTintBrush is not null)
        {
            pIconDrawing = PIconTintApply(pIconDrawing, pTintBrush);
        }

        var pIconImage = new DrawingImage(pIconDrawing);
        if (pIconImage.CanFreeze)
        {
            pIconImage.Freeze();
        }

        return pIconImage;
    }

    private static Uri PIconUriCreate(string pIconPath) =>
        new("pack://application:,,,/" + pIconPath.TrimStart('/'), UriKind.Absolute);

    private static Stream PIconStreamRead(Uri pIconUri)
    {
        StreamResourceInfo? pIconResource = System.Windows.Application.GetResourceStream(pIconUri);
        if (pIconResource is null)
        {
            throw new InvalidOperationException($"Icon asset was not found: {pIconUri}");
        }

        return pIconResource.Stream;
    }

    private static DrawingGroup PIconTintApply(DrawingGroup pIconDrawing, Brush pTintBrush)
    {
        var pClone = pIconDrawing.Clone();
        var pBrush = pTintBrush.Clone();
        if (pBrush.CanFreeze)
        {
            pBrush.Freeze();
        }

        PIconDrawingApply(pClone, pBrush);
        if (pClone.CanFreeze)
        {
            pClone.Freeze();
        }

        return pClone;
    }

    private static void PIconDrawingApply(Drawing pDrawing, Brush pTintBrush)
    {
        switch (pDrawing)
        {
            case DrawingGroup pGroup:
                foreach (Drawing pChild in pGroup.Children)
                {
                    PIconDrawingApply(pChild, pTintBrush);
                }
                break;
            case GeometryDrawing pGeometry:
                if (PIconBrushCheck(pGeometry.Brush))
                {
                    pGeometry.Brush = pTintBrush;
                }

                if (pGeometry.Pen is not null && PIconBrushCheck(pGeometry.Pen.Brush))
                {
                    pGeometry.Pen = pGeometry.Pen.Clone();
                    pGeometry.Pen.Brush = pTintBrush;
                    if (pGeometry.Pen.CanFreeze)
                    {
                        pGeometry.Pen.Freeze();
                    }
                }
                break;
            case GlyphRunDrawing pGlyph:
                pGlyph.ForegroundBrush = pTintBrush;
                break;
        }
    }

    private static bool PIconBrushCheck(Brush? pBrush)
    {
        if (pBrush is null || pBrush.Opacity <= 0)
        {
            return false;
        }

        return pBrush switch
        {
            SolidColorBrush pSolidBrush => pSolidBrush.Color.A > 0,
            GradientBrush pGradientBrush => pGradientBrush.GradientStops.Any(pStop => pStop.Color.A > 0),
            _ => true
        };
    }
}
