using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;

using Cadroue.Application;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TInfoFace
{
    private static readonly string[] tInfoKinds =
    [
        LInfoRow.LInfoRowMuted, LInfoRow.LInfoRowText, LInfoRow.LInfoRowGrey, LInfoRow.LInfoRowGood,
        LInfoRow.LInfoRowBad, LInfoRow.LInfoRowSeparator,
    ];

    private static void TInfoRun(Action body)
    {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                body();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }

    private static UserControl TInfoSurfaceBuild()
    {
        UserControl surface = new() { Content = new StackPanel { Name = "pInfoItems" } };
        foreach (string kind in tInfoKinds)
        {
            string item = kind == LInfoRow.LInfoRowSeparator
                ? $"<Border Tag=\"{kind}\" />"
                : $"<StackPanel Tag=\"{kind}\"><TextBlock x:Name=\"pInfoCaption\" /></StackPanel>";
            surface.Resources["pInfo" + kind] = (DataTemplate)XamlReader.Parse(
                "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" "
                + $"xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{item}</DataTemplate>");
        }

        return surface;
    }

    private static void TInfoRowsCheck(QInfo info, UserControl surface)
    {
        IReadOnlyList<LInfoRow> rows = TInterface.TInfoRowsRead(info);
        FrameworkElement[] items = ((StackPanel)surface.Content).Children.Cast<FrameworkElement>().ToArray();
        Assert.Equal(rows.Count, items.Length);
        for (int index = 0; index < rows.Count; index++)
        {
            Assert.Equal(rows[index].LInfoRowKind, items[index].Tag);
            if (items[index] is StackPanel line)
            {
                Assert.Equal(rows[index].LInfoRowCaption, ((TextBlock)line.Children[0]).Text);
            }
        }
    }

    [Fact]
    public void Attach_NoMedia_ShowsTheMutedRowThroughItsTemplate() => TInfoRun(() =>
    {
        QInfo info = TInterface.TInfoCreate(TInterfaceViewer.TViewerCreate());
        UserControl surface = TInfoSurfaceBuild();

        Assert.Same(surface, TInterface.TInfoFaceAttach(info, surface));

        FrameworkElement item = Assert.IsAssignableFrom<FrameworkElement>(
            Assert.Single(((StackPanel)surface.Content).Children.Cast<UIElement>()));
        Assert.Equal(LInfoRow.LInfoRowMuted, item.Tag);
        TInfoRowsCheck(info, surface);
    });

    [Fact]
    public void MediaChange_RedrawsEachRowWithTheTemplateOfItsKind() => TInfoRun(() =>
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        QInfo info = TInterface.TInfoCreate(viewer);
        UserControl surface = TInterface.TInfoFaceAttach(info, TInfoSurfaceBuild());

        TInterfaceViewer.TViewerMediaRaise(
            viewer,
            TInterfaceViewer.TCargoCreate(
                @"C:\clip.mp4", TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(75), 640, 360), true));
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

        Assert.True(((StackPanel)surface.Content).Children.Count > 1);
        TInfoRowsCheck(info, surface);
    });
}
