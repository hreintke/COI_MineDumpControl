﻿using Mafi.Base;
using Mafi.Core.Mods;
using Mafi.Core.Research;
using Mafi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiningDumpingMod
{
    public partial class PrototypeIDs
    {
        public partial class Research
        {
            public static readonly ResearchNodeProto.ID UnlockMDTower = Ids.Research.CreateId("UnlockMDTower");
        }
    }

    public class MDResearch : IModData
    {
        public void RegisterData(ProtoRegistrator registrator)
        {
            registrator.ResearchNodeProtoBuilder
                .Start("MD Control Tower", PrototypeIDs.Research.UnlockMDTower)
                .Description("MD Control Tower")
                .AddLayoutEntityToUnlock(PrototypeIDs.LocalEntities.MDTowerID)
                .SetGridPosition(registrator.PrototypesDb.GetOrThrow<ResearchNodeProto>(Ids.Research.BasicComputing).GridPosition + new Vector2i(0,-4))
                .SetCosts(new ResearchCostsTpl(5))
                .AddParents(registrator.PrototypesDb.GetOrThrow<ResearchNodeProto>(Ids.Research.VehicleAssembly3),
                            registrator.PrototypesDb.GetOrThrow<ResearchNodeProto>(Ids.Research.GlassSmeltingT2))
                .BuildAndAdd();
        }
    }
}
    