using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PProcessing : PPanel
{
    private static readonly FontFamily pProcessingFontFamily = new("Segoe UI");
    private static readonly Brush pProcessingSelectBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB));
    private static readonly Brush pProcessingIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pProcessingTextBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly Brush pProcessingActiveBrush = new SolidColorBrush(Color.FromRgb(0x2C, 0x6C, 0xCE));
    private static readonly Brush pProcessingLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));

    private const string PProcessingUpIcon = "/PAsset/PPanel/PProcessingUp.svg";
    private const string PProcessingDownIcon = "/PAsset/PPanel/PProcessingDown.svg";
    private const string PProcessingMonitorIcon = "/PAsset/PPanel/PProcessingViewer.svg";
    private const string PProcessingSkipIcon = "/PAsset/PPanel/PProcessingSkip.svg";

    public const double PProcessingStripWidth = 48;

    public event Action<string?>? PProcessingStepChange;
    public event Action<string>? PProcessingStepOpen;
    public event Action<bool>? PProcessingMinimizeChange;
    public event Action? PProcessingOrderChange;
    public event Action? PProcessingMonitorShow;

    private readonly StackPanel pProcessingRowPanel;
    private readonly UIElement pProcessingFullBody;
    private readonly UIElement pProcessingStripBody;
    private readonly UIElement pProcessingActionBar;
    private readonly Border pProcessingSkipRow;
    private readonly Dictionary<string, Border> pProcessingRows = new(StringComparer.Ordinal);

    public LProcessing LProcessing { get; } = new();

    public PProcessing() : base("")
    {
        LProcessing.LProcessingChange += PProcessingUpdate;
        LProcessing.LProcessingOrderChange += PProcessingOrderUpdate;
        LProcessing.LProcessingMinimizeChange += PProcessingMinimizeHandle;
        LProcessing.LProcessingStepChange += PProcessingStepHandle;
        UIElement pHeader = PProcessingHeaderBuild();

        pProcessingRowPanel = new StackPanel();
        pProcessingRowPanel.PreviewMouseMove += PProcessingMoveHandle;
        pProcessingRowPanel.MouseLeftButtonUp += PProcessingUpHandle;
        pProcessingRowPanel.LostMouseCapture += PProcessingLostHandle;

        var pScroll = new ScrollViewer
        {
            Content = pProcessingRowPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        pProcessingActionBar = PProcessingActionBuild();
        pProcessingActionBar.Visibility = Visibility.Collapsed;
        pProcessingSkipRow = PProcessingSkipBuild();

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pRoot.Children.Add(pHeader);
        DockPanel.SetDock(pProcessingActionBar, Dock.Bottom);
        pRoot.Children.Add(pProcessingActionBar);
        DockPanel.SetDock(pProcessingSkipRow, Dock.Bottom);
        pRoot.Children.Add(pProcessingSkipRow);
        pRoot.Children.Add(pScroll);

        pProcessingFullBody = pRoot;
        pProcessingStripBody = PProcessingStripBuild();
        pProcessingStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pProcessingFullBody);
        pBodyHost.Children.Add(pProcessingStripBody);

        FocusVisualStyle = null;
        Content = PPanelBorderBuild(pBodyHost);
    }

    private void PProcessingStepHandle(string pStepName)
    {
        PProcessingStepChange?.Invoke(pStepName);
        PProcessingStepOpen?.Invoke(pStepName);
    }
}
