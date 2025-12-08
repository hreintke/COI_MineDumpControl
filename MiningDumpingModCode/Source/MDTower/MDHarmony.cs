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
using Mafi.Core.GameLoop;
using Mafi.Core.Map;

namespace MiningDumpingMod
{
    [GlobalDependency(RegistrationMode.AsSelf)]
    [HarmonyPatch]
    public class modPatches
    {
        private readonly Harmony harmony;
        static private MDManager _mdManager;
        static private GameLoopEvents gameLoopEvents;
        static bool hasRun = false;

        modPatches(MDManager mdManager, GameLoopEvents gle )
        {
            LogWrite.Info($"ModPatches start");

            harmony = new Harmony("MiningDumping");

            if (Harmony.HasAnyPatches("MiningDumping")) 
            {
                LogWrite.Info($"Allready applied , removing MD harmony patches");
                harmony.UnpatchAll("MiningDumping");
            }
            harmony.PatchAll();
            LogWrite.Info("Harmony patches applied");
            _mdManager = mdManager;
            gameLoopEvents = gle;
            gameLoopEvents.Terminate.AddNonSaveable<modPatches>(this, terminateEvent);
        }

        void terminateEvent()
        {
            LogWrite.Info("Remove Harmony patches");
            harmony.UnpatchAll("MiningDumping");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TowerAreasRenderer), "rendererLoadState")]
        static void Postfix(TowerAreasRenderer __instance)
        {
            foreach(MDTower mt in _mdManager.MDs)
            {
                typeof(TowerAreasRenderer).GetMethod("addTower", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, (new object[] { mt }));
            }
            _mdManager.OnMDAdded.AddNonSaveable(__instance, delegate (MDTower tower, EntityAddReason reason)
            {
                FieldInfo fa = typeof(TowerAreasRenderer).GetField("m_onTowerAdded", BindingFlags.NonPublic | BindingFlags.Instance);
                LystStruct<IAreaManagingTower> towerAdded = (LystStruct<IAreaManagingTower>)fa.GetValue(__instance);
                towerAdded.Add(tower);
                fa.SetValue(__instance, towerAdded);
            });
            _mdManager.OnAreaChange.AddNonSaveable(__instance, delegate (MDTower tower, PolygonTerrainArea2i oldArea)
            {
                FieldInfo fu = typeof(TowerAreasRenderer).GetField("m_onTowerUpdated", BindingFlags.NonPublic | BindingFlags.Instance);
                LystStruct<IAreaManagingTower> towerUpdated = (LystStruct<IAreaManagingTower>)fu.GetValue(__instance);
                towerUpdated.Add(tower);
                fu.SetValue(__instance, towerUpdated);
            });
            _mdManager.OnMDRemoved.AddNonSaveable(__instance, delegate (MDTower tower, EntityRemoveReason reason)
            {
                FieldInfo fr = typeof(TowerAreasRenderer).GetField("m_onTowerRemoved", BindingFlags.NonPublic | BindingFlags.Instance);
                LystStruct<IAreaManagingTower> towerRemoved = (LystStruct<IAreaManagingTower>)fr.GetValue(__instance);
                towerRemoved.Add(tower);
                fr.SetValue(__instance, towerRemoved);
            });

        }
    }

}

