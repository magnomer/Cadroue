using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection(nameof(TProgramCollection))]
public sealed class TBlankWheel
{
    [Fact]
    public void Repaint_DoesNotRequestBlank()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            Dictionary<bool, Func<string, double, string?>>? resolvers = null;
            Func<string, double, string?>? resolver = null;
            try
            {
                System.Windows.Application program = System.Windows.Application.Current
                    ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                program.Resources["PToolFont"] = new FontFamily("Segoe UI");
                program.Resources["PToolText"] = Brushes.Black;
                program.Resources["PToolMuted"] = Brushes.Gray;
                program.Resources["PToolIcon"] = Brushes.Black;
                program.Resources["PToolAccent"] = Brushes.Blue;
                program.Resources["PToolDimmed"] = 0.5;
                program.Resources["PIconRoot"] = "pack://application:,,,/";
                foreach (string sheet in new[]
                         {
                             "Kit/PCheckbox", "Kit/PSlider", "Kit/PRadio", "Kit/PTextbox",
                             "Kit/Panel/PPanel", "Wing/Inspector/PBlank",
                             "Wing/Inspector/Whitebalance/PWhitebalanceWheel",
                         })
                {
                    string path = Path.GetFullPath(Path.Combine(
                        AppContext.BaseDirectory, $"../../../../../src/Cadroue.UIVeneer/{sheet}.xaml"));
                    if (sheet.StartsWith("Wing/", StringComparison.Ordinal))
                    {
                        var document = System.Xml.Linq.XDocument.Load(path);
                        System.Xml.Linq.XElement surface = document.Root!.Elements().Single();
                        System.Xml.Linq.XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
                        string key = (string)surface.Attribute(x + "Key")!;
                        surface.Attribute(x + "Key")!.Remove();
                        surface.Attribute(x + "Shared")!.Remove();
                        program.Resources[key] = XamlReader.Parse(surface.ToString());
                    }
                    else
                    {
                        string contract = File.ReadAllText(path)
                            .Replace(" x:Shared=\"False\"", string.Empty, StringComparison.Ordinal);
                        using var reader = System.Xml.XmlReader.Create(new StringReader(contract));
                        program.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(reader));
                    }
                }

