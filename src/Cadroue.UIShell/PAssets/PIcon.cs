using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        string pFilePath = PIconFilePathRead(pIconPath);
        if (pIconPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return PIconSvgRead(pFilePath, pTintBrush);
        }

        var pIconBitmap = new BitmapImage(new Uri(pFilePath, UriKind.Absolute));
        pIconBitmap.Freeze();
        return pIconBitmap;
    }

    private static ImageSource PIconSvgRead(string pFilePath, Brush? pTintBrush)
    {
        using var pIconReader = new FileSvgReader(pIconSettings);
        DrawingGroup? pIconDrawing = pIconReader.Read(pFilePath);
        if (pIconDrawing is null)
        {
            throw new InvalidOperationException($"Icon asset could not be rendered: {pFilePath}");
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

    private static string PIconFilePathRead(string pIconPath)
    {
        string pRelativePath = pIconPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        string pFilePath = Path.Combine(AppContext.BaseDirectory, pRelativePath);
        if (!File.Exists(pFilePath))
        {
            throw new InvalidOperationException($"Icon asset was not found: {pIconPath}");
        }

        return pFilePath;
    }

    private static DrawingGroup PIconTintApply(DrawingGroup pIconDrawing, Brush pTintBrush)
    {
        var pClone = pIconDrawing.Clone();
        var pBrush = pTintBrush.Clone();
        if (pBrush.CanFreeze)
        {
            pBrush.Freeze();
        }

        PIconTintApplyToDrawing(pClone, pBrush);
        if (pClone.CanFreeze)
        {
            pClone.Freeze();
        }

        return pClone;
    }

    private static void PIconTintApplyToDrawing(Drawing pDrawing, Brush pTintBrush)
    {
        switch (pDrawing)
        {
            case DrawingGroup pGroup:
                foreach (Drawing pChild in pGroup.Children)
                {
                    PIconTintApplyToDrawing(pChild, pTintBrush);
                }
                break;
            case GeometryDrawing pGeometry:
                if (PIconBrushVisible(pGeometry.Brush))
                {
                    pGeometry.Brush = pTintBrush;
                }

                if (pGeometry.Pen is not null && PIconBrushVisible(pGeometry.Pen.Brush))
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

    private static bool PIconBrushVisible(Brush? pBrush)
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
