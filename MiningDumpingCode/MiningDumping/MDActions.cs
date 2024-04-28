using Mafi;
using Mafi.Base;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Entities;
using Mafi.Core.Input;
using Mafi.Core.Prototypes;
using Mafi.Core.Terrain;
using Mafi.Core.World.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiningDumpingMod
{

    [GlobalDependency(RegistrationMode.AsSelf)]
    public class MDActions
    {
        private readonly ProtosDb _protosDb;
        private readonly UnlockedProtosDb _unlockedProtosDb;

        public MDActions(
            ProtosDb protosDb,
            UnlockedProtosDb unlockedProtosDb
        )
        {
            // This unlocks the custom entity at startup
            // Next verions will show the use of research
 //           unlockedProtosDb.Unlock(ImmutableArray.Create((IProto)protosDb.Get(PrototypeIDs.LocalEntities.MDTowerID).Value));
        }
    }
}
