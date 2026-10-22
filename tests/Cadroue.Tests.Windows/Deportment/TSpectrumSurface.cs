using Cadroue.UIDeportment;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preference")]
public sealed class TSpectrumSurface
{
    [Fact]
    public void QSSpectrumAttach()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            string folder = Path.Combine(
                Path.GetTempPath(), "Cadroue.Tests", "SpectrumSurface", Guid.NewGuid().ToString("N"));
            string invalid = Path.Combine(Path.GetTempPath(), $"spectrum-invalid-{Guid.NewGuid():N}.json");
            Dictionary<bool, Func<string, double, string?>>? resolvers = null;
            Func<string, double, string?>? resolver = null;
            Window? host = null;
            try
            {
                File.WriteAllText(invalid, "{ invalid");
                System.Windows.Application program = System.Windows.Application.Current
                    ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                program.Resources["PToolIcon"] = Brushes.Black;
                program.Resources["PIconRoot"] = "pack://application:,,,/";
                string buttonPath = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "../../../../../src/Cadroue.UIVeneer/Kit/Button/PButton.xaml"));
                using (var reader = System.Xml.XmlReader.Create(new StringReader(File.ReadAllText(buttonPath))))
                {
                    program.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(reader));
                }
                resolvers = (Dictionary<bool, Func<string, double, string?>>)typeof(QIcon)
                    .GetField("qIconResolvers", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                resolver = resolvers[true];
                resolvers[true] = (_, _) => null;
                var icons = (System.Collections.Concurrent.ConcurrentDictionary<string, ImageSource>)typeof(QIcon)
                    .GetField("qIconCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                foreach (string icon in new[]
                         {
                             "PSSpectrumLoad.svg", "PSSpectrumSave.svg", "PSSpectrumRemove.svg"
                         })
                {
                    icons[$"pack://application:,,,/PAsset/PPanel/{icon}|{QTool.QToolIconRead()}"] =
                        new DrawingImage();
                }

                string path = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "../../../../../src/Cadroue.UIVeneer/Annex/Spectrum/PSSpectrum.xaml"));
                string contract = File.ReadAllText(path)
                    .Replace(" x:Class=\"Cadroue.UIVeneer.PSSpectrum\"", string.Empty, StringComparison.Ordinal);
                var surface = (UserControl)XamlReader.Parse(contract);
                host = new Window
                {
                    Content = surface,
                    Width = 640,
                    Height = 480,
                    ShowActivated = false,
                    ShowInTaskbar = false
                };
                host.Show();
                int folderReads = 0;
                var spectrum = new QSSpectrum("Cadroue", () =>
                {
                    folderReads++;
                    return folder;
                });
                int loadAnswers = 0;
                int saveAnswers = 0;
                int confirmations = 0;
                bool confirmed = false;
                string? answer = null;
                var warnings = new List<string>();
                var dialog = new QSSpectrumDialog(
                    spectrum,
                    surface,
                    warnings.Add,
                    _ => { confirmations++; return confirmed; },
                    () => { loadAnswers++; return answer; },
                    _ => { saveAnswers++; return null; });

                dialog.QSSpectrumDialogAttach();
                dialog.QSSpectrumDialogAttach();
                surface.Dispatcher.Invoke(
                    () => { },
                    System.Windows.Threading.DispatcherPriority.DataBind);
                var gallery = QContract.QContractFind<StackPanel>(surface, "PSSpectrumGallery");
                Assert.NotEmpty(gallery.Children);
                StackPanel muted = gallery.Children.Cast<StackPanel>()
                    .Single(line => ((QSSpectrumRow)line.DataContext).QSSpectrumRowName == "Muted");
                Border label = QContract.QContractFind<Border>(muted, "PSSpectrumLabel");
                Button remover = QContract.QContractFind<Button>(muted, "PSSpectrumRemover");
                Assert.Equal("Muted", ((TextBlock)((StackPanel)label.Child).Children[0]).Text);
                Assert.Equal(10, ((ItemsControl)((StackPanel)label.Child).Children[1]).Items.Count);
                Assert.Equal(Color.FromRgb(0xFF, 0xFF, 0xFF), ((SolidColorBrush)label.Background).Color);
                Assert.Equal(Color.FromRgb(0xD9, 0xDE, 0xE7), ((SolidColorBrush)label.BorderBrush).Color);
                Assert.Equal(new Thickness(1), label.BorderThickness);
                Assert.Equal(new Thickness(12, 8, 14, 8), label.Padding);

                surface.UpdateLayout();
                var swatches = new List<Border>();
                var pending = new Stack<DependencyObject>();
                pending.Push(((StackPanel)label.Child).Children[1]);
                while (pending.Count > 0)
                {
                    DependencyObject node = pending.Pop();
                    if (node is Border { Width: 20 } swatch)
                    {
                        swatches.Add(swatch);
                    }

                    for (int index = VisualTreeHelper.GetChildrenCount(node) - 1; index >= 0; index--)
                    {
                        pending.Push(VisualTreeHelper.GetChild(node, index));
                    }
                }
                Assert.Equal(
                    ((QSSpectrumRow)muted.DataContext).QSSpectrumRowColors
                        .Select(color => (Color)ColorConverter.ConvertFromString(color)),
                    swatches.Select(swatch => ((SolidColorBrush)swatch.Background).Color));

                MouseDevice mouse = Mouse.PrimaryDevice;
                var mouseOver = (DependencyPropertyKey)typeof(UIElement)
                    .GetField("IsMouseOverPropertyKey", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                label.SetValue(mouseOver, true);
                label.RaiseEvent(new MouseEventArgs(mouse, 0) { RoutedEvent = UIElement.MouseEnterEvent });
                surface.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.DataBind);
                Assert.Equal(Color.FromRgb(0xF7, 0xF9, 0xFC), ((SolidColorBrush)label.Background).Color);
                Assert.Equal(Color.FromRgb(0x4C, 0x86, 0xF7), ((SolidColorBrush)label.BorderBrush).Color);
                Assert.Equal(new Thickness(1), label.BorderThickness);
                Assert.Equal(new Thickness(12, 8, 14, 8), label.Padding);
                label.SetValue(mouseOver, false);
                label.RaiseEvent(new MouseEventArgs(mouse, 0) { RoutedEvent = UIElement.MouseLeaveEvent });
                surface.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.DataBind);
                Assert.Equal(Color.FromRgb(0xFF, 0xFF, 0xFF), ((SolidColorBrush)label.Background).Color);
                Assert.Equal(Color.FromRgb(0xD9, 0xDE, 0xE7), ((SolidColorBrush)label.BorderBrush).Color);
                Assert.Equal(new Thickness(1), label.BorderThickness);
                Assert.Equal(new Thickness(12, 8, 14, 8), label.Padding);

                int readsAfterAttach = folderReads;
                Button loader = QContract.QContractFind<Button>(surface, "PSSpectrumLoader");
                Button saver = QContract.QContractFind<Button>(surface, "PSSpectrumSaver");
                loader.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, loader));
                saver.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, saver));
                Assert.Equal(1, loadAnswers);
                Assert.Equal(1, saveAnswers);
                Assert.Equal(readsAfterAttach, folderReads);

                answer = invalid;
                loader.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, loader));
                Assert.Equal(["Options.Timeline.InvalidPalette"], warnings);
                Assert.Equal(readsAfterAttach, folderReads);

                label.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                    Source = label
                });
                Assert.Equal("Muted", spectrum.QSSpectrumName);
                surface.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.DataBind);
                muted = gallery.Children.Cast<StackPanel>()
                    .Single(line => ((QSSpectrumRow)line.DataContext).QSSpectrumRowName == "Muted");
                label = QContract.QContractFind<Border>(muted, "PSSpectrumLabel");
                remover = QContract.QContractFind<Button>(muted, "PSSpectrumRemover");
                Assert.Equal(Color.FromRgb(0xF7, 0xF9, 0xFC), ((SolidColorBrush)label.Background).Color);
                Assert.Equal(Color.FromRgb(0x4C, 0x86, 0xF7), ((SolidColorBrush)label.BorderBrush).Color);
                Assert.Equal(new Thickness(2), label.BorderThickness);
                Assert.Equal(new Thickness(11, 7, 13, 7), label.Padding);
                Assert.Equal("Muted", ((TextBlock)((StackPanel)label.Child).Children[0]).Text);
                label.SetValue(mouseOver, true);
                label.RaiseEvent(new MouseEventArgs(mouse, 0) { RoutedEvent = UIElement.MouseEnterEvent });
                label.SetValue(mouseOver, false);
                label.RaiseEvent(new MouseEventArgs(mouse, 0) { RoutedEvent = UIElement.MouseLeaveEvent });
                surface.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.DataBind);
                Assert.Equal(Color.FromRgb(0xF7, 0xF9, 0xFC), ((SolidColorBrush)label.Background).Color);
                Assert.Equal(Color.FromRgb(0x4C, 0x86, 0xF7), ((SolidColorBrush)label.BorderBrush).Color);
                Assert.Equal(new Thickness(2), label.BorderThickness);
                Assert.Equal(new Thickness(11, 7, 13, 7), label.Padding);

                spectrum.QSSpectrumSelect("Cadroue");
                surface.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.DataBind);
                muted = gallery.Children.Cast<StackPanel>()
                    .Single(line => ((QSSpectrumRow)line.DataContext).QSSpectrumRowName == "Muted");
                label = QContract.QContractFind<Border>(muted, "PSSpectrumLabel");
                remover = QContract.QContractFind<Button>(muted, "PSSpectrumRemover");
                Assert.Equal(Color.FromRgb(0xFF, 0xFF, 0xFF), ((SolidColorBrush)label.Background).Color);
                Assert.Equal(Color.FromRgb(0xD9, 0xDE, 0xE7), ((SolidColorBrush)label.BorderBrush).Color);
                label.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                    Source = label
                });
                surface.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.DataBind);
                muted = gallery.Children.Cast<StackPanel>()
                    .Single(line => ((QSSpectrumRow)line.DataContext).QSSpectrumRowName == "Muted");
                remover = QContract.QContractFind<Button>(muted, "PSSpectrumRemover");

                var refused = new RoutedEventArgs(Button.ClickEvent, remover);
                remover.RaiseEvent(refused);
                Assert.True(refused.Handled);
                Assert.Equal(1, confirmations);
                Assert.Contains(muted, gallery.Children.Cast<StackPanel>());
                Assert.Equal("Muted", spectrum.QSSpectrumName);

                confirmed = true;
                var removed = new RoutedEventArgs(Button.ClickEvent, remover);
                remover.RaiseEvent(removed);
                Assert.True(removed.Handled);
                Assert.Equal(2, confirmations);
                Assert.DoesNotContain(muted, gallery.Children.Cast<StackPanel>());
                Assert.Equal("Cadroue", spectrum.QSSpectrumName);
                Assert.DoesNotContain(
                    gallery.Children.Cast<StackPanel>(),
                    line => ((QSSpectrumRow)line.DataContext).QSSpectrumRowName == "Muted");

                StackPanel vivid = gallery.Children.Cast<StackPanel>()
                    .Single(line => ((QSSpectrumRow)line.DataContext).QSSpectrumRowName == "Vivid");
                label = QContract.QContractFind<Border>(vivid, "PSSpectrumLabel");
                remover = QContract.QContractFind<Button>(vivid, "PSSpectrumRemover");

                dialog.QSSpectrumDialogDetach();
                Assert.Empty(gallery.Children);
                int answersAfterDetach = loadAnswers;
                spectrum.QSSpectrumSelect("Cadroue");
                loader.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, loader));
                remover.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, remover));
                label.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                    Source = label
                });
                Assert.Equal(answersAfterDetach, loadAnswers);
                Assert.Equal(2, confirmations);
                Assert.Equal("Cadroue", spectrum.QSSpectrumName);

                answer = null;
                dialog.QSSpectrumDialogAttach();
                loader.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, loader));
                Assert.Equal(answersAfterDetach + 1, loadAnswers);
                dialog.QSSpectrumDialogDetach();
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
                host?.Close();
                File.Delete(invalid);
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void QSSpectrumImport()
    {
        string folder = Path.Combine(
            Path.GetTempPath(), "Cadroue.Tests", "SpectrumImport", Guid.NewGuid().ToString("N"));
        string invalid = Path.Combine(Path.GetTempPath(), $"spectrum-invalid-{Guid.NewGuid():N}.json");
        string tSource = Path.Combine(Path.GetTempPath(), $"spectrum-outside-{Guid.NewGuid():N}.json");
        Directory.CreateDirectory(folder);
        File.WriteAllText(invalid, "{ invalid");
        File.WriteAllText(tSource, "{\"Name\":\"Outside\",\"Colors\":[\"#123456\"]}");
        int folderReads = 0;
        var spectrum = new QSSpectrum("Cadroue", () =>
        {
            folderReads++;
            return folder;
        });
        try
        {
            Assert.Equal("Options.Timeline.InvalidPalette", spectrum.QSSpectrumImport(invalid));
            Assert.Equal(0, folderReads);
            Assert.Null(spectrum.QSSpectrumImport(tSource));
            Assert.Equal("Outside", spectrum.QSSpectrumName);

            string reserved = Path.Combine(folder, "Reserved.json");
            File.WriteAllText(reserved, "{\"Name\":\".hidden\",\"Colors\":[\"#123456\"]}");
            Assert.Equal("Options.Timeline.ReservedPalette", spectrum.QSSpectrumImport(reserved));

            string loaded = Path.Combine(folder, "Loaded.json");
            File.WriteAllText(loaded, "{\"Name\":\"Loaded\",\"Colors\":[\"#123456\"]}");
            spectrum.QSSpectrumUpdate();

            Assert.Null(spectrum.QSSpectrumImport(loaded));
            Assert.Equal("Loaded", spectrum.QSSpectrumName);

            string blocked = Path.Combine(folder, "blocked");
            File.WriteAllText(blocked, "occupied");
            string source = Path.Combine(folder, "Source.json");
            File.WriteAllText(source, "{\"Name\":\"Source\",\"Colors\":[\"#654321\"]}");
            var failed = new QSSpectrum("Cadroue", () => blocked);
            Assert.Equal("Options.Timeline.PaletteCopyFailed", failed.QSSpectrumImport(source));
        }
        finally
        {
            File.Delete(invalid);
            File.Delete(tSource);
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void QSSpectrumSave()
    {
        string folder = Path.Combine(
            Path.GetTempPath(), "Cadroue.Tests", "SpectrumSave", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var spectrum = new QSSpectrum("Cadroue", () => folder);
        try
        {
            spectrum.QSSpectrumUpdate();
            string saved = Path.Combine(folder, "Saved.json");
            spectrum.QSSpectrumSave(saved);
            Assert.True(File.Exists(saved));
            Assert.Contains("\"Name\": \"Cadroue\"", File.ReadAllText(saved), StringComparison.Ordinal);

            string blocked = Path.Combine(folder, "blocked");
            File.WriteAllText(blocked, "occupied");
            string refused = Path.Combine(blocked, "Refused.json");
            spectrum.QSSpectrumSave(refused);
            Assert.False(File.Exists(refused));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void QSSpectrumRemove()
    {
        string folder = Path.Combine(
            Path.GetTempPath(), "Cadroue.Tests", "SpectrumRemove", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string custom = Path.Combine(folder, "Custom.json");
        File.WriteAllText(custom, "{\"Name\":\"Custom\",\"Colors\":[\"#123456\"]}");
        var spectrum = new QSSpectrum("Custom", () => folder);
        try
        {
            spectrum.QSSpectrumUpdate();
            Assert.False(spectrum.QSSpectrumRemove("Custom", _ => false));
            Assert.True(File.Exists(custom));
            Assert.Equal("Custom", spectrum.QSSpectrumName);

            using (File.Open(custom, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.False(spectrum.QSSpectrumRemove("Custom", _ => true));
                Assert.Equal("Custom", spectrum.QSSpectrumName);
            }

            Assert.True(spectrum.QSSpectrumRemove("Custom", _ => true));
            Assert.False(File.Exists(custom));
            Assert.Equal("Cadroue", spectrum.QSSpectrumName);

            spectrum.QSSpectrumSelect("Muted");
            Directory.Delete(folder, true);
            File.WriteAllText(folder, "occupied");
            Assert.True(spectrum.QSSpectrumRemove("Muted", _ => true));
            Assert.Equal("Cadroue", spectrum.QSSpectrumName);
            // The row is still listed: a refused removal of a missing row would throw.
            Assert.False(spectrum.QSSpectrumRemove("Muted", _ => false));
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
            else
            {
                File.Delete(folder);
            }
        }
    }
}
