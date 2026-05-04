using AutoTerrainDesignations;
using Mafi.Core.Buildings.Towers;
using Mafi.Unity.UiToolkit.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MiningDumpingMod;

public static class ATDBridge
{
#if AutoTerrainDesignations_enabled
    public static PanelWithHeader BuildDesignationPanel(Func<IAreaManagingTower?> getTower, object key)
    {
        return AutoTerrainDesignationsApi.BuildDesignationPanel(getTower, key);
    }

    public static PanelWithHeader BuildOreCompositionPanel(Func<IAreaManagingTower?> getTower, object key)
    {
        return AutoTerrainDesignationsApi.BuildOreCompositionPanel(getTower, key);
    }
#endif
}
