using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Collections.Concurrent;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace Cadroue.UIVeneer.PAsset;

public static class PIcon
{
    private static readonly ConcurrentDictionary<string, ImageSource> pIconCache =
        new(StringComparer.Ordinal);

    private static readonly WpfDrawingSettings pIconSettings = new()
    {
        IncludeRuntime = false,
        TextAsGeometry = false,
        IgnoreRootViewbox = false,
        EnsureViewboxSize = true,
        EnsureViewboxPosition = true
    };

    private static readonly IReadOnlyDictionary<string, Func<Uri, Brush?, ImageSource>> PIconReaders =
        new Dictionary<string, Func<Uri, Brush?, ImageSource>>(StringComparer.OrdinalIgnoreCase)
        {
            [".svg"] = PIconSvgRead,
            [".png"] = PIconBitmapRead,
            [".ico"] = PIconBitmapRead,
        };

    private static readonly IReadOnlyDictionary<bool, Func<DrawingGroup, Brush?, DrawingGroup>> PIconTints =
        new Dictionary<bool, Func<DrawingGroup, Brush?, DrawingGroup>>
        {
            [true] = PIconDrawingRead,
            [false] = PIconTintApply,
        };

    private static readonly IReadOnlyDictionary<Type, Action<Drawing, Brush>> PIconPainters =
        new Dictionary<Type, Action<Drawing, Brush>>
        {
            [typeof(DrawingGroup)] = PIconGroupApply,
            [typeof(GeometryDrawing)] = PIconGeometryApply,
            [typeof(GlyphRunDrawing)] = PIconGlyphApply,
        };

    public static ImageSource PIconRead(string pIconPath)
    {
        return PIconRead(pIconPath, null);
    }

    public static ImageSource PIconRead(string pIconPath, Brush? pTintBrush)
    {
        Uri pIconUri = PIconUriCreate(pIconPath);
        string pIconCacheKey = $"{pIconUri.AbsoluteUri}|{pTintBrush}";
        return pIconCache.GetOrAdd(pIconCacheKey, _ => PIconCreate(pIconPath, pIconUri, pTintBrush));
    }

    private static ImageSource PIconCreate(string pIconPath, Uri pIconUri, Brush? pTintBrush) =>
        PIconReaders[System.IO.Path.GetExtension(pIconPath)](pIconUri, pTintBrush);

    private static ImageSource PIconBitmapRead(Uri pIconUri, Brush? pTintBrush)
    {
        var pIconBitmap = new BitmapImage(pIconUri);
        pIconBitmap.Freeze();
        return pIconBitmap;
    }

    private static ImageSource PIconSvgRead(Uri pIconUri, Brush? pTintBrush)
    {
        using var pIconStream = PIconStreamRead(pIconUri);
        using var pIconReader = new FileSvgReader(pIconSettings);
        DrawingGroup pIconDrawing = pIconReader.Read(pIconStream)!;
        var pIconImage = new DrawingImage(PIconTints[ReferenceEquals(pTintBrush, null)](pIconDrawing, pTintBrush));
        pIconImage.Freeze();
        return pIconImage;
    }

    private static Uri PIconUriCreate(string pIconPath) =>
        new(string.Concat("pack://application:,,,/", pIconPath.TrimStart('/')), UriKind.Absolute);

    private static System.IO.Stream PIconStreamRead(Uri pIconUri) =>
        System.Windows.Application.GetResourceStream(pIconUri)!.Stream;

    private static DrawingGroup PIconDrawingRead(DrawingGroup pIconDrawing, Brush? pTintBrush) => pIconDrawing;

    private static DrawingGroup PIconTintApply(DrawingGroup pIconDrawing, Brush? pTintBrush)
    {
        var pClone = pIconDrawing.Clone();
        var pBrush = pTintBrush!.Clone();
        pBrush.Freeze();
        PIconDrawingApply(pClone, pBrush);
        pClone.Freeze();
        return pClone;
    }

    private static void PIconDrawingApply(Drawing pDrawing, Brush pTintBrush) =>
        PIconPainters.GetValueOrDefault(pDrawing.GetType(), PIconOtherApply)(pDrawing, pTintBrush);

    private static void PIconOtherApply(Drawing pDrawing, Brush pTintBrush)
    {
    }

    private static void PIconGroupApply(Drawing pDrawing, Brush pTintBrush) =>
        ((DrawingGroup)pDrawing).Children.ToList().ForEach(pChild => PIconDrawingApply(pChild, pTintBrush));

    private static void PIconGeometryApply(Drawing pDrawing, Brush pTintBrush)
    {
        var pGeometry = (GeometryDrawing)pDrawing;
        pGeometry.Brush = PIconBrushResolve(pGeometry.Brush, pTintBrush);
        pGeometry.Pen = PIconPenResolve(pGeometry.Pen, pTintBrush);
    }

    private static void PIconGlyphApply(Drawing pDrawing, Brush pTintBrush) =>
        ((GlyphRunDrawing)pDrawing).ForegroundBrush = pTintBrush;

    private static Brush? PIconBrushResolve(Brush? pBrush, Brush pTintBrush) =>
        PIconBrushCheck(pBrush) ? pTintBrush : pBrush;

    private static Pen? PIconPenResolve(Pen? pPen, Brush pTintBrush)
    {
        if (pPen is null || !PIconBrushCheck(pPen.Brush))
        {
            return pPen;
        }

        Pen pTinted = pPen.Clone();
        pTinted.Brush = pTintBrush;
        pTinted.Freeze();
        return pTinted;
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
