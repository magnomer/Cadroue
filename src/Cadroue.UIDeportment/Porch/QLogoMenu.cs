using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QLogoMenu
{
    private readonly CUsher qLogoMenuUsher;
    private readonly Func<CSAbout> qLogoMenuAbout;

    private FrameworkElement qLogoMenuSurface = null!;
    private QChrome qLogoMenuChrome = null!;
    private QLogoAnswer qLogoMenuAnswer = null!;
    private Action? qLogoMenuApplied;

    public QLogoMenu(CUsher qUsher, Func<CSAbout> qAbout)
    {
        qLogoMenuUsher = qUsher;
        qLogoMenuAbout = qAbout;
    }

    public QLog QLogoLogCreate() => new(qLogoMenuUsher);

    public QSAbout QLogoAboutCreate(
        Func<Window> surface,
        Func<Window, string?, FrameworkElement, UIElement> shell,
        Func<Window, (Action qsAboutGrabberAttachment, Action qsAboutGrabberDetachment)> grabber,
        Action<Window?, string, string> warning,
        Action<Window, string> restore,
        Action<Window, string> save) =>
        new(qLogoMenuAbout(), surface, shell, grabber, warning, restore, save);

    public QSOptions QLogoOptionsCreate() => new(qLogoMenuUsher);

    public QSKeymap QLogoKeymapCreate() => new();

    public QSVerdict QLogoVerdictCreate() => new("Debug verdict", new[]
    {
        new QSVerdictRow("Video", "debug-pass", true, "Simulated successful verification."),
        new QSVerdictRow("Audio", "debug-fail", false, "Simulated verification detail.")
    });

    public QSMonitor QLogoMonitorCreate() => new();

    public void QLogoMenuAttach(
        FrameworkElement surface,
        QChrome chrome,
        QLogoAnswer answer,
        Action? applied)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(chrome);
        ArgumentNullException.ThrowIfNull(answer);

        qLogoMenuSurface = surface;
        FrameworkElement logo = QContract.QContractFind<FrameworkElement>(surface, "PLogo");
        qLogoMenuChrome = chrome;
        qLogoMenuAnswer = answer;
        qLogoMenuApplied = applied;
        surface.PreviewMouseLeftButtonUp += (_, e) => QLogoMenuHandle(e, logo);
    }

    internal ContextMenu QLogoMenuBuild()
    {
        Window owner = Window.GetWindow(qLogoMenuSurface)!;
        FrameworkElement logo = QContract.QContractFind<FrameworkElement>(qLogoMenuSurface, "PLogo");
        ContextMenu menu = QMenu.QMenuBuild(logo, QMenuSide.QMenuSideBelow);
        (string, string, Action<Window>)[] rows =
        [
            ("Chrome.Menu.Options", "/PAsset/PMenu/PMenuPreferences.svg",
                window => qLogoMenuAnswer.QLogoAnswerOptions(window, qLogoMenuApplied)),
            ("Chrome.Menu.Shortcuts", "/PAsset/PMenu/PMenuShortcuts.svg",
                window => qLogoMenuAnswer.QLogoAnswerShortcut(window, qLogoMenuApplied)),
            ("Chrome.Menu.Log", "/PAsset/PMenu/PMenuLog.svg", qLogoMenuAnswer.QLogoAnswerLog),
            ("Chrome.Menu.About", "/PAsset/PMenu/PMenuAbout.svg", qLogoMenuAnswer.QLogoAnswerAbout),
        ];

        foreach (var row in rows)
        {
            MenuItem item = QMenu.QMenuItemBuild(
                QLocalizationCatalog.QLocalizationTextRead(row.Item1),
                QMenu.QMenuIconRead(row.Item2));
            item.Click += (_, _) => row.Item3(owner);
            menu.Items.Add(item);
        }

        MenuItem debug = QMenu.QMenuItemBuild("Debug");
        debug.Visibility = QChrome.QChromeDebugVisible ? Visibility.Visible : Visibility.Collapsed;
        debug.Click += (_, _) => qLogoMenuAnswer.QLogoAnswerDebug(owner, qLogoMenuAnswer);
        menu.Items.Add(debug);

        MenuItem exit = QMenu.QMenuItemBuild(
            QLocalizationCatalog.QLocalizationTextRead("Chrome.Menu.Exit"),
            QMenu.QMenuIconRead("/PAsset/PMenu/PMenuExit.svg"));
        exit.Click += (_, _) => owner.Close();
        menu.Items.Add(exit);
        return menu;
    }

    private void QLogoMenuShow() => QLogoMenuBuild().IsOpen = true;

    private void QLogoMenuHandle(MouseButtonEventArgs e, FrameworkElement logo)
    {
        bool released = QWalk.QWalkParentCheck(
            e.OriginalSource as DependencyObject,
            node => ReferenceEquals(node, logo));
        if (qLogoMenuChrome.QChromeReleaseResolve(released))
        {
            QLogoMenuShow();
        }
    }
}
