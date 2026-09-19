using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PPorch;

namespace Cadroue.UIVeneer.PCabin;

public sealed record PActionRelayOption(Guid PActionRelayId, string PActionRelayTitle, ImageSource? PActionRelayIcon);

public sealed partial class PAction : UserControl
{
    private static readonly Brush pActionPositiveBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x64));
    private static readonly Brush pActionNegativeBrush = new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x45));
    private static readonly Brush pActionRelayText = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private readonly Button pActionAllButton;
    private readonly Button pActionRelayButton;
    private readonly CheckBox pActionAutoBox;
    private readonly Image pActionRelayIcon;
    private readonly TextBlock pActionRelayLabel;

    public event Action<LWorkPriority>? PActionRun;
    public event Action? PActionAllAdd;
    public event Func<Guid, int>? PActionCohortAdd;
    public event Action<IReadOnlyList<string>>? PActionItemsAdd;
    public event Action<Guid>? PActionRelayChange;

    public PAction()
    {
        var pPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Button pAddListButton = PActionButtonBuild("AddList", "PActionAddList.svg", "Action.AddList");
        pActionAllButton = PActionButtonBuild("AddAll", "PActionAddAll.svg", "Action.AddAll");
        Button pExecuteButton = PActionButtonBuild("Execute", "PActionExecute.svg", "Action.Execute");
        pAddListButton.Click += (_, _) => PActionListAdd();
        pActionAllButton.Click += (_, _) => PActionAllRun();
        pExecuteButton.Click += (_, _) => PActionHighRun();
        pActionAllButton.ToolTip = LLocalization.LLocalizationTextRead("Action.AddAll.Tooltip");
        pActionRelayIcon = PActionIconBuild();
        pActionRelayLabel = PActionLabelBuild();
        pActionRelayButton = PActionRelayBuild();
        pActionAutoBox = PActionAutoBuild();
        pPanel.Children.Add(pAddListButton);
        pPanel.Children.Add(new Border { Width = 2 });
        pPanel.Children.Add(pActionAllButton);
        pPanel.Children.Add(new Border { Width = 2 });
        pPanel.Children.Add(pExecuteButton);
        pPanel.Children.Add(new Border { Width = 12 });
        pPanel.Children.Add(pActionRelayButton);
        pPanel.Children.Add(new Border { Width = 6 });
        pPanel.Children.Add(pActionAutoBox);
        Content = new Border { Child = pPanel };
    }

    public Guid PActionRelayTarget { get; private set; }

    public Guid PActionSourceTab { get; set; }

    public Func<IReadOnlyList<PActionRelayOption>>? PActionRelaySource { get; set; }

    public Func<IReadOnlyList<string>>? PActionSelectionSource { get; set; }

    public Func<IReadOnlyList<string>>? PActionEligibleSource { get; set; }

    public void PActionListAttach(PWing.PList pActionList)
    {
        PActionSelectionSource = pActionList.PListSelectionRead;
        PActionEligibleSource = () => pActionList.PListUnlockedRead()
            .Select(pActionItem => pActionItem.LDocketEntryPath)
            .ToArray();
    }

    public void PActionRelayAttach(LStripTab lStripTab)
    {
        Guid pActionSourceTab = lStripTab.LStripTabId;
        lStripTab.LStripActionAttach(PActionAutoCheck, PActionCohortRun);
        LCartographer.LCartographerStart();
        PActionSourceTab = pActionSourceTab;
        PActionRelaySource = () => PStrip.PStripRelayRead(pActionSourceTab);
        PActionRelayChange += pActionTarget =>
        {
            LCartographer.LCartographerTargetSet(pActionSourceTab, pActionTarget);
            PActionRelayApply(LCartographer.LCartographerTargetRead(pActionSourceTab));
        };
        PActionRelayApply(LCartographer.LCartographerTargetRead(pActionSourceTab));
    }

    public static void PActionAccept(Guid pActionTargetTab, string pActionPath, Guid pActionCohort)
    {
        if (PActionAutoFind(pActionTargetTab) is null)
        {
            return;
        }

        LSeal.LSealPendingAdd(pActionCohort);
        void PActionAcceptRun()
        {
            try
            {
                if (PActionAutoFind(pActionTargetTab) is { } pActionSurface)
                {
                    pActionSurface.PActionItemsRun(new[] { pActionPath });
                }
                else
                {
                    LTraceLog.LTraceWarningRecord(
                        $"Relay left '{System.IO.Path.GetFileName(pActionPath)}' unprocessed: "
                        + "the destination tab closed or left Auto Relay before it ran");
                }
            }
            finally
            {
                LSeal.LSealPendingRemove(pActionCohort);
                LSeal.LSealRun();
            }
        }

        if (System.Windows.Application.Current?.Dispatcher is { } pActionDispatcher)
        {
            pActionDispatcher.BeginInvoke(new Action(PActionAcceptRun));
        }
        else
        {
            PActionAcceptRun();
        }
    }

    private static PAction? PActionAutoFind(Guid pActionTargetTab) =>
        PStrip.PStripTabFind(pActionTargetTab) is { } pActionTarget
            && pActionTarget.PTabWorkspace.PWorkspaceSurface is not PMergeTab
            && pActionTarget.PTabWorkspace.PWorkspaceSurface.PTabAction is { PActionAutoRelay: true } pActionSurface
            ? pActionSurface
            : null;

    public void PActionRelayHide()
    {
        pActionRelayButton.Visibility = Visibility.Collapsed;
    }

    public bool PActionAutoRelay => pActionAutoBox.IsChecked == true;

    private bool PActionAutoCheck() => PActionAutoRelay;

    public void PActionAutoApply(bool pActionAutoRelay)
    {
        pActionAutoBox.IsChecked = pActionAutoRelay;
    }

    public void PActionAllRun()
    {
        if (PActionEligibleCheck(false))
        {
            PActionAllAdd?.Invoke();
        }
    }

    public void PActionItemsRun(IReadOnlyList<string> pActionPaths) => PActionItemsAdd?.Invoke(pActionPaths);

    public bool PActionCohortRun(Guid pActionCohort) =>
        PActionAutoRelay && PActionCohortAdd?.Invoke(pActionCohort) > 0;

    private void PActionHighRun()
    {
        if (PActionEligibleCheck(true))
        {
            PActionRun?.Invoke(LWorkPriority.LWorkPriorityHigh);
        }
    }

    private void PActionListAdd()
    {
        if (!PActionEligibleCheck(true))
        {
            return;
        }

        if (PActionSelectionSource?.Invoke() is { Count: > 0 } pActionSelected)
        {
            PActionItemsAdd?.Invoke(pActionSelected);
            return;
        }

        PActionRun?.Invoke(LWorkPriority.LWorkPriorityNormal);
    }

    private bool PActionEligibleCheck(bool pActionSelected)
    {
        if (PActionEligibleSource is not { } pActionEligibleSource)
        {
            return true;
        }

        IReadOnlyList<string> pActionEligible = pActionEligibleSource();
        IReadOnlyList<string> pActionChosen = pActionSelected
            ? PActionSelectionSource?.Invoke() ?? Array.Empty<string>()
            : Array.Empty<string>();
        bool pActionAllowed = pActionChosen.Count == 0
            ? pActionEligible.Count > 0
            : pActionChosen.Any(pActionPath => pActionEligible.Contains(pActionPath, StringComparer.OrdinalIgnoreCase));
        if (!pActionAllowed)
        {
            PActionEmptyShow();
        }

        return pActionAllowed;
    }

    public static void PActionEmptyShow() => PSWarning.PSWarningShow(
        null,
        LLocalization.LLocalizationTextRead("Action.Empty.Title"),
        LLocalization.LLocalizationTextRead("Action.Empty.Body"));

    private CheckBox PActionAutoBuild()
    {
        var pAutoBox = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Action.AutoRelay.Label"),
            FontSize = 12,
            Foreground = pActionRelayText,
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0),
            FocusVisualStyle = null,
            ToolTip = LLocalization.LLocalizationTextRead("Action.AutoRelay.Tooltip")
        };
        PHouse.PCheckbox.PCheckboxApply(pAutoBox);
        return pAutoBox;
    }

    public void PActionAllSet(bool pActionAllAllowed, string pActionAllTooltip)
    {
        pActionAllButton.IsEnabled = pActionAllAllowed;
        pActionAllButton.Opacity = pActionAllAllowed ? 1 : 0.35;
        pActionAllButton.ToolTip = pActionAllTooltip;
    }

    private static Button PActionButtonBuild(string pActionToken, string pIconAssetName, string pLabelKey)
    {
        string pLabelText = LLocalization.LLocalizationTextRead(pLabelKey);
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(new Image
        {
            Source = PIcon.PIconRead($"/PAsset/PCompass/{pIconAssetName}", PActionAccentRead(pActionToken)),
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        pStack.Children.Add(new Border { Height = 1 });
        pStack.Children.Add(new TextBlock
        {
            Text = pLabelText,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(0x0D, 0x1B, 0x2F))
        });

        return new Button
        {
            Width = 58,
            Height = 58,
            Content = pStack,
            Style = PHouse.PButton.PButtonCommandCreate(),
            ToolTip = pLabelText
        };
    }

    private static Brush? PActionAccentRead(string pActionToken) => pActionToken switch
    {
        "AddList" => pActionPositiveBrush,
        "AddAll" => pActionPositiveBrush,
        "Execute" => pActionNegativeBrush,
        _ => null
    };
}
