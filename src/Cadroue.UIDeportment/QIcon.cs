using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Collections.Concurrent;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace Cadroue.UIDeportment;

public static class QIcon
{
    private const double QIconLargeThreshold = 60;
    private const string QIconAssetRoot = "/PAsset/";
    private const string QIconSvgExtension = ".svg";

    private static readonly IReadOnlyDictionary<string, bool> qIconKinds =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            [".svg"] = true,
            [".png"] = false,
            [".ico"] = false,
        };

    private static readonly IReadOnlyDictionary<bool, Func<string, double, string?>> qIconResolvers =
        new Dictionary<bool, Func<string, double, string?>>
        {
            [true] = QIconSvgResolve,
            [false] = QIconNoneResolve,
        };

    private static readonly ConcurrentDictionary<string, ImageSource> qIconCache =
        new(StringComparer.Ordinal);

    private static readonly WpfDrawingSettings qIconSettings = new()
    {
        IncludeRuntime = false,
        TextAsGeometry = false,
        IgnoreRootViewbox = false,
        EnsureViewboxSize = true,
        EnsureViewboxPosition = true
    };

    private static readonly IReadOnlyDictionary<bool, Func<Uri, Brush?, ImageSource>> qIconReaders =
        new Dictionary<bool, Func<Uri, Brush?, ImageSource>>
        {
            [true] = QIconSvgRead,
            [false] = QIconBitmapRead,
        };

    private static readonly IReadOnlyDictionary<bool, Func<DrawingGroup, Brush?, DrawingGroup>> qIconTints =
        new Dictionary<bool, Func<DrawingGroup, Brush?, DrawingGroup>>
        {
            [true] = QIconDrawingRead,
            [false] = QIconTintApply,
        };

    private static readonly IReadOnlyDictionary<Type, Action<Drawing, Brush>> qIconPainters =
        new Dictionary<Type, Action<Drawing, Brush>>
        {
            [typeof(DrawingGroup)] = QIconGroupApply,
            [typeof(GeometryDrawing)] = QIconGeometryApply,
            [typeof(GlyphRunDrawing)] = QIconGlyphApply,
        };

    public static ImageSource QIconRead(string qIconPath)
    {
        return QIconRead(qIconPath, 32, null);
    }

    public static ImageSource QIconRead(string qIconPath, Brush? qIconTintBrush)
    {
        return QIconRead(qIconPath, 32, qIconTintBrush);
    }

    public static ImageSource QIconRead(string qIconPath, double qIconWidth)
    {
        return QIconRead(qIconPath, qIconWidth, null);
    }

    public static ImageSource QIconRead(string qIconPath, double qIconWidth, Brush? qIconTintBrush)
    {
        string qIconResolvedPath = QIconPathResolve(qIconPath, qIconWidth);
        Uri qIconUri = QIconUriCreate(qIconResolvedPath);
        string qIconCacheKey = $"{qIconUri.AbsoluteUri}|{qIconTintBrush}";
        return qIconCache.GetOrAdd(qIconCacheKey, _ => QIconCreate(qIconResolvedPath, qIconUri, qIconTintBrush));
    }

    private static bool QIconSvgCheck(string qIconPath) => qIconKinds[QIconExtensionRead(qIconPath)];

    private static string? QIconBitmapResolve(string qIconPath, double qIconWidth)
    {
        bool qIconSvg = qIconPath.EndsWith(QIconSvgExtension, StringComparison.OrdinalIgnoreCase);
        return qIconResolvers[qIconSvg](qIconPath, qIconWidth);
    }

    private static bool QIconPrerenderCheck(string qIconPath) =>
        qIconPath.Contains("/P32/", StringComparison.OrdinalIgnoreCase)
        || qIconPath.Contains("/P256/", StringComparison.OrdinalIgnoreCase);

    private static string QIconExtensionRead(string qIconPath)
    {
        string qIconFile = qIconPath[(qIconPath.Replace('\\', '/').LastIndexOf('/') + 1)..];
        int qIconDot = qIconFile.LastIndexOf('.');
        return qIconDot < 0 ? string.Empty : qIconFile[qIconDot..];
    }

    private static string? QIconNoneResolve(string qIconPath, double qIconWidth) => null;

    private static string? QIconSvgResolve(string qIconPath, double qIconWidth)
    {
        string qNormalizedPath = qIconPath.Replace('\\', '/');
        int qAssetStart = qNormalizedPath.IndexOf(QIconAssetRoot, StringComparison.OrdinalIgnoreCase);
        int qFileStart = qNormalizedPath.LastIndexOf('/') + 1;
        if (qAssetStart < 0 || qFileStart <= qAssetStart + QIconAssetRoot.Length)
        {
            return null;
        }

        string qIconFolder = qNormalizedPath[(qAssetStart + QIconAssetRoot.Length)..(qFileStart - 1)];
        int qNameStart = qFileStart + (qNormalizedPath.AsSpan(qFileStart).StartsWith("P") ? 1 : 0);
        string qIconName = qNormalizedPath[qNameStart..^QIconSvgExtension.Length];
        string qResolution = qIconWidth < QIconLargeThreshold ? "P32" : "P256";
        return $"{QIconAssetRoot}{qResolution}/{qIconFolder}/{qIconName}.png";
    }

    private static string QIconPathResolve(string qIconPath, double qIconWidth)
    {
        if (QIconBitmapResolve(qIconPath, qIconWidth) is not { } qIconBitmapPath)
        {
            return qIconPath;
        }

        var qIconResource = System.Windows.Application.GetResourceStream(QIconUriCreate(qIconBitmapPath));
        using var qIconStream = qIconResource?.Stream;
        return qIconResource is null ? qIconPath : qIconBitmapPath;
    }

    private static ImageSource QIconCreate(string qIconPath, Uri qIconUri, Brush? qIconTintBrush) =>
        qIconReaders[QIconSvgCheck(qIconPath)](qIconUri, qIconTintBrush);

    private static ImageSource QIconBitmapRead(Uri qIconUri, Brush? qIconTintBrush)
    {
        var qIconBitmap = new BitmapImage(qIconUri);
        qIconBitmap.Freeze();
        return QIconMarginResolve(qIconUri, qIconBitmap);
    }

    private static BitmapSource QIconMarginResolve(Uri qIconUri, BitmapSource qIconBitmap)
    {
        if (!QIconPrerenderCheck(qIconUri.AbsolutePath))
        {
            return qIconBitmap;
        }

        int qIconPixelWidth = qIconBitmap.PixelWidth;
        int qIconPixelHeight = qIconBitmap.PixelHeight;
        var qIconPixels = new byte[qIconPixelWidth * qIconPixelHeight * 4];
        var qIconConverted = new FormatConvertedBitmap(qIconBitmap, PixelFormats.Bgra32, null, 0);
        qIconConverted.CopyPixels(qIconPixels, qIconPixelWidth * 4, 0);

        int qIconLeft = qIconPixelWidth;
        int qIconTop = qIconPixelHeight;
        int qIconRight = -1;
        int qIconBottom = -1;
        for (int qIconRow = 0; qIconRow < qIconPixelHeight; qIconRow++)
        {
            for (int qIconColumn = 0; qIconColumn < qIconPixelWidth; qIconColumn++)
            {
                if (qIconPixels[(qIconRow * qIconPixelWidth + qIconColumn) * 4 + 3] == 0)
                {
                    continue;
                }

                qIconLeft = Math.Min(qIconLeft, qIconColumn);
                qIconTop = Math.Min(qIconTop, qIconRow);
                qIconRight = Math.Max(qIconRight, qIconColumn);
                qIconBottom = Math.Max(qIconBottom, qIconRow);
            }
        }

        if (qIconRight < qIconLeft
            || (qIconRight - qIconLeft + 1) * (qIconBottom - qIconTop + 1) == qIconPixelWidth * qIconPixelHeight)
        {
            return qIconBitmap;
        }

        var qIconCropped = new CroppedBitmap(
            qIconBitmap,
            new Int32Rect(qIconLeft, qIconTop, qIconRight - qIconLeft + 1, qIconBottom - qIconTop + 1));
        qIconCropped.Freeze();
        return qIconCropped;
    }

    private static ImageSource QIconSvgRead(Uri qIconUri, Brush? qIconTintBrush)
    {
        using var qIconStream = QIconStreamRead(qIconUri);
        using var qIconReader = new FileSvgReader(qIconSettings);
        DrawingGroup qIconDrawing = qIconReader.Read(qIconStream)!;
        DrawingGroup qIconTinted = qIconTints[ReferenceEquals(qIconTintBrush, null)](qIconDrawing, qIconTintBrush);
        var qIconImage = new DrawingImage(qIconTinted);
        qIconImage.Freeze();
        return qIconImage;
    }

    private static Uri QIconUriCreate(string qIconPath) =>
        new(
            string.Concat(
                QContract.QContractSheetFind<string>("PIconRoot"),
                qIconPath.TrimStart('/')),
            UriKind.Absolute);

    private static System.IO.Stream QIconStreamRead(Uri qIconUri) =>
        System.Windows.Application.GetResourceStream(qIconUri)!.Stream;

    private static DrawingGroup QIconDrawingRead(DrawingGroup qIconDrawing, Brush? qIconTintBrush) => qIconDrawing;

    private static DrawingGroup QIconTintApply(DrawingGroup qIconDrawing, Brush? qIconTintBrush)
    {
        var qIconGroup = qIconDrawing.Clone();
        var qIconBrush = qIconTintBrush!.Clone();
        qIconBrush.Freeze();
        QIconDrawingApply(qIconGroup, qIconBrush);
        qIconGroup.Freeze();
        return qIconGroup;
    }

    private static void QIconDrawingApply(Drawing qIconDrawing, Brush qIconTintBrush) =>
        qIconPainters.GetValueOrDefault(qIconDrawing.GetType(), QIconOtherApply)(qIconDrawing, qIconTintBrush);

    private static void QIconOtherApply(Drawing qIconDrawing, Brush qIconTintBrush)
    {
    }

    private static void QIconGroupApply(Drawing qIconDrawing, Brush qIconTintBrush)
    {
        var qIconGroup = (DrawingGroup)qIconDrawing;
        qIconGroup.Children.ToList().ForEach(qIconChild => QIconDrawingApply(qIconChild, qIconTintBrush));
    }

    private static void QIconGeometryApply(Drawing qIconDrawing, Brush qIconTintBrush)
    {
        var qIconGeometry = (GeometryDrawing)qIconDrawing;
        qIconGeometry.Brush = QIconBrushResolve(qIconGeometry.Brush, qIconTintBrush);
        qIconGeometry.Pen = QIconPenResolve(qIconGeometry.Pen, qIconTintBrush);
    }

    private static void QIconGlyphApply(Drawing qIconDrawing, Brush qIconTintBrush) =>
        ((GlyphRunDrawing)qIconDrawing).ForegroundBrush = qIconTintBrush;

    private static Brush? QIconBrushResolve(Brush? qIconBrush, Brush qIconTintBrush) =>
        QIconBrushCheck(qIconBrush) ? qIconTintBrush : qIconBrush;

    private static Pen? QIconPenResolve(Pen? qIconPen, Brush qIconTintBrush)
    {
        if (qIconPen is null || !QIconBrushCheck(qIconPen.Brush))
        {
            return qIconPen;
        }

        Pen qIconTintedPen = qIconPen.Clone();
        qIconTintedPen.Brush = qIconTintBrush;
        qIconTintedPen.Freeze();
        return qIconTintedPen;
    }

    private static bool QIconBrushCheck(Brush? qIconBrush)
    {
        if (qIconBrush is null || qIconBrush.Opacity <= 0)
        {
            return false;
        }

        return qIconBrush switch
        {
            SolidColorBrush qIconSolid => qIconSolid.Color.A > 0,
            GradientBrush qIconGradient => qIconGradient.GradientStops.Any(qIconStep => qIconStep.Color.A > 0),
            _ => true
        };
    }
}
