using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Mafi.Core.Factory.Machines;
using Mafi;
using Mafi.Unity.Mine;
using Mafi.Collections.ReadonlyCollections;
using Mafi.Core.Buildings.Mine;
using Mafi.Core.Buildings.Towers;
using System.Reflection;
using System.ComponentModel;
using Mafi.Core.Entities;
using Mafi.Unity.Utils;
using Mafi.Core.Terrain;
using Mafi.Collections;

namespace MiningDumpingMod
{
    [GlobalDependency(RegistrationMode.AsSelf)]
    [HarmonyPatch]
    public class modPatches
    {
        private readonly Harmony harmony;
        static private MDManager _mdManager;

        modPatches(MDManager mdManager)
        {
            harmony = new Harmony("myPatch");
            harmony.PatchAll();
            LogWrite.Info("Harmony patches applied");
            _mdManager = mdManager;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TowerAreasRenderer), "rendererLoadState")]
        static void Postfix(TowerAreasRenderer __instance)
        {
            LogWrite.Info($"TowerAreasRender rendererLoadState");
            
            foreach(MDTower mt in _mdManager.MDs)
            {
                LogWrite.Info($"Adding MDTower");
                typeof(TowerAreasRenderer).GetMethod("addTower", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, (new object[] { mt }));
            }
            _mdManager.OnMDAdded.AddNonSaveable(__instance, delegate (MDTower tower, EntityAddReason reason)
            {
                FieldInfo fa = typeof(TowerAreasRenderer).GetField("m_onTowerAdded", BindingFlags.NonPublic | BindingFlags.Instance);
                LystStruct<IAreaManagingTower> towerAdded = (LystStruct<IAreaManagingTower>)fa.GetValue(__instance);
                towerAdded.Add(tower);
                fa.SetValue(__instance, towerAdded);
                LogWrite.Info($"MDTower added {tower.Id}");
            });
            _mdManager.OnAreaChange.AddNonSaveable(__instance, delegate (MDTower tower, PolygonTerrainArea2i oldArea)
            {
                FieldInfo fu = typeof(TowerAreasRenderer).GetField("m_onTowerUpdated", BindingFlags.NonPublic | BindingFlags.Instance);
                LystStruct<IAreaManagingTower> towerUpdated = (LystStruct<IAreaManagingTower>)fu.GetValue(__instance);
                towerUpdated.Add(tower);
                fu.SetValue(__instance, towerUpdated);
                LogWrite.Info($"MDTower areachanged {tower.Id}");
            });
            _mdManager.OnMDRemoved.AddNonSaveable(__instance, delegate (MDTower tower, EntityRemoveReason reason)
            {
                FieldInfo fr = typeof(TowerAreasRenderer).GetField("m_onTowerRemoved", BindingFlags.NonPublic | BindingFlags.Instance);
                LystStruct<IAreaManagingTower> towerRemoved = (LystStruct<IAreaManagingTower>)fr.GetValue(__instance);
                towerRemoved.Add(tower);
                fr.SetValue(__instance, towerRemoved);
                LogWrite.Info($"MDTower removed {tower.Id}");
            });

        }
    }

}

