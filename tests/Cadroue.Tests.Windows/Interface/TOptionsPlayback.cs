using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preference")]
public sealed class TOptionsPlayback : CSMpvPort, CSFlyleafPort
{
    [Fact]
    public void Close_DetachesMpvProjection()
    {
        string optionsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/Cadroue.UIDeportment/Annex/Options/QSOptionsWindow.cs"));
        Assert.Contains("qsOptions.QSOptionsDetach();", File.ReadAllText(optionsPath), StringComparison.Ordinal);

        TOptionsPlaybackRun(() =>
        {
            LPreferenceState draft = Interface.TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead());
            QSOptions options = TInterface.TOptionsCreate(draft, false, this, this);
            var projections = new List<(string, bool)>();
            TOptionsPlaybackAttach(options, projections, []);

            projections.Clear();
            options.QSMpv.QSMpvUpdate(true);
            Assert.Contains(("Mpv", true), projections);

            options.QSOptionsDetach();
            projections.Clear();
            options.QSMpv.QSMpvUpdate(false);
            Assert.Empty(projections);
        });
    }

    [Theory]
    [InlineData(250d, 100d)]
    [InlineData(-5d, 0d)]
    [InlineData(42d, 42d)]
    public void Volume_ClampsToSliderRange(double volume, double expected)
    {
        TOptionsPlaybackRun(() =>
        {
            LPreferenceState draft = Interface.TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead());
            draft.LPreferenceVolume = volume;
            QSOptions options = TInterface.TOptionsCreate(draft, false, this, this);
            FrameworkElement surface = TOptionsPlaybackAttach(options, [], []);

            Assert.Equal(expected, ((Slider)surface.FindName("PSPlaybackSlider")).Value);
            Assert.Equal($"{expected:0}%", ((TextBlock)surface.FindName("PSPlaybackReadout")).Text);
            options.QSOptionsDetach();
        });
    }

    [Theory]
    [InlineData(false, "Flyleaf")]
    [InlineData(true, "Mpv")]
    public void Engine_FallsBackToFlyleaf_WithoutMpv(bool mpv, string expected)
    {
        TOptionsPlaybackRun(() =>
        {
            LPreferenceState draft = Interface.TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead());
            draft.LPreferencePreviewEngine = "Mpv";
            QSOptions options = TInterface.TOptionsCreate(draft, mpv, this, this);
            var projections = new List<(string, bool)>();
            var engines = new List<string>();
            TOptionsPlaybackAttach(options, projections, engines);

            Assert.Equal([expected], engines);
            Assert.Equal([("Mpv", mpv)], projections);
            projections.Clear();
            options.QSMpv.QSMpvUpdate(!mpv);
            Assert.Equal([("Mpv", !mpv)], projections);
            options.QSOptionsDetach();
        });
    }

    private static FrameworkElement TOptionsPlaybackAttach(
        QSOptions options,
        List<(string, bool)> projections,
        List<string> engines)
    {
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/Cadroue.UIVeneer/Annex/Options/PSOptionsPlayback.xaml"));
        string contract = File.ReadAllText(path)
            .Replace(" x:Class=\"Cadroue.UIVeneer.PSOptionsPlayback\"", string.Empty, StringComparison.Ordinal)
            .Replace(" x:ClassModifier=\"internal\"", string.Empty, StringComparison.Ordinal);
        var surface = (UserControl)XamlReader.Parse(contract);
        return options.QSOptionsPlayback.QSOptionsPlaybackAttach(
            surface,
            new ContentControl(),
            new ContentControl(),
            (string selected, Action change, out Action<string, bool> enable, LLocalizationChoice[] choices) =>
            {
                if (choices.Any(choice => choice.LLocalizationChoiceToken == "Mpv"))
                {
                    engines.Add(selected);
                    enable = (token, enabled) => projections.Add((token, enabled));
                }
                else
                {
                    enable = (_, _) => { };
                }

                return new Border { Tag = selected };
            },
            border => (string)border.Tag);
    }

    private static void TOptionsPlaybackRun(Action test)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                System.Windows.Application program = System.Windows.Application.Current
                    ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                foreach (string sheet in new[] { "PCheckbox", "PSlider" })
                {
                    if (program.TryFindResource(sheet) is null)
                    {
                        using Stream stream = File.OpenRead(Path.GetFullPath(Path.Combine(
                            AppContext.BaseDirectory, $"../../../../../src/Cadroue.UIVeneer/Kit/{sheet}.xaml")));
                        program.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(stream));
                    }
                }

                test();
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

    bool CSMpvPort.CSMpvInstalledCheck() => false;

    Task<bool> CSMpvPort.CSMpvAvailabilityCheck() => new TaskCompletionSource<bool>().Task;

    Task<(bool, string, bool)> CSMpvPort.CSMpvStart(IProgress<double> progress) =>
        throw new NotSupportedException();

    bool CSFlyleafPort.CSFlyleafInstalledCheck() => false;

    Task<(bool, string, bool)> CSFlyleafPort.CSFlyleafStart(IProgress<double> progress) =>
        throw new NotSupportedException();
}
