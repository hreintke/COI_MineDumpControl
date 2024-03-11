using Mafi.Collections.ReadonlyCollections;
using Mafi.Collections;
using Mafi;
using Mafi.Core.Buildings.Mine;
using Mafi.Core.Entities;
using Mafi.Core.Terrain;
using Mafi.Serialization;
using System;

namespace MiningDumpingMod
{
    [GenerateSerializer(false, null, 0)]
    [GlobalDependency(RegistrationMode.AsSelf, false, false)]
    public class MDManager
    {
        private readonly Event<MDTower, EntityAddReason> m_onMDAdded;

        private readonly Event<MDTower, EntityRemoveReason> m_onMDRemoved;

        private readonly Event<MDTower, RectangleTerrainArea2i> m_onAreaChange;

        private readonly EntitiesManager m_entitiesManager;

        private readonly Lyst<MDTower> m_MDs;

        private static readonly Action<object, BlobWriter> s_serializeDataDelayedAction;

        private static readonly Action<object, BlobReader> s_deserializeDataDelayedAction;

        public IEvent<MDTower, EntityAddReason> OnMDAdded => m_onMDAdded;

        public IEvent<MDTower, EntityRemoveReason> OnMDRemoved => m_onMDRemoved;

        public IEvent<MDTower, RectangleTerrainArea2i> OnAreaChange => m_onAreaChange;

        public IIndexable<MDTower> MDs => m_MDs;

        public MDManager(EntitiesManager entitiesManager)
        {
            m_onMDAdded = new Event<MDTower, EntityAddReason>();
            m_onMDRemoved = new Event<MDTower, EntityRemoveReason>();
            m_onAreaChange = new Event<MDTower, RectangleTerrainArea2i>();
            m_MDs = new Lyst<MDTower>();
            m_entitiesManager = entitiesManager;
            entitiesManager.EntityAddedFull.Add(this, entityAdded);
            entitiesManager.EntityRemovedFull.Add(this, entityRemoved);
        }

        private void entityAdded(IEntity entity, EntityAddReason addReason)
        {
            MDTower mineMD = entity as MDTower;
            if (mineMD != null)
            {
                m_MDs.Add(mineMD);
                m_onMDAdded.Invoke(mineMD, addReason);
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
            }
        }

        internal void InvokeOnAreaChanged(MDTower tower, RectangleTerrainArea2i oldArea)
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
            Event<MDTower, RectangleTerrainArea2i>.Serialize(m_onAreaChange, writer);
            Event<MDTower, EntityAddReason>.Serialize(m_onMDAdded, writer);
            Event<MDTower, EntityRemoveReason>.Serialize(m_onMDRemoved, writer);
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
            reader.SetField(this, "m_entitiesManager", EntitiesManager.Deserialize(reader));
            reader.SetField(this, "m_MDs", Lyst<MDTower>.Deserialize(reader));
            reader.SetField(this, "m_onAreaChange", Event<MDTower, RectangleTerrainArea2i>.Deserialize(reader));
            reader.SetField(this, "m_onMDAdded", Event<MDTower, EntityAddReason>.Deserialize(reader));
            reader.SetField(this, "m_onMDRemoved", Event<MDTower, EntityRemoveReason>.Deserialize(reader));
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
