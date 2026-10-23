using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection(nameof(TProgramCollection))]
public sealed class TLogoMenu
{
    [Fact]
    public void Build_PresentsAcceptedOrderTextIconsAndDebugVisibility()
    {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            Window? owner = null;
            try
            {
                System.Windows.Application program = System.Windows.Application.Current
                    ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                program.Resources["PIconRoot"] =
                    "pack://application:,,,/Cadroue.UIVeneer;component/";
                if (program.TryFindResource("PMenu") is null)
                {
                    program.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri(
                            "pack://application:,,,/Cadroue.UIVeneer;component/Kit/Menu/PMenu.xaml",
                            UriKind.Absolute),
                    });
                }

                List<string> actions = [];
                Action applied = () => actions.Add("Applied");
                QLogoAnswer? expected = null;
                expected = new QLogoAnswer(
                    actualOwner =>
                    {
                        Assert.Same(owner, actualOwner);
                        actions.Add("About");
                    },
                    (actualOwner, callback) =>
                    {
                        Assert.Same(owner, actualOwner);
                        Assert.Same(applied, callback);
                        actions.Add("Options");
                    },
                    (actualOwner, callback) =>
                    {
                        Assert.Same(owner, actualOwner);
                        Assert.Same(applied, callback);
                        actions.Add("Shortcuts");
                    },
                    actualOwner =>
                    {
                        Assert.Same(owner, actualOwner);
                        actions.Add("Log");
                    },
                    (actualOwner, answer) =>
                    {
                        Assert.Same(owner, actualOwner);
                        Assert.Same(expected, answer);
                        actions.Add("Debug");
                    });

                Grid surface = new();
                Grid logo = new() { Name = "PLogo" };
                NameScope.SetNameScope(surface, new NameScope());
                surface.Children.Add(logo);
                surface.RegisterName("PLogo", logo);
                owner = new Window
                {
                    Content = surface,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None,
                };
                owner.Closed += (_, _) => actions.Add("Exit");
                owner.Show();

                QLogoMenu menuDriver = TInterface.TWindowCreate(body => body()).QWindowLogo;
                QChrome chrome = new();
                menuDriver.QLogoMenuAttach(surface, chrome, expected, applied);
                ContextMenu menu = menuDriver.QLogoMenuBuild();
                MenuItem[] items = [.. menu.Items.Cast<MenuItem>()];
                Assert.NotSame(menu, menuDriver.QLogoMenuBuild());

                Assert.Equal(
                    [
                        TInterface.TLocalizationCatalogRead("Chrome.Menu.Options"),
                        TInterface.TLocalizationCatalogRead("Chrome.Menu.Shortcuts"),
                        TInterface.TLocalizationCatalogRead("Chrome.Menu.Log"),
                        TInterface.TLocalizationCatalogRead("Chrome.Menu.About"),
                        "Debug",
                        TInterface.TLocalizationCatalogRead("Chrome.Menu.Exit"),
                    ],
                    items.Select(item => Assert.IsType<string>(item.Header)));
                Assert.All(items.Where((_, index) => index != 4), item =>
                    Assert.NotNull(Assert.IsType<Image>(item.Icon).Source));
                Assert.Null(items[4].Icon);
                Assert.Equal(
                    QChrome.QChromeDebugVisible ? Visibility.Visible : Visibility.Collapsed,
                    items[4].Visibility);

                chrome.QChromePressHandle(true, 0, 0);
                logo.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = Mouse.PreviewMouseUpEvent,
                });
                Assert.False(chrome.QChromeArmed);

                foreach (MenuItem item in items)
                {
                    item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, item));
                }

                Assert.Equal(["Options", "Shortcuts", "Log", "About", "Debug", "Exit"], actions);
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                if (owner?.IsVisible == true)
                {
                    owner.Close();
                }
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
