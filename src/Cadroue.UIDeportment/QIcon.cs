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

    public static bool QIconSvgCheck(string qIconPath) => qIconKinds[QIconExtensionRead(qIconPath)];

    public static string? QIconBitmapResolve(string qIconPath, double qIconWidth)
    {
        bool qIconSvg = qIconPath.EndsWith(QIconSvgExtension, StringComparison.OrdinalIgnoreCase);
        return qIconResolvers[qIconSvg](qIconPath, qIconWidth);
    }

    public static bool QIconPrerenderCheck(string qIconPath) =>
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
}
