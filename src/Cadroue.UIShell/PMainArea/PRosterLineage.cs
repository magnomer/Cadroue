using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    internal const double PRosterLineageIndent = 16;

    private readonly Dictionary<Guid, ListBoxItem> pRosterLineageRows = new();
    private readonly Dictionary<Guid, TextBlock> pRosterLineageLabels = new();

    private sealed class PRosterLineageEntry
    {
        public required Guid PRosterLineageId { get; init; }
        public required string PRosterLineageSubject { get; init; }
        public required List<LWorkItem> PRosterLineageItems { get; init; }
        public long? PLineageOriginBytes { get; set; }
    }

    private IReadOnlyList<PRosterLineageEntry> PRosterLineageRead(IReadOnlyList<LWorkItem> pWorkItems)
    {
        var pConsumedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem pWorkItem in pWorkItems)
        {
            if (PLineagePathRead(pWorkItem.LWorkSourcePath) is { } pSourceKey)
            {
                pConsumedPaths.Add(pSourceKey);
            }
        }

        var pLineageOrder = new List<PRosterLineageEntry>();
        var pLineageIndex = new Dictionary<Guid, PRosterLineageEntry>();

        foreach (LWorkItem pWorkItem in pWorkItems)
        {
            Guid pLineageId = PLineageKeyRead(pWorkItem, pConsumedPaths);
            if (!pLineageIndex.TryGetValue(pLineageId, out PRosterLineageEntry? pLineageEntry))
            {
                pLineageEntry = new PRosterLineageEntry
                {
                    PRosterLineageId = pLineageId,
                    PRosterLineageSubject = PLineageSubjectRead(pWorkItem, pLineageId),
                    PRosterLineageItems = new List<LWorkItem>()
                };
                pLineageIndex[pLineageId] = pLineageEntry;
                pLineageOrder.Add(pLineageEntry);
            }

            pLineageEntry.PRosterLineageItems.Add(pWorkItem);
        }

        foreach (PRosterLineageEntry pLineageEntry in pLineageOrder)
        {
            pLineageEntry.PLineageOriginBytes = PLineageOriginRead(pLineageEntry, pWorkItems);
        }

        return pLineageOrder;
    }

    private Guid PLineageKeyRead(LWorkItem pWorkItem, HashSet<string> pConsumedPaths)
    {
        if (pWorkItem.LWorkKind == LWorkKind.LWorkKindSplit
            && PLineagePathRead(pWorkItem.LWorkOutputPath) is { } pOutputKey
            && pConsumedPaths.Contains(pOutputKey))
        {
            return LSchedule.LScheduleFileRead(pWorkItem.LWorkOutputPath);
        }

        return pRosterSchedule.LScheduleLineageRead(pWorkItem);
    }

    private static string PLineageSubjectRead(LWorkItem pLineageFirst, Guid pLineageId)
    {
        if (pLineageFirst.LWorkKind == LWorkKind.LWorkKindMerge
            || LSchedule.LScheduleFileRead(pLineageFirst.LWorkOutputPath) == pLineageId)
        {
            return pLineageFirst.LWorkOutputPath;
        }

        return pLineageFirst.LWorkSourcePath;
    }

    private static long? PLineageOriginRead(
        PRosterLineageEntry pLineageEntry,
        IReadOnlyList<LWorkItem> pWorkItems)
    {
        string pSubject = pLineageEntry.PRosterLineageSubject;
        foreach (LWorkItem pWorkItem in pWorkItems)
        {
            if (PRosterLineageMatch(pWorkItem.LWorkOutputPath, pSubject) && pWorkItem.LWorkOutputBytes is { } pOutput)
            {
                return pOutput;
            }

            if (PRosterLineageMatch(pWorkItem.LWorkSourcePath, pSubject) && pWorkItem.LWorkSourceBytes is { } pSource)
            {
                return pSource;
            }
        }

        try
        {
            return File.Exists(pSubject) ? new FileInfo(pSubject).Length : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string PLineageStepRead(LWorkItem pWorkItem, string pSubject, bool pLineageFirstRow)
    {
        if (pLineageFirstRow && !PRosterLineageMatch(pWorkItem.LWorkSourcePath, pSubject))
        {
            string pFromName = PLineageFileRead(pWorkItem.LWorkSourcePath);
            int pExtraCount = pWorkItem.LWorkMergeSources.Count > 1 ? pWorkItem.LWorkMergeSources.Count - 1 : 0;
            return pExtraCount > 0
                ? LLocalization.LLocalizationFormat("Roster.Lineage.FromMore", pFromName, pExtraCount)
                : LLocalization.LLocalizationFormat("Roster.Lineage.From", pFromName);
        }

        if (pWorkItem.LWorkKind == LWorkKind.LWorkKindSplit
            && !PRosterLineageMatch(pWorkItem.LWorkOutputPath, pSubject))
        {
            return LLocalization.LLocalizationFormat(
                "Roster.Lineage.Split", PLineageFileRead(pWorkItem.LWorkOutputPath));
        }

        return LLocalization.LLocalizationFormat("Roster.Lineage.Step", PLineageKindRead(pWorkItem.LWorkKind));
    }

    private static string PLineageKindRead(LWorkKind pWorkKind) =>
        LLocalization.LLocalizationTextRead(pWorkKind switch
        {
            LWorkKind.LWorkKindEdit => "Roster.Kind.Edit",
            LWorkKind.LWorkKindAudio => "Roster.Kind.Audio",
            LWorkKind.LWorkKindConvert => "Roster.Kind.Convert",
            LWorkKind.LWorkKindMerge => "Roster.Kind.Merge",
            _ => "Roster.Kind.Split"
        });

    private static string PLineageRatioFormat(LWorkItem pWorkItem, string pSubject, long? pOriginBytes)
    {
        if (PRosterLineageMatch(pWorkItem.LWorkOutputPath, pSubject))
        {
            return PRosterRatioFormat(pWorkItem);
        }

        if (pOriginBytes is not { } pOriginWhole || pOriginWhole <= 0 || pWorkItem.LWorkOutputBytes is not { } pOutput)
        {
            return "-";
        }

        return $"{(double)pOutput / pOriginWhole:P1}";
    }

    private static bool PRosterLineageMatch(string pLeftPath, string pRightPath) =>
        PLineagePathRead(pLeftPath) is { } pLeftKey
        && PLineagePathRead(pRightPath) is { } pRightKey
        && string.Equals(pLeftKey, pRightKey, StringComparison.OrdinalIgnoreCase);

    private static string? PLineagePathRead(string pPath)
    {
        if (string.IsNullOrWhiteSpace(pPath))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(pPath);
        }
        catch (Exception pPathError) when (
            pPathError is ArgumentException or IOException or NotSupportedException)
        {
            return pPath;
        }
    }

    private ListBoxItem PLineageRowRead(PRosterLineageEntry pLineageEntry)
    {
        if (!pRosterLineageRows.TryGetValue(pLineageEntry.PRosterLineageId, out ListBoxItem? pLineageRow))
        {
            pLineageRow = PLineageRowBuild(pLineageEntry.PRosterLineageId);
            pRosterLineageRows[pLineageEntry.PRosterLineageId] = pLineageRow;
        }

        if (pRosterLineageLabels.TryGetValue(pLineageEntry.PRosterLineageId, out TextBlock? pLineageLabel))
        {
            pLineageLabel.Text = PLineageTitleRead(pLineageEntry);
        }

        return pLineageRow;
    }

    private ListBoxItem PLineageRowBuild(Guid pLineageId)
    {
        var pLineageMark = new Border
        {
            Width = 3,
            Height = 12,
            CornerRadius = new CornerRadius(2),
            Background = PRosterTheme.PRosterTitleBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var pLineageLabel = new TextBlock
        {
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = PRosterTheme.PRosterTitleBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        pRosterLineageLabels[pLineageId] = pLineageLabel;

        var pLineageContent = new StackPanel { Orientation = Orientation.Horizontal };
        pLineageContent.Children.Add(pLineageMark);
        pLineageContent.Children.Add(pLineageLabel);

        return new ListBoxItem
        {
            Content = pLineageContent,
            Focusable = false,
            IsHitTestVisible = false,
            Style = PLineageStyleCreate()
        };
    }

    private static Style PLineageStyleCreate()
    {
        var pStyle = new Style(typeof(ListBoxItem));
        pStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PLineageTemplateCreate()));
        return pStyle;
    }

    private static ControlTemplate PLineageTemplateCreate()
    {
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BackgroundProperty, PRosterTheme.PRosterHeaderBrush);
        pBorder.SetValue(Border.BorderBrushProperty, PRosterTheme.PRosterLineBrush);
        pBorder.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 1));
        pBorder.SetValue(Border.PaddingProperty, PRosterTheme.PRosterRowPadding);
        pBorder.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
        return new ControlTemplate(typeof(ListBoxItem)) { VisualTree = pBorder };
    }

    private static string PLineageTitleRead(PRosterLineageEntry pLineageEntry)
    {
        string pLineageName = PLineageFileRead(pLineageEntry.PRosterLineageSubject);
        return pLineageEntry.PRosterLineageItems.Count == 1
            ? LLocalization.LLocalizationFormat("Roster.Lineage.One", pLineageName)
            : LLocalization.LLocalizationFormat(
                "Roster.Lineage.Many", pLineageName, pLineageEntry.PRosterLineageItems.Count);
    }

    private static string PLineageFileRead(string pFilePath)
    {
        if (string.IsNullOrWhiteSpace(pFilePath))
        {
            return LLocalization.LLocalizationTextRead("Roster.Lineage.Unknown");
        }

        try
        {
            return Path.GetFileName(pFilePath);
        }
        catch (ArgumentException)
        {
            return pFilePath;
        }
    }

    private void PRosterLineageRemove(IReadOnlyCollection<Guid> pLineageKeep)
    {
        foreach (Guid pLineageId in pRosterLineageRows.Keys.ToArray())
        {
            if (pLineageKeep.Contains(pLineageId))
            {
                continue;
            }

            pRosterLineageRows.Remove(pLineageId);
            pRosterLineageLabels.Remove(pLineageId);
        }
    }
}
