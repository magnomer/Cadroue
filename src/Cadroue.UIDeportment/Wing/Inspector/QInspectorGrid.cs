using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

// The crop grid's wiring: pulls the passive grid contract and relays each edge act to its crop owner.
public sealed class QInspectorGrid
{
    private IReadOnlyDictionary<TextBox, TextChangedEventHandler> qInspectorGridPainting = null!;
    private TextBox qInspectorGridBottom = null!;
    private TextBox qInspectorGridLeft = null!;
    private TextBox qInspectorGridRight = null!;
    private TextBox qInspectorGridTop = null!;
    private TextBlock qInspectorGridResolution = null!;

    public Grid PInspectorGridBody { get; private set; } = null!;

    public void QInspectorGridBuild(QInspectorCrop lCrop)
    {
        PInspectorGridBody = QContract.QContractSheetFind<Grid>("PInspectorGridBody");
        qInspectorGridBottom =
            QContract.QContractFind<TextBox>(PInspectorGridBody, "PInspectorInsetBottom");
        qInspectorGridLeft = QContract.QContractFind<TextBox>(PInspectorGridBody, "PInspectorInsetLeft");
        qInspectorGridRight = QContract.QContractFind<TextBox>(PInspectorGridBody, "PInspectorInsetRight");
        qInspectorGridTop = QContract.QContractFind<TextBox>(PInspectorGridBody, "PInspectorInsetTop");
        qInspectorGridResolution =
            QContract.QContractFind<TextBlock>(PInspectorGridBody, "PInspectorResolution");

        QContract.QContractFind<TextBlock>(PInspectorGridBody, "PInspectorBottomLabel").Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Bottom");
        QContract.QContractFind<TextBlock>(PInspectorGridBody, "PInspectorLeftLabel").Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Left");
        QContract.QContractFind<TextBlock>(PInspectorGridBody, "PInspectorRightLabel").Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Right");
        QContract.QContractFind<TextBlock>(PInspectorGridBody, "PInspectorTopLabel").Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Top");

        Button lResetButton = QContract.QContractFind<Button>(PInspectorGridBody, "PInspectorResetButton");
        lResetButton.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Reset");
        lResetButton.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.ResetTooltip");
        QContract.QContractFind<Border>(PInspectorGridBody, "PInspectorResolutionBody").ToolTip =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Resolution");
        QInspectorGridAttach(lCrop, lResetButton);
        QInspectorGridBuild(true);
    }

    public void QInspectorGridBuild(bool lAttached)
    {
        foreach ((Action lAttach, Action lDetach) in
                 (List<(Action, Action)>)qInspectorGridLeft.Resources[typeof(QInspectorGrid)])
        {
            if (lAttached) lAttach(); else lDetach();
        }

        qInspectorGridLeft.Resources[typeof(QInspectorFrame)] = lAttached;
    }

    public void QInspectorGridUpdate(QInspectorCrop lCrop)
    {
        bool lAttached = !qInspectorGridLeft.Resources.Contains(typeof(QInspectorFrame))
            || qInspectorGridLeft.Resources[typeof(QInspectorFrame)] is true;
        if (lAttached)
        {
            foreach ((TextBox lBox, TextChangedEventHandler lHandler) in qInspectorGridPainting)
            {
                lBox.TextChanged -= lHandler;
            }
        }

        try
        {
            foreach (int lSide in new[] { 0, 1, 2, 3 })
            {
                QInspectorGridUpdate(lCrop, lSide);
            }

            qInspectorGridResolution.Text = lCrop.QInspectorResolutionRead();
        }
        finally
        {
            if (lAttached)
            {
                foreach ((TextBox lBox, TextChangedEventHandler lHandler) in qInspectorGridPainting)
                {
                    lBox.TextChanged += lHandler;
                }
            }
        }
    }

    private void QInspectorGridUpdate(QInspectorCrop lCrop, int lSide)
    {
        TextBox lBox = lSide switch
        {
            0 => qInspectorGridLeft,
            1 => qInspectorGridTop,
            2 => qInspectorGridRight,
            _ => qInspectorGridBottom,
        };
        lBox.Text = lCrop.QInspectorEdgeFormat(lSide, lBox.Text);
    }

    private void QInspectorGridAttach(QInspectorCrop lCrop, Button lResetButton)
    {
        var lPainting = new Dictionary<TextBox, TextChangedEventHandler>();
        foreach ((TextBox lBox, int lSide) in new[]
                 {
                     (qInspectorGridLeft, 0),
                     (qInspectorGridTop, 1),
                     (qInspectorGridRight, 2),
                     (qInspectorGridBottom, 3),
                 })
        {
            QTextbox.QTextboxAttach(lBox);
            lBox.PreviewTextInput += (_, lEvent) =>
                lEvent.Handled = !QInspectorField.QInspectorDigitCheck(lEvent.Text);
            TextChangedEventHandler lHandler = (_, _) =>
                QInspectorGridCommit(lCrop, lSide);
            lPainting.Add(lBox, lHandler);
        }

        qInspectorGridPainting = lPainting;
        RoutedEventHandler lReset = (_, _) =>
            QInspectorGridReset(lCrop);
        Action lAttach = () =>
        {
            foreach ((TextBox lBox, TextChangedEventHandler lHandler) in qInspectorGridPainting)
            {
                lBox.TextChanged -= lHandler;
                lBox.TextChanged += lHandler;
            }
            lResetButton.Click -= lReset;
            lResetButton.Click += lReset;
        };
        Action lDetach = () =>
        {
            foreach ((TextBox lBox, TextChangedEventHandler lHandler) in qInspectorGridPainting)
            {
                lBox.TextChanged -= lHandler;
            }
            lResetButton.Click -= lReset;
        };
        lAttach();
        qInspectorGridLeft.Resources[typeof(QInspectorGrid)] = new List<(Action, Action)> { (lAttach, lDetach) };
        QButton.QButtonAttach(lResetButton, QButtonLook.QButtonLookPanel);
    }

    private void QInspectorGridCommit(QInspectorCrop lCrop, int lSide)
    {
        TextBox lBox = lSide switch
        {
            0 => qInspectorGridLeft,
            1 => qInspectorGridTop,
            2 => qInspectorGridRight,
            _ => qInspectorGridBottom,
        };
        lCrop.QInspectorEdgeCommit(lSide, lBox.Text, lBox.IsKeyboardFocusWithin);
    }

    private static void QInspectorGridReset(QInspectorCrop lCrop) => lCrop.QInspectorEdgeReset();
}
