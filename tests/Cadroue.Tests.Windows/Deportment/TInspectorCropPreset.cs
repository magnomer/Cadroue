using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.UIDeportment;
using System.Reflection;
using System.Windows.Controls;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TInspectorCropPreset
{
    [Fact]
    public void RatioRepaint_DoesNotRequestConduct()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                QInspector inspector = TInterfaceInspector.TInspectorCreate();
                TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
                QInspectorRatio ratio = inspector.QInspectorRatio;
                Action<string, object> set = (name, value) => typeof(QInspectorRatio)
                    .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(ratio, value);
                set("qInspectorRatioCustom", new StackPanel());
                set("qInspectorRatioFixed", new CheckBox { IsChecked = true });
                var height = new TextBox { Text = "9" };
                set("qInspectorRatioHeight", height);
                set("qInspectorRatioLenient", new CheckBox { IsChecked = true });
                set("qInspectorRatioNotice", new TextBlock());
                var preset = new ComboBox();
                foreach (int index in Enumerable.Range(0, 7)) preset.Items.Add(index);
                set("qInspectorRatioPreset", preset);
                var width = new TextBox { Text = "16" };
                set("qInspectorRatioWidth", width);
                int requests = 0;
                TextChangedEventHandler widthHandler = (_, _) =>
                {
                    requests++;
                    ratio.QInspectorRatioCommit(width.Text, height.Text, false);
                };
                TextChangedEventHandler heightHandler = (_, _) =>
                {
                    requests++;
                    ratio.QInspectorRatioCommit(width.Text, height.Text, false);
                };
                SelectionChangedEventHandler presetHandler = (_, _) =>
                {
                    requests++;
                    ratio.QInspectorPresetSelect(preset.SelectedIndex);
                };
                set("qInspectorRatioPainting", new Delegate[] { widthHandler, heightHandler, presetHandler });
                width.TextChanged += widthHandler;
                height.TextChanged += heightHandler;
                preset.SelectionChanged += presetHandler;
                var port = new TCropPort(inspector);
                inspector.QInspectorGate.CInspectorCrop.CCropAccept(port);

                ratio.QInspectorRatioUpdate();

                Assert.Equal(0, requests);
                preset.SelectedIndex = 1;
                Assert.Equal(1, requests);
                Assert.Equal(1, port.TCropPortRequests);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void GridRepaint_DoesNotRequestConduct()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                QInspector inspector = TInterfaceInspector.TInspectorCreate();
                QInspectorGrid grid = inspector.QInspectorCrop.QInspectorGrid;
                Action<string, object> set = (name, value) => typeof(QInspectorGrid)
                    .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(grid, value);
                var left = new TextBox { Text = "9" };
                set("qInspectorGridBottom", new TextBox { Text = "9" });
                set("qInspectorGridLeft", left);
                set("qInspectorGridRight", new TextBox { Text = "9" });
                set("qInspectorGridTop", new TextBox { Text = "9" });
                set("qInspectorGridResolution", new TextBlock());
                int requests = 0;
                var handlers = new Dictionary<TextBox, TextChangedEventHandler>();
                foreach ((TextBox box, int side) in new[]
                         {
                             (left, 0),
                             ((TextBox)typeof(QInspectorGrid).GetField("qInspectorGridTop",
                                 BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(grid)!, 1),
                             ((TextBox)typeof(QInspectorGrid).GetField("qInspectorGridRight",
                                 BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(grid)!, 2),
                             ((TextBox)typeof(QInspectorGrid).GetField("qInspectorGridBottom",
                                 BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(grid)!, 3),
                         })
                {
                    TextChangedEventHandler handler = (_, _) =>
                    {
                        requests++;
                        inspector.QInspectorCrop.QInspectorEdgeCommit(side, box.Text, true);
                    };
                    handlers.Add(box, handler);
                    box.TextChanged += handler;
                }
                set("qInspectorGridPainting", handlers);
                var port = new TCropPort(inspector);
                inspector.QInspectorGate.CInspectorCrop.CCropAccept(port);
                grid.QInspectorGridUpdate(inspector.QInspectorCrop);

                Assert.Equal(0, requests);
                Assert.Equal(0, port.TCropPortRequests);
                left.Text = "5";
                Assert.Equal(1, requests);
                Assert.Equal(1, port.TCropPortRequests);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Theory]
    [InlineData(1, "16", "9")]
    [InlineData(2, "9", "16")]
    [InlineData(3, "4", "3")]
    [InlineData(4, "3", "4")]
    [InlineData(5, "1", "1")]
    [InlineData(6, "21", "9")]
    public void PresetSelect_Index_SetsRatioAndReadsBack(int index, string width, string height)
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);

        TInterfaceInspector.TInspectorPresetSelect(inspector, index);

        Assert.True(inspector.QInspectorCrop.QInspectorRatioFixed);
        Assert.Equal(index, inspector.QInspectorCrop.QInspectorPresetIndex);
        Assert.False(inspector.QInspectorCrop.QInspectorCustomShown);
        Assert.Equal(width, TInterfaceInspector.TInspectorRatioFormat(inspector, true, string.Empty));
        Assert.Equal(height, TInterfaceInspector.TInspectorRatioFormat(inspector, false, string.Empty));
    }

    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(1080, 1920)]
    [InlineData(1440, 1080)]
    [InlineData(720, 1280)]
    public void PresetSelect_FitsInsideSource(double sourceWidth, double sourceHeight)
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, sourceWidth, sourceHeight);

        TInterfaceInspector.TInspectorPresetSelect(inspector, 5);

        LWorkCrop crop = TInterfaceInspector.TInspectorCropRead(inspector);
        double width = sourceWidth - crop.LWorkCropLeft - crop.LWorkCropRight;
        double height = sourceHeight - crop.LWorkCropTop - crop.LWorkCropBottom;
        Assert.True(width > 0 && height > 0);
        Assert.True(width <= sourceWidth && height <= sourceHeight);
        Assert.Equal(width, height);
        Assert.False(inspector.QInspectorCrop.QInspectorNoticeShown);
    }

    [Fact]
    public void PresetSelect_NoSource_LeavesRatioFree()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();

        TInterfaceInspector.TInspectorPresetSelect(inspector, 1);

        Assert.False(inspector.QInspectorCrop.QInspectorRatioFixed);
        Assert.Equal(0, inspector.QInspectorCrop.QInspectorPresetIndex);
        Assert.True(inspector.QInspectorCrop.QInspectorCustomShown);
    }

    [Fact]
    public void PresetSelect_Custom_AfterPreset_ResetsRatio()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        TInterfaceInspector.TInspectorPresetSelect(inspector, 1);

        TInterfaceInspector.TInspectorPresetSelect(inspector, 0);

        Assert.False(inspector.QInspectorCrop.QInspectorRatioFixed);
        Assert.Equal("0", TInterfaceInspector.TInspectorRatioFormat(inspector, true, "16"));
        Assert.Equal(string.Empty, TInterfaceInspector.TInspectorRatioFormat(inspector, false, string.Empty));
    }

    [Fact]
    public void RatioCommit_EchoAfterPreset_KeepsPreset()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        TInterfaceInspector.TInspectorLenientSet(inspector, true);
        TInterfaceInspector.TInspectorPresetSelect(inspector, 1);
        TInterfaceInspector.TInspectorFixedSet(inspector, true);
        TInterfaceInspector.TInspectorLenientSet(inspector, true);
        LWorkCrop before = TInterfaceInspector.TInspectorCropRead(inspector);

        TInterfaceInspector.TInspectorRatioCommit(inspector, "16", "0", false);
        TInterfaceInspector.TInspectorEdgeCommit(inspector, 0, "7", false);

        Assert.Equal(1, inspector.QInspectorCrop.QInspectorPresetIndex);
        Assert.True(inspector.QInspectorCrop.QInspectorRatioLenient);
        Assert.Equal("9", TInterfaceInspector.TInspectorRatioFormat(inspector, false, "0"));
        Assert.Equal(before, TInterfaceInspector.TInspectorCropRead(inspector));
    }

    [Fact]
    public void RatioCommit_CustomText_KeepsIndexZero_FixedShowsMismatch()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);

        TInterfaceInspector.TInspectorRatioCommit(inspector, "5", "4");
        TInterfaceInspector.TInspectorFixedSet(inspector, true);
        CInspectorNotice? notice = CInspectorNotice.CInspectorNoticeResolve(
            inspector.QInspectorGate.CInspectorCrop.CCropNotice);

        Assert.Equal(0, inspector.QInspectorCrop.QInspectorPresetIndex);
        Assert.True(inspector.QInspectorCrop.QInspectorCustomShown);
        Assert.True(inspector.QInspectorCrop.QInspectorNoticeShown);
        Assert.NotEqual(string.Empty, TInterfaceInspector.TInspectorNoticeRead(inspector));
        Assert.NotNull(notice);
        Assert.NotEmpty(notice.CInspectorNoticeArguments);
        var arguments = new List<object?> { 7 };
        var direct = new CInspectorNotice("Alias", arguments);
        arguments[0] = 9;
        Assert.Equal(7, direct.CInspectorNoticeArguments[0]);
        Assert.False(typeof(CInspectorNotice)
            .GetProperty(nameof(CInspectorNotice.CInspectorNoticeArguments))!.CanWrite);
    }

    [Fact]
    public void LenientSet_WithinTolerance_HidesNotice_LenientNeedsFixed()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        TInterfaceInspector.TInspectorRatioCommit(inspector, "16", "9");

        TInterfaceInspector.TInspectorLenientSet(inspector, true);
        Assert.False(inspector.QInspectorCrop.QInspectorRatioLenient);

        TInterfaceInspector.TInspectorFixedSet(inspector, true);
        TInterfaceInspector.TInspectorLenientSet(inspector, true);
        TInterfaceInspector.TInspectorEdgeCommit(inspector, 0, "2");

        Assert.True(inspector.QInspectorCrop.QInspectorRatioLenient);
        Assert.False(inspector.QInspectorCrop.QInspectorNoticeShown);
        Assert.Null(CInspectorNotice.CInspectorNoticeResolve(
            inspector.QInspectorGate.CInspectorCrop.CCropNotice));
    }

    [Fact]
    public void RatioFormat_FreeRatio_ShowsNormalizedCropSize()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        Assert.Equal(string.Empty, TInterfaceInspector.TInspectorRatioFormat(inspector, true, string.Empty));

        TInterfaceInspector.TInspectorEdgeCommit(inspector, 0, "320");

        Assert.Equal("40", TInterfaceInspector.TInspectorRatioFormat(inspector, true, string.Empty));
        Assert.Equal("27", TInterfaceInspector.TInspectorRatioFormat(inspector, false, string.Empty));
        Assert.Equal("40", TInterfaceInspector.TInspectorRatioFormat(inspector, true, "40"));
        Assert.Equal("1600 × 1080", TInterfaceInspector.TInspectorResolutionRead(inspector));
    }

    [Fact]
    public void ApplySet_Off_ResetsRatio_OnKeepsIt()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        TInterfaceInspector.TInspectorApplySet(inspector, true);
        TInterfaceInspector.TInspectorPresetSelect(inspector, 1);

        TInterfaceInspector.TInspectorApplySet(inspector, true);
        Assert.True(inspector.QInspectorCrop.QInspectorRatioFixed);

        TInterfaceInspector.TInspectorApplySet(inspector, false);

        Assert.False(inspector.QInspectorCrop.QInspectorActive);
        Assert.False(inspector.QInspectorCrop.QInspectorRatioFixed);
    }
}
