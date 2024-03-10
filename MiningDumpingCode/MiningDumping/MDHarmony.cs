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

        static int cnt = 0;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TowerAreasRenderer), "rendererLoadState")]
        static void Postfix(TowerAreasRenderer __instance)
        {
            LogWrite.Info($"{cnt++} TowerAreasRender rendererLoadState");
            IndexableEnumerator<MDTower> enumerator = _mdManager.MDs.GetEnumerator();
            while (enumerator.MoveNext())
            {
                LogWrite.Info($"Adding MDTower");
                IAreaManagingTower current = enumerator.Current;
                typeof(TowerAreasRenderer).GetMethod("addTowerOrUpdateArea", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, (new object[] { current }));
            }

            FieldInfo fo = typeof(TowerAreasRenderer).GetField("m_delayedProcessing", BindingFlags.NonPublic | BindingFlags.Instance);
            DelayedItemsProcessing<IAreaManagingTower> x = (DelayedItemsProcessing<IAreaManagingTower>)fo.GetValue(__instance);

            _mdManager.OnMDAdded.AddNonSaveable(__instance, delegate (MDTower tower, EntityAddReason reason)
            {
                LogWrite.Info("MDHarmony tower add");
                x.AddOnSim(tower);
            });

            _mdManager.OnAreaChange.AddNonSaveable(__instance, delegate (MDTower tower, RectangleTerrainArea2i oldArea)
            {
                LogWrite.Info("MDHarmony area update");
                x.AddOnSim(tower);
            });
            _mdManager.OnMDRemoved.AddNonSaveable(__instance, delegate (MDTower tower, EntityRemoveReason reason)
            {
                LogWrite.Info("MDHarmony tower remove");
                x.RemoveOnSim(tower);
            });

        }
    }
}

