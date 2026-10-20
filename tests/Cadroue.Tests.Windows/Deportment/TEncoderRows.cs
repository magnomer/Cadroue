using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Tests.Interface;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

[CollectionDefinition("EncoderSurface", DisableParallelization = true)]
public sealed class TEncoderSurfaceCollection;

[Collection("EncoderSurface")]
public sealed class TEncoderRows
{
    private static void TEncoderRun(Action body)
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

    private static string TEncoderRootFind()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cadroue.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the folder holding Cadroue.slnx.");
    }

    private static UserControl TEncoderSurfaceBuild()
    {
        string veneer = Path.Combine(TEncoderRootFind(), "src", "Cadroue.UIVeneer");
        System.Windows.Application program = System.Windows.Application.Current
            ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        foreach (string sheet in new[] { "PSlider", "PTextbox", "PCheckbox" })
        {
            if (program.TryFindResource(sheet) is null)
            {
                using FileStream stream = File.OpenRead(Path.Combine(veneer, "Kit", sheet + ".xaml"));
                program.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(stream));
            }
        }

        string markup = File.ReadAllText(Path.Combine(veneer, "Annex", "Encoder", "PSEncoderResolution.xaml"));
        return (UserControl)XamlReader.Parse(
            markup.Replace("x:Class=\"Cadroue.UIVeneer.PSEncoderResolution\"", string.Empty, StringComparison.Ordinal));
    }

    [Fact]
    public void Tier_SelectFromSource_StoresTierSize() => TEncoderRun(() =>
    {
        QSEncoder encoder = TInterface.TEncoderCreate(TInterface.TPresetSelectionCreate(string.Empty), false);
        UserControl surface = TEncoderSurfaceBuild();
        QSEncoderResolution resolution = new(surface, encoder.QSEncoderSize, false);
        TInterface.TEncoderSizeAttach(encoder, resolution.QSEncoderResolutionApply);
        resolution.QSEncoderResolutionBuild();
        Assert.Equal(0, encoder.QSEncoderSize.QSEncoderSizeValue.CSEncoderDimensionTier);
        TextBox[] boxes = [.. QContract.QContractFind<StackPanel>(surface, "psEncoderResolutionRows").Children
            .Cast<UIElement>()
            .Select(row => QContract.QContractFind<TextBox>(row, "psEncoderResolutionValue"))];
        string[] loaded = [.. boxes.Select(box => box.Text)];
        Slider tier = QContract.QContractFind<Slider>(surface, "psEncoderResolutionTier");

        tier.Value = 3;

        Assert.Equal(3, encoder.QSEncoderSize.QSEncoderSizeValue.CSEncoderDimensionTier);
        Assert.Equal("1920 × 1080", TInterface.TEncoderSizeFormat(encoder));
        Assert.Equal("1080p", QContract.QContractFind<TextBlock>(surface, "psEncoderResolutionReadout").Text);

        tier.Value = 0;

        Assert.Equal(0, encoder.QSEncoderSize.QSEncoderSizeValue.CSEncoderDimensionTier);
        Assert.Equal(loaded, boxes.Select(box => box.Text));
    });

    [Fact]
    public void VideoSet_UnknownRate_ResolvesToFirstMode_RaisesOnce()
    {
        QSEncoder encoder = TInterface.TEncoderCreate(TInterface.TPresetSelectionCreate(string.Empty), false);
        int rows = 0;
        TInterface.TEncoderVideoAttach(encoder, () => rows++);
        (IReadOnlyList<string> offered, int kept) = encoder.QSEncoderVideo.QSEncoderVideoOffer;
        string codec = offered[kept];
        IReadOnlyList<string> modes = encoder.QSEncoderVideo.QSEncoderVideoSync(null)!;
        bool several = modes.Count > 1;

        Assert.Equal(several, TInterface.TEncoderVideoSet(encoder, codec, modes[^1]));
        Assert.Equal(modes[^1], encoder.QSEncoderVideo.QSEncoderVideoRate);
        Assert.Equal(several, TInterface.TEncoderVideoSet(encoder, codec, "no such mode"));
        Assert.Equal(modes[0], encoder.QSEncoderVideo.QSEncoderVideoRate);
        Assert.False(TInterface.TEncoderVideoSet(encoder, codec, modes[0]));
        Assert.Equal(several ? 2 : 0, rows);
    }

    [Fact]
    public void VideoSet_EncoderChange_KeepsRateWhenOffered()
    {
        QSEncoder encoder = TInterface.TEncoderCreate(TInterface.TPresetSelectionCreate(string.Empty), false);
        (IReadOnlyList<string> offered, int kept) = encoder.QSEncoderVideo.QSEncoderVideoOffer;
        string other = offered.First(text => !string.Equals(text, offered[kept], StringComparison.Ordinal));
        string rate = encoder.QSEncoderVideo.QSEncoderVideoRate;

        Assert.True(TInterface.TEncoderVideoSet(encoder, other, rate));
        (IReadOnlyList<string> changed, int chosen) = encoder.QSEncoderVideo.QSEncoderVideoOffer;
        Assert.Equal(other, changed[chosen]);
        Assert.Contains(
            encoder.QSEncoderVideo.QSEncoderVideoRate,
            encoder.QSEncoderVideo.QSEncoderVideoSync(null)!);
    }

    [Fact]
    public void Tier_SelectSetsDimensions_SizeSetMatchesTier()
    {
        QSEncoder encoder = TInterface.TEncoderCreate(TInterface.TPresetSelectionCreate(string.Empty), false);
        int sizes = 0;
        TInterface.TEncoderSizeAttach(encoder, () => sizes++);
        Assert.Equal(0, encoder.QSEncoderSize.QSEncoderSizeValue.CSEncoderDimensionTier);
        Assert.Equal("Same as source", TInterface.TEncoderSizeFormat(encoder));

        Assert.True(TInterface.TEncoderTierSelect(encoder, 3));
        Assert.Equal(1920, encoder.QSEncoderSize.QSEncoderSizeValue.CSEncoderDimensionWidth);
        Assert.Equal(1080, encoder.QSEncoderSize.QSEncoderSizeValue.CSEncoderDimensionHeight);
        Assert.Equal("1920 × 1080", TInterface.TEncoderSizeFormat(encoder));
        Assert.False(TInterface.TEncoderTierSelect(encoder, 3));

        Assert.False(TInterface.TEncoderSizeSet(encoder, 1920, 1080));
        Assert.True(TInterface.TEncoderSizeSet(encoder, 1080, 1920));
        Assert.Equal(3, encoder.QSEncoderSize.QSEncoderSizeValue.CSEncoderDimensionTier);
        Assert.True(TInterface.TEncoderSizeSet(encoder, 1000, 0));
        Assert.Equal(-1, encoder.QSEncoderSize.QSEncoderSizeValue.CSEncoderDimensionTier);
        Assert.Equal("Same as source", TInterface.TEncoderSizeFormat(encoder));
        Assert.True(TInterface.TEncoderSizeSet(encoder, 0, 0));
        Assert.Equal(0, encoder.QSEncoderSize.QSEncoderSizeValue.CSEncoderDimensionTier);
        Assert.Equal(4, sizes);
    }

    [Fact]
    public void Smart_GatesVideoNotice()
    {
        QSEncoder plain = TInterface.TEncoderCreate(TInterface.TPresetSelectionCreate(string.Empty), false);
        QSEncoder smart = TInterface.TEncoderCreate(TInterface.TPresetSelectionCreate(string.Empty), true);

        Assert.Equal("Encoder.Video.Notice.SmartFull", TInterface.TEncoderNoticeResolve(plain, 1));
        Assert.Equal("Encoder.Video.Notice.Smart", TInterface.TEncoderNoticeResolve(smart, 1));
        Assert.Equal("Encoder.Video.Notice.Copied", TInterface.TEncoderNoticeResolve(smart, 0));
    }

    [Fact]
    public void Suffix_SelectStoresShownUnderPreviousPolicy()
    {
        QSEncoder encoder = TInterface.TEncoderCreate(TInterface.TPresetSelectionCreate(string.Empty), false);
        string first = TInterface.TEncoderSuffixSelect(encoder, "Rename output", "ignored");
        Assert.Equal(encoder.QSEncoderSuffix.QSEncoderSuffixRead("Rename output"), first);

        TInterface.TEncoderSuffixSelect(encoder, "Rename existing", "_mine");
        Assert.Equal("_mine", encoder.QSEncoderSuffix.QSEncoderSuffixRead("Rename output"));
        Assert.Equal("_mine", TInterface.TEncoderSuffixSelect(encoder, "Rename output", "_theirs"));
        Assert.Equal("_theirs", encoder.QSEncoderSuffix.QSEncoderSuffixRead("Rename existing"));
    }

    [Fact]
    public void Apply_CopiesDraftIntoSource()
    {
        LPresetSelection selection = TInterface.TPresetSelectionCreate(string.Empty);
        LPresetDocument prior = selection.LPresetSelectionValue;
        selection.LPresetSelectionValue = TInterface.TPresetDocumentCreate(selection.LPresetSelectionName, "  ");
        try
        {
            QSEncoder encoder = TInterface.TEncoderCreate(selection, false);
            TInterface.TEncoderTierSelect(encoder, 2);

            TInterface.TEncoderApply(encoder);
            encoder.QSEncoderSave();

            LPresetDocument saved = selection.LPresetSelectionValue;
            Assert.Equal("1280 × 720", saved.LPresetVideo.LPresetSize);
            Assert.Equal("{OriginalName}_export", saved.LPresetDisplay);
            Assert.Equal("Include", saved.LPresetVideo.LPresetStream);
        }
        finally
        {
            selection.LPresetSelectionValue = prior;
            TInterface.TPresetSelectionClose(selection);
        }
    }

    [Fact]
    public void Apply_AfterRename_CopiesDraftIntoLiveSelection()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Encoder before rename");
        LPresetSelection selection = TInterface.TPresetSelectionCreate("Encoder before rename");
        selection.LPresetSelectionValue =
            TInterface.TPresetDocumentCreate(selection.LPresetSelectionName, "  ");
        QSEncoder? encoder = null;
        try
        {
            encoder = TInterface.TEncoderCreate(selection, false);
            Assert.True(TInterface.TPresetSelectionCommit(
                selection, "Encoder before rename", "Encoder after rename"));
            TInterface.TEncoderTierSelect(encoder, 2);

            TInterface.TEncoderApply(encoder);
            encoder.QSEncoderSave();

            LPresetDocument saved = selection.LPresetSelectionValue;
            Assert.Equal("Encoder after rename", selection.LPresetSelectionName);
            Assert.Equal("1280 × 720", saved.LPresetVideo.LPresetSize);
        }
        finally
        {
            if (encoder is not null)
            {
                TInterface.TEncoderClose(encoder);
            }
            TInterface.TPresetSelectionClose(selection);
        }
    }
}
