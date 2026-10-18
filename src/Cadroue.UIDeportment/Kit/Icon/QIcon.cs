using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        string qIconCacheKey = QIconKeyFormat(qIconUri, qIconTintBrush);
        return qIconCache.GetOrAdd(qIconCacheKey, _ => QIconCreate(qIconResolvedPath, qIconUri, qIconTintBrush));
    }

    private static string QIconKeyFormat(Uri qIconUri, Brush? qIconTintBrush) =>
        $"{qIconUri.AbsoluteUri}|{qIconTintBrush}";

    private static bool QIconSvgCheck(string qIconPath) => qIconKinds[QIconExtensionRead(qIconPath)];

    private static string? QIconBitmapResolve(string qIconPath, double qIconWidth)
    {
        bool qIconSvg = qIconPath.EndsWith(QIconSvgExtension, StringComparison.OrdinalIgnoreCase);
        return qIconResolvers[qIconSvg](qIconPath, qIconWidth);
    }

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
        return QIconMargin.QIconMarginResolve(qIconUri, qIconBitmap);
    }

    private static ImageSource QIconSvgRead(Uri qIconUri, Brush? qIconTintBrush)
    {
        using var qIconStream = QIconStreamRead(qIconUri);
        using var qIconReader = new FileSvgReader(qIconSettings);
        DrawingGroup qIconDrawing = qIconReader.Read(qIconStream)!;
        DrawingGroup qIconTinted = QIconTint.QIconTintApply(qIconDrawing, qIconTintBrush);
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
}
