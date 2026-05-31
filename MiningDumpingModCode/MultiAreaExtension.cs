using Mafi;
using Mafi.Collections;
using Mafi.Core.Buildings.Forestry;
using Mafi.Core.Buildings.Mine;
using Mafi.Core.Buildings.Towers;
using Mafi.Core.Input;
using Mafi.Core.Terrain;
using Mafi.Unity.Mine;
using Mafi.Unity.Ui.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiningDumpingMod;

public static class MDBuildingMultiAreaEditControllerExtensions
{
    private static readonly ColorRgba TOWER_OUTLINE_COLOR;
    public static void ActivateForMDTower(
      this MultiAreaEditController controller,
      IAreaManagingTower tower,
      MDManager mdManager,
      TowerAreasRenderer towerAreasRenderer,
      IInputScheduler inputScheduler,
      Action onDeactivated = null)
    {
        Lyst<MultiAreaEditController.Entry> entries = new Lyst<MultiAreaEditController.Entry>();
        int initialIndex = -1;
        foreach (MDTower tower1 in mdManager.MDs)
        {
            if (tower1 == tower)
                initialIndex = entries.Count;
            entries.Add(MDBuildingMultiAreaEditControllerExtensions.createTowerEntry((IAreaManagingTower)tower1, towerAreasRenderer, inputScheduler));
        }
        controller.ActivateForAreas(entries, initialIndex, onDeactivated, towerAreasRenderer.CreateCombinedActivatorWithTerrainDesignatorsAndGrid());
    }

    private static MultiAreaEditController.Entry createTowerEntry(
          IAreaManagingTower tower,
          TowerAreasRenderer towerAreasRenderer,
          IInputScheduler inputScheduler)
    {
        Fix32 maxEdgeSize = (Fix32)(tower is MineTower mineTower1 ? mineTower1.Prototype.Area.MaxAreaEdgeSize.Value : (tower is ForestryTower forestryTower1 ? forestryTower1.Prototype.Area.MaxAreaEdgeSize.Value : 256 /*0x0100*/));
        return new MultiAreaEditController.Entry(tower.Area, maxEdgeSize, MDBuildingMultiAreaEditControllerExtensions.TOWER_OUTLINE_COLOR, (Action<PolygonTerrainArea2i>)(newArea =>
        {
            inputScheduler.ScheduleInputCmd<MDAreaChangedCmd>(new MDAreaChangedCmd(tower.Id, newArea));

        }), (Action)(() => towerAreasRenderer.MarkAreaUnderEdit(tower.CreateOption<IAreaManagingTower>())), (Action)(() => towerAreasRenderer.MarkAreaUnderEdit(Option<IAreaManagingTower>.None)));
    }
    static MDBuildingMultiAreaEditControllerExtensions()
    {
        MDBuildingMultiAreaEditControllerExtensions.TOWER_OUTLINE_COLOR = new ColorRgba(12303291 /*0xBBBBBB*/);
    }
}