                resolvers = (Dictionary<bool, Func<string, double, string?>>)typeof(QIcon)
                    .GetField("qIconResolvers", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                resolver = resolvers[true];
                resolvers[true] = (_, _) => null;
                var icons = (System.Collections.Concurrent.ConcurrentDictionary<string, ImageSource>)typeof(QIcon)
                    .GetField("qIconCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                foreach (Brush brush in new[] { QTool.QToolIconRead(), QTool.QToolPickRead(true) })
                {
                    icons[$"pack://application:,,,/PAsset/PPanel/PProcessingPicker.svg|{brush}"] = new DrawingImage();
                }

                QInspector inspector = TInterfaceInspector.TInspectorCreate();
                QBlank blank = inspector.QInspectorBlank;
                int requests = 0;
                int picks = 0;
                blank.QBlankChanged += () => requests++;
                blank.QBlankPickChanged += _ => picks++;

                blank.QBlankBuild(new QWhitebalanceWheel(inspector));
                typeof(QBlank).GetMethod("QBlankUpdate", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(blank, null);
                typeof(QBlank).GetMethod("QBlankUpdate", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(blank, null);

                Assert.Equal(0, requests);
                Assert.Equal(0, picks);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                if (resolvers is not null && resolver is not null)
                {
                    resolvers[true] = resolver;
                }
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void UserActs_RequestBlankOnce()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            Dictionary<bool, Func<string, double, string?>>? resolvers = null;
            Func<string, double, string?>? resolver = null;
            try
            {
                System.Windows.Application program = System.Windows.Application.Current
                    ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                program.Resources["PToolFont"] = new FontFamily("Segoe UI");
                program.Resources["PToolText"] = Brushes.Black;
                program.Resources["PToolMuted"] = Brushes.Gray;
                program.Resources["PToolIcon"] = Brushes.Black;
                program.Resources["PToolAccent"] = Brushes.Blue;
                program.Resources["PToolDimmed"] = 0.5;
                program.Resources["PIconRoot"] = "pack://application:,,,/";
                foreach (string sheet in new[]
                         {
                             "Kit/PCheckbox", "Kit/PSlider", "Kit/PRadio", "Kit/PTextbox",
                             "Kit/Panel/PPanel", "Wing/Inspector/PBlank",
                             "Wing/Inspector/Whitebalance/PWhitebalanceWheel",
                         })
                {
                    string path = Path.GetFullPath(Path.Combine(
                        AppContext.BaseDirectory, $"../../../../../src/Cadroue.UIVeneer/{sheet}.xaml"));
                    if (sheet.StartsWith("Wing/", StringComparison.Ordinal))
                    {
                        var document = System.Xml.Linq.XDocument.Load(path);
                        System.Xml.Linq.XElement surface = document.Root!.Elements().Single();
                        System.Xml.Linq.XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
                        string key = (string)surface.Attribute(x + "Key")!;
                        surface.Attribute(x + "Key")!.Remove();
                        surface.Attribute(x + "Shared")!.Remove();
                        program.Resources[key] = XamlReader.Parse(surface.ToString());
                    }
                    else
                    {
                        string contract = File.ReadAllText(path)
                            .Replace(" x:Shared=\"False\"", string.Empty, StringComparison.Ordinal);
                        using var reader = System.Xml.XmlReader.Create(new StringReader(contract));
                        program.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(reader));
                    }
                }

                resolvers = (Dictionary<bool, Func<string, double, string?>>)typeof(QIcon)
                    .GetField("qIconResolvers", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                resolver = resolvers[true];
                resolvers[true] = (_, _) => null;
                var icons = (System.Collections.Concurrent.ConcurrentDictionary<string, ImageSource>)typeof(QIcon)
                    .GetField("qIconCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                foreach (Brush brush in new[] { QTool.QToolIconRead(), QTool.QToolPickRead(true) })
                {
                    icons[$"pack://application:,,,/PAsset/PPanel/PProcessingPicker.svg|{brush}"] = new DrawingImage();
                }

                QInspector inspector = TInterfaceInspector.TInspectorCreate();
                QBlank blank = inspector.QInspectorBlank;
                blank.QBlankBuild(new QWhitebalanceWheel(inspector));
                int requests = 0;
                int picks = 0;
                blank.QBlankChanged += () => requests++;
                blank.QBlankPickChanged += _ => picks++;

                QContract.QContractFind<CheckBox>(blank.QBlankBody, "PBlankSwitch").IsChecked = true;
                Assert.Equal(1, requests);
                requests = 0;

                QContract.QContractFind<RadioButton>(blank.QBlankBody, "PBlankColor").IsChecked = true;
                Assert.Equal(1, requests);
                requests = 0;

                QContract.QContractFind<ToggleButton>(blank.QBlankBody, "PBlankPicker").IsChecked = true;
                Assert.Equal(1, picks);
                Assert.Equal(0, requests);
                picks = 0;

                TextBox tolerance = QContract.QContractFind<TextBox>(blank.QBlankBody, "PBlankToleranceValue");
                tolerance.Text = "0.5";
                tolerance.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent, tolerance));
                Assert.Equal(1, requests);
                Assert.Equal(0, picks);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                if (resolvers is not null && resolver is not null)
                {
                    resolvers[true] = resolver;
                }
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void FreshInstance_TypeSetColor_WheelPresent()
    {
        QBlank blank = TInterface.TBlankCreate();
        Assert.False(TInterface.TBlankPresentRead(blank));

        TInterface.TBlankTypeSet(blank, LDetectorType.LDetectorTypeColor);

        Assert.True(TInterface.TBlankPresentRead(blank));
    }

    [Fact]
    public void TypeSetBlack_AfterPick_WheelAbsent()
    {
        QBlank blank = TInterface.TBlankCreate();
        TInterface.TBlankWheelHandle(blank, true, 75, 50, 100);
        Assert.True(TInterface.TBlankPresentRead(blank));

        TInterface.TBlankTypeSet(blank, LDetectorType.LDetectorTypeBlack);

        Assert.False(TInterface.TBlankPresentRead(blank));
    }

    [Fact]
    public void StepSet_FollowsStepType()
    {
        QBlank blank = TInterface.TBlankCreate();
        TInterface.TBlankStepSet(blank, TInterface.TBlankStepCreate(LDetectorType.LDetectorTypeColor));
        Assert.True(TInterface.TBlankPresentRead(blank));

        TInterface.TBlankStepSet(blank, TInterface.TBlankStepCreate(LDetectorType.LDetectorTypeBlack));
        Assert.False(TInterface.TBlankPresentRead(blank));
    }

    [Fact]
    public void SampleSet_ForcesColorType()
    {
        QBlank blank = TInterface.TBlankCreate();

        TInterface.TBlankSampleSet(blank, 255, 0, 0);

        Assert.True(TInterface.TBlankPresentRead(blank));
        Assert.Equal(LDetectorType.LDetectorTypeColor, TInterface.TBlankStepRead(blank).LDetectorBlankType);
    }
    [Fact]
    public void DotRead_AbsentWhileBlack_PlacedAfterPress()
    {
        QBlank blank = TInterface.TBlankCreate();
        Assert.False(TInterface.TBlankDotRead(blank, 120, 11).LNeutralDotPresent);

        TInterface.TBlankWheelHandle(blank, false, 114, 60, 120);
        Assert.False(TInterface.TBlankPresentRead(blank));

        TInterface.TBlankWheelHandle(blank, true, 114, 60, 120);
        LNeutralDot dot = TInterface.TBlankDotRead(blank, 120, 11);

        Assert.True(dot.LNeutralDotPresent);
        Assert.Equal(60 + 54 - 5.5, dot.LNeutralDotLeft, 1);
        Assert.Equal(60 - 5.5, dot.LNeutralDotTop, 1);
        Assert.Equal(0, TInterface.TBlankStepRead(blank).LDetectorBlankHue, 3);
    }
}
