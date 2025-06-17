using Mafi.Base;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mafi.Base.Assets.Base.Machines.PowerPlant;
using static Mafi.Base.Assets.Base.Machines;
using static Mafi.Base.Assets;
using static Mafi.Unity.Assets.Unity;
using Mafi;
using static Mafi.Core.Entities.Static.Layout.LayoutEntityProto;
using Mafi.Core.PropertiesDb;
using UnityEngine;
using static RTG.Object2ObjectSnap;

namespace MiningDumpingMod
{
    public class MDRegistrator : IModData
    {
        public void RegisterData(ProtoRegistrator registrator)
        {
            Proto.Str ps = Proto.CreateStr(PrototypeIDs.LocalEntities.MDTowerID, "MineDumpControl", "A building to control a mining and dumping in an area");

            EntityLayout el = registrator.LayoutParser.ParseLayoutOrThrow(
                "   [8][8][8][8][8][8][8][8][8][8][8][8][8][8][8]   ",
                "   [8][8][8][8][8][8][8][8][8][8][8][8][8][8][8]   ",
                "   [8][8][9][9][9][9][9][8][9][9][9][9][9][8][8]   ",
                "   [8][8][9][9][9][9][9][8][9][9][9][9][9][8][8]   ",
                "C~>[8][8][9][9][9][9][9][8][9][9][9][9][9][8][8]>~D",
                "A~>[8][8][9][9][9][9][9][8][9][9][9][9][9][8][8]>~B",
                "   [8][8][9][9][9][9][9][8][9][9][9][9][9][8][8]   ",
                "   [8][8][9][9][9][9][9][8][9][9][9][9][9][8][8]   ",
                "   [8][8][8][8][8][8][8][8][8][8][8][8][8][8][8]   ",
                "   [8][8][8][8][8][8][8][8][8][8][8][8][8][8][8]   "
                );


            EntityCostsTpl ecTpl = new EntityCostsTpl.Builder().CP3(100).Glass(20).MaintenanceT3(20).Workers(20);
//            EntityCostsTpl ecTpl = new EntityCostsTpl.Builder().CP(1).MaintenanceT1(2);
            EntityCosts ec = ecTpl.MapToEntityCosts(registrator);

            LayoutEntityProto.Gfx lg =
                 new LayoutEntityProto.Gfx("Assets/MiningDumpingMod/MDControlBuilding2.prefab",
                customIconPath: "Assets/MiningDumpingMod/Building256.png",

                categories: new ImmutableArray<ToolbarEntryData>?(registrator.GetCategoriesProtos(Ids.ToolbarCategories.Buildings)))
                ;

            MDPrototype bp =
                new MDPrototype(
                    PrototypeIDs.LocalEntities.MDTowerID, ps, el, ec, lg);
            bp.maxAreaSize = 250;
            bp.maxDistanceFromBuilding = 100;
            registrator.PrototypesDb.Add(bp);
        }
    }
}
