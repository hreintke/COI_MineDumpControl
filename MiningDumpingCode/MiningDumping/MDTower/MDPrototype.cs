using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mafi.Core.Prototypes.Proto;

namespace MiningDumpingMod
{
    public partial class PrototypeIDs
    {
        public partial class LocalEntities
        {
            public static readonly MDPrototype.ID MDTowerID = new MDPrototype.ID("MDTower");
        }
    }

    public class MDPrototype : LayoutEntityProto, IProto
    {
        public MDPrototype(ID id, Str strings, EntityLayout layout, EntityCosts costs, Gfx graphics)
             : base(id, strings, layout, costs, graphics)
        {
        }

        public override Type EntityType => typeof(MDTower);
       
    }
}
