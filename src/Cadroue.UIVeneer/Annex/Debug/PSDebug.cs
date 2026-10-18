using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSDebug : Window
{
    private const double PSDebugWidth = 780;
    private const double PSDebugHeight = 640;

    private static PSDebug? psDebugCurrent;

    private readonly PWindow psDebugApplicationOwner;
    private readonly QLogoMenu qLogoMenu;

    internal static void PSDebugShow(Window pOwner, QLogoMenu qLogoMenuOwner)
    {
        if (psDebugCurrent is not null)
        {
            psDebugCurrent.Activate();
            return;
        }

        var pDebug = new PSDebug(pOwner, qLogoMenuOwner);
        PSDebugCurrentSet(pDebug);
        pDebug.Show();
    }

    private static void PSDebugCurrentSet(PSDebug? pDebug) => psDebugCurrent = pDebug;

    private PSDebug(Window pOwner, QLogoMenu qLogoMenuOwner)
    {
        psDebugApplicationOwner = (PWindow)pOwner;
        qLogoMenu = qLogoMenuOwner;
        PSDebugTitleSet();
        PSDebugOwnerApply(pOwner);
        PSDebugFrameApply();
        PSDebugBuild();
        PSDebugClosedAttach();
    }

    private void PSDebugTitleSet() => Title = "Debug";

    private void PSDebugOwnerApply(Window pOwner) => Owner = pOwner;

    private void PSDebugFrameApply()
    {
        Width = PSDebugWidth;
        Height = PSDebugHeight;
        MinWidth = 520;
        MinHeight = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;
        FontSize = PSField.PSFieldFontSize;
    }

    private void PSDebugClosedAttach() => Closed += (_, _) => PSDebugCurrentSet(null);

    private void PSDebugBuild()
    {
        var pRoot = new StackPanel { Margin = new Thickness(18) };
        pRoot.Children.Add(PSDebugHeadingBuild("Subwindows"));

        var pWindows = PSDebugWrapBuild();
        PSDebugButtonAdd(pWindows, "About", () => PSAbout.PSAboutShow(this, qLogoMenu.QLogoAboutCreate()));
        PSDebugButtonAdd(
            pWindows, "Options", () => PSOptions.PSOptionsShow(this, qLogoMenu.QLogoOptionsCreate(), null));
        PSDebugButtonAdd(pWindows, "Shortcuts", () => PSKeymap.PSKeymapShow(this, qLogoMenu.QLogoKeymapCreate(), null));
        PSDebugButtonAdd(
            pWindows,
            "Log",
            () => PLogWindow.PLogWindowShow(psDebugApplicationOwner, qLogoMenu.QLogoLogCreate));
        PSDebugButtonAdd(
            pWindows,
            "Diagnosis",
            () => PSDiagnosis.PSDiagnosisShow(this, () => qLogoMenu.QLogoOptionsCreate().QSOptionsDiagnosisCreate()));
        PSDebugButtonAdd(pWindows, "Encoder", PSDebugEncoderShow);
        PSDebugButtonAdd(pWindows, "Verdict", () => PSVerdict.PSVerdictShow(this, qLogoMenu.QLogoVerdictCreate()));
        PSDebugButtonAdd(
            pWindows, "Loupe", () => PSLoupe.PSLoupeShow(
                this,
                psDebugApplicationOwner.QWindow.QWindowPairCreate().QWorkspaceViewer
                    .QViewerMedia.QViewerLoupe.QViewerLoupeCreate()));
        PSDebugButtonAdd(pWindows, "Monitor", PSDebugMonitorShow);
        PSDebugButtonAdd(pWindows, "Destructive alert", () => PSAlert.PSAlertConfirm(
            this,
            "Debug alert",
            "Exercise the custom destructive confirmation window?",
            "Confirm"));
        PSDebugButtonAdd(pWindows, "Decision", () => PSDecision.PSDecisionSelect(
            this,
            "Debug decision",
            "Exercise the neutral multi-choice decision window?",
            "Primary",
            "Alternate",
            "Cancel"));
        pRoot.Children.Add(pWindows);

        pRoot.Children.Add(PSDebugHeadingBuild("Message boxes"));
        var pMessages = PSDebugWrapBuild();
        foreach (PSDebugMessage pMessage in PSDebugMessage.PSDebugMessages)
        {
            PSDebugMessage pCurrent = pMessage;
            PSDebugButtonAdd(pMessages, pCurrent.PSDebugName, () => pCurrent.PSDebugMessageShow(this));
        }
        pRoot.Children.Add(pMessages);

        Content = new ScrollViewer
        {
            Content = pRoot,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
    }

    private void PSDebugEncoderShow()
    {
        if (psDebugApplicationOwner.PStrip.QStrip.QStripSelected is not { } lTab)
        {
            return;
        }

        var pEncoder = new PSEncoder(
            lTab.QStripTabWorkspace.QWorkspaceExport.QExportEncoderCreate(new LPreset()), () => { }) { Owner = this };
        pEncoder.ShowDialog();
    }

    private void PSDebugMonitorShow()
    {
        QSMonitor pSource = qLogoMenu.QLogoMonitorCreate();
        QWorkspacePair qPair = psDebugApplicationOwner.QWindow.QWindowPairCreate();
        var pFlow = new PFlow(qPair.QWorkspaceFlow);
        var pViewer = new PViewer(qPair.QWorkspaceViewer);
        Window pMonitor = PSMonitor.PSMonitorShow(Owner, pSource, pFlow, pViewer);
        PSDebugMonitorAttach(pMonitor, pSource);
    }

    private static void PSDebugMonitorAttach(Window pMonitor, QSMonitor pSource) =>
        pMonitor.Closed += (_, _) => pSource.Dispose();

    private static TextBlock PSDebugHeadingBuild(string pText) => new()
    {
        Text = pText,
        FontSize = 17,
        FontWeight = FontWeights.SemiBold,
        Foreground = PSField.PSFieldText,
        Margin = new Thickness(0, 4, 0, 10)
    };

    private static WrapPanel PSDebugWrapBuild() => new()
    {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(0, 0, 0, 18)
    };

    private static void PSDebugButtonAdd(Panel pPanel, string pText, Action pAction)
    {
        Button pButton = PSDebugButtonBuild(pText);
        PSDebugClickAttach(pButton, pAction);
        pPanel.Children.Add(pButton);
    }

    private static Button PSDebugButtonBuild(string pText) => new()
    {
        Content = pText,
        MinWidth = 150,
        MinHeight = 34,
        Margin = new Thickness(0, 0, 8, 8),
        Padding = new Thickness(10, 5, 10, 5),
        HorizontalContentAlignment = HorizontalAlignment.Left
    };

    private static void PSDebugClickAttach(Button pButton, Action pAction) => pButton.Click += (_, _) => pAction();
}
