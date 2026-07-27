using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
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
        if (pIconPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return PIconSvgRead(pIconPath);
        }

        var pIconBitmap = new BitmapImage(new Uri($"pack://application:,,,{pIconPath}", UriKind.Absolute));
        pIconBitmap.Freeze();
        return pIconBitmap;
    }

    private static ImageSource PIconSvgRead(string pIconPath)
    {
        StreamResourceInfo? pIconResource = Application.GetResourceStream(
            new Uri($"pack://application:,,,{pIconPath}", UriKind.Absolute));
        if (pIconResource is null)
        {
            throw new InvalidOperationException($"Icon asset was not found: {pIconPath}");
        }

        using var pIconReader = new FileSvgReader(pIconSettings);
        DrawingGroup? pIconDrawing = pIconReader.Read(pIconResource.Stream);
        if (pIconDrawing is null)
        {
            throw new InvalidOperationException($"Icon asset could not be rendered: {pIconPath}");
        }

        var pIconImage = new DrawingImage(pIconDrawing);
        if (pIconImage.CanFreeze)
        {
            pIconImage.Freeze();
        }

        return pIconImage;
    }
}
