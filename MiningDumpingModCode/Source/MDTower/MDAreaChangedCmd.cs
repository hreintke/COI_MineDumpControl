using Mafi.Core;
using Mafi.Core.Buildings.Mine;
using Mafi.Core.Input;
using Mafi.Core.Terrain;
using Mafi.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiningDumpingMod;

[GenerateSerializer(false, null, 0)]
public class MDAreaChangedCmd : InputCommand
{
    public EntityId mdTowerId;
    public PolygonTerrainArea2i Area { get; private set; }

    private static readonly Action<object, BlobWriter> s_serializeDataDelayedAction;
    private static readonly Action<object, BlobReader> s_deserializeDataDelayedAction;

    public MDAreaChangedCmd(EntityId mdId, PolygonTerrainArea2i area)
    {
        this.mdTowerId = mdId;
        this.Area = area;
    }

    public static void Serialize(MDAreaChangedCmd value, BlobWriter writer)
    {
        if (!writer.TryStartClassSerialization<MDAreaChangedCmd>(value))
            return;
        writer.EnqueueDataSerialization((object)value, MDAreaChangedCmd.s_serializeDataDelayedAction);
    }

    protected override void SerializeData(BlobWriter writer)
    {
        base.SerializeData(writer);
        PolygonTerrainArea2i.Serialize(this.Area, writer);
        EntityId.Serialize(this.mdTowerId, writer);
    }

    public static MDAreaChangedCmd Deserialize(BlobReader reader)
    {
        MDAreaChangedCmd towerAreaChangeCmd;
        if (reader.TryStartClassDeserialization<MDAreaChangedCmd>(out towerAreaChangeCmd))
            reader.EnqueueDataDeserialization((object)towerAreaChangeCmd, MDAreaChangedCmd.s_deserializeDataDelayedAction);
        return towerAreaChangeCmd;
    }

    protected override void DeserializeData(BlobReader reader)
    {
        base.DeserializeData(reader);
        this.Area = PolygonTerrainArea2i.Deserialize(reader);
        reader.SetField<MDAreaChangedCmd>(this, "mdTowerId", (object)EntityId.Deserialize(reader));
    }

//    static MDAreaChangedCmd()
 //   {
   //     MDAreaChangedCmd.s_serializeDataDelayedAction = (Action<object, BlobWriter>)((obj, writer) => ((InputCommand<bool>)obj).SerializeData(writer));
     //   MDAreaChangedCmd.s_deserializeDataDelayedAction = (Action<object, BlobReader>)((obj, reader) => ((InputCommand<bool>)obj).DeserializeData(reader));
    //}
}
