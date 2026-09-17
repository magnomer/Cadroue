using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PExport
{
    private void PExportPresetSelect(string lPresetName)
    {
        pPresetNameSelected = lPresetName;
        PExportGroupShow(lPresetName);
        lPresetOwner.LPresetSelectionSelect(lPresetName);
    }

    private void PExportGroupShow(string lPresetName)
    {
        if (LPreset.LPresetNativeCheck(lPresetName))
        {
            if (LPreset.LPresetGroupRead(lPresetName) is not string pGroupName
                || !LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(pGroupName))
            {
                return;
            }

            LPreference.LPreferenceFoldSet(pGroupName, false);
        }
        else
        {
            if (!LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(PExportUserGroup, false))
            {
                return;
            }

            LPreference.LPreferenceFoldSet(PExportUserGroup, false, false);
        }

        PExportPresetRebuild();
    }

}
