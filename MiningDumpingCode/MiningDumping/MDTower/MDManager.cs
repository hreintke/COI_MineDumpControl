using Mafi.Collections.ReadonlyCollections;
using Mafi.Collections;
using Mafi;
using Mafi.Core.Buildings.Mine;
using Mafi.Core.Entities;
using Mafi.Core.Terrain;
using Mafi.Serialization;
using System;
using Mafi.Unity.Mine;
using System.Reflection;
using Mafi.Unity.Terrain;
using Mafi.Unity;

namespace MiningDumpingMod
{
    [GenerateSerializer(false, null, 0)]
    [GlobalDependency(RegistrationMode.AsSelf, false, false)]
    public class MDManager
    {
        private readonly Event<MDTower, EntityAddReason> m_onMDAdded;

        private readonly Event<MDTower, EntityRemoveReason> m_onMDRemoved;

        private readonly Event<MDTower, PolygonTerrainArea2i> m_onAreaChange;

        private readonly EntitiesManager m_entitiesManager;

        private readonly Lyst<MDTower> m_MDs;

        private static readonly Action<object, BlobWriter> s_serializeDataDelayedAction;

        private static readonly Action<object, BlobReader> s_deserializeDataDelayedAction;

        public IEvent<MDTower, EntityAddReason> OnMDAdded => m_onMDAdded;

        public IEvent<MDTower, EntityRemoveReason> OnMDRemoved => m_onMDRemoved;

        public IEvent<MDTower, PolygonTerrainArea2i> OnAreaChange => m_onAreaChange;

        public Lyst<MDTower> MDs => m_MDs;

//        private TowerAreasRenderer towerAreasRenderer;

        public MDManager(EntitiesManager entitiesManager, TowerAreasRenderer tr)
        {
            m_onMDAdded = new Event<MDTower, EntityAddReason>();
            m_onMDRemoved = new Event<MDTower, EntityRemoveReason>();
            m_onAreaChange = new Event<MDTower, PolygonTerrainArea2i>();
            m_MDs = new Lyst<MDTower>();
            m_entitiesManager = entitiesManager;
            entitiesManager.EntityAddedFull.Add(this, entityAdded);
            entitiesManager.EntityRemovedFull.Add(this, entityRemoved);
//            towerAreasRenderer = tr;
        }

        private void entityAdded(IEntity entity, EntityAddReason addReason)
        {
            MDTower mineMD = entity as MDTower;
            if (mineMD != null)
            {
                m_MDs.Add(mineMD);
                LogWrite.Info($"MD Manager Invoke add");
                m_onMDAdded.Invoke(mineMD, addReason);
  
               // typeof(TowerAreasRenderer).GetMethod("addTower", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(towerAreasRenderer, (new object[] { mineMD }));
                LogWrite.Info($"MD Manager Invoke add Done");
            }
        }

        private void entityRemoved(IEntity entity, EntityRemoveReason removeReason)
        {
            MDTower mineMD = entity as MDTower;
            if (mineMD != null)
            {
                bool value = m_MDs.TryRemoveReplaceLast(mineMD);
                Assert.That(value).IsTrue();
                m_onMDRemoved.Invoke(mineMD, removeReason);
                //typeof(TowerAreasRenderer).GetMethod("removeTower", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(towerAreasRenderer, (new object[] { mineMD }));

            }
        }

        internal void InvokeOnAreaChanged(MDTower tower, PolygonTerrainArea2i oldArea)
        {
            m_onAreaChange.Invoke(tower, oldArea);
        }

        public static void Serialize(MDManager value, BlobWriter writer)
        {
            if (writer.TryStartClassSerialization(value))
            {
                writer.EnqueueDataSerialization(value, s_serializeDataDelayedAction);
            }
        }

        protected virtual void SerializeData(BlobWriter writer)
        {
            EntitiesManager.Serialize(m_entitiesManager, writer);
            Lyst<MDTower>.Serialize(m_MDs, writer);
            Event<MDTower, PolygonTerrainArea2i>.Serialize(m_onAreaChange, writer);

            Event<MDTower, EntityAddReason>.Serialize(m_onMDAdded, writer);
            Event<MDTower, EntityRemoveReason>.Serialize(m_onMDRemoved, writer);
            LogWrite.Info($"MD Manager serialize {m_MDs.Count} towers");
        }

        public static MDManager Deserialize(BlobReader reader)
        {
            if (reader.TryStartClassDeserialization(out MDManager obj, (Func<BlobReader, Type, MDManager>)null))
            {
                reader.EnqueueDataDeserialization(obj, s_deserializeDataDelayedAction);
            }

            return obj;
        }

        protected virtual void DeserializeData(BlobReader reader)
        {
            reader.SetField<MDManager>(this, "m_entitiesManager", EntitiesManager.Deserialize(reader));
            reader.SetField<MDManager>(this, "m_MDs", Lyst<MDTower>.Deserialize(reader));
            reader.SetField<MDManager>(this, "m_onAreaChange", reader.LoadedSaveVersion >= 180 ? (object)Event<MDTower, PolygonTerrainArea2i>.Deserialize(reader) : (object)new Event<MDTower, PolygonTerrainArea2i>());
            if (reader.LoadedSaveVersion < 180)
            {
                Event<MDTower, RectangleTerrainArea2i>.Deserialize(reader);
   
            }
            reader.SetField<MDManager>(this, "m_onMDAdded", Event<MDTower, EntityAddReason>.Deserialize(reader));
            reader.SetField<MDManager>(this, "m_onMDRemoved", Event<MDTower, EntityRemoveReason>.Deserialize(reader));

            LogWrite.Info($"MD Manager deserialize {m_MDs.Count} towers");
        }

        static MDManager()
        {
            s_serializeDataDelayedAction = delegate (object obj, BlobWriter writer)
            {
                ((MDManager)obj).SerializeData(writer);
            };
            s_deserializeDataDelayedAction = delegate (object obj, BlobReader reader)
            {
                ((MDManager)obj).DeserializeData(reader);
            };
        }

    }
}
