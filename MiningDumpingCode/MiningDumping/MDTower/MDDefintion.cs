using Mafi.Core.Entities;
using Mafi.Core;
using Mafi.Serialization;
using System;
using System.Linq;
using Mafi.Core.Entities.Static.Layout;
using Mafi;
using Mafi.Core.Population;
using Mafi.Core.Ports;
using Mafi.Core.Ports.Io;
using Mafi.Core.Terrain;
using Mafi.Collections;
using Mafi.Core.Products;
using System.Xml.Serialization;
using Mafi.Core.Buildings.Towers;
using Mafi.Core.Terrain.Designation;
using Mafi.Core.Buildings.FuelStations;
using Mafi.Core.Buildings.Mine;
using System.Collections.Generic;
using Mafi.Core.Buildings.Forestry;
using Mafi.Vornoi;
using Mafi.Unity.Mine;
using System.Reflection;
using Mafi.Unity.Utils;
using Mafi.Core.Maintenance;
using Mafi.Core.Vehicles.Jobs;

namespace MiningDumpingMod
{

    [GenerateSerializer(false, null, 0)]
    public class MDTower : LayoutEntity, IEntityWithWorkers, IEntityWithSimUpdate, IEntityWithPorts, IAreaManagingTower, IMaintainedEntity
    {
        public MDTower(EntityId id, MDPrototype proto, TileTransform transform, EntityContext context,
            TerrainDesignationsManager designationManager,
            MDManager mdManager,
            IEntityMaintenanceProvidersFactory maintenanceProvidersFactory) : base(id, proto, transform, context)
        {
            _proto = proto;
            minableArea = new RectangleTerrainArea2i(this.Position2f.Tile2i.AddY(10).AddX(-5), new RelTile2i(10, 10));
            _designationManager = designationManager;

            _designationManager.DesignationAdded.Add<MDTower>(this, new Action<TerrainDesignation>(this.designationAdded));
            _designationManager.DesignationRemoved.Add<MDTower>(this, new Action<TerrainDesignation>(this.designationRemoved));
            _mdManager = mdManager;
            _maintenance = maintenanceProvidersFactory.CreateFor(this);

            editMinableArea(minableArea);
         }

        private int serializeVersion = 1; // real value will be set after deserialization;

        public new MDPrototype Prototype
        {
            get
            {
                return _proto;
            }
            protected set
            {
                _proto = value;
                base.Prototype = value;
            }
        }

        public enum State
        {
            None,
            Working,
            Paused,
            Waiting,
            BufferIssue,
            NotEnoughWorkers,
        }

        private readonly TerrainManager _terrainManager;
        private readonly ITerrainDesignationsManager _designationManager;
        private MDPrototype _proto;
        private RectangleTerrainArea2i minableArea;
        private ProductsManager _productsManager;
        private readonly TowerAreasRenderer _towerAreasRenderer;
        private readonly MDManager _mdManager;
        public IEntityMaintenanceProvider _maintenance { get; private set; }

        public RectangleTerrainArea2i Area { get { return minableArea; } }
        public int maxAreaSize = 150;

        private PartialProductsBuffer minedProducts = new PartialProductsBuffer(new PartialQuantity(100));
        private PartialProductsBuffer tobeDumpedProducts = new PartialProductsBuffer(new PartialQuantity(100));

        public IReadOnlySet<TerrainDesignation> ManagedDesignations { get => managedDesignations; }
        private readonly Set<TerrainDesignation> managedDesignations = new Set<TerrainDesignation>();

        LystStruct<TerrainDesignation> mineDesignations = new LystStruct<TerrainDesignation>();
        LystStruct<TerrainDesignation> dumpDesignations = new LystStruct<TerrainDesignation>();

        // Not used but required because if IAreaManagingTower interface
        public IReadOnlySet<FuelStation> AssignedFuelStations { get => assignedFuelStations; }
        private readonly Set<FuelStation> assignedFuelStations;

        public State CurrentState { get; private set; }

        public Quantity mineBufferQuantity => minedProducts.GetQuantity();
        public Quantity mineBufferMax => minedProducts.maxQuantity.IntegerPart;

        public Quantity dumpBufferQuantity => tobeDumpedProducts.GetQuantity();
        public Quantity dumpBufferMax => tobeDumpedProducts.maxQuantity.IntegerPart;

        private ProductQuantity sendInProgress = ProductQuantity.None;
        void IEntityWithSimUpdate.SimUpdate()
        {
            CurrentState = updateState();
            simStepCounter++;
            if (isMining)
            {
                tryMining();
            }
            if (isDumping)
            {
                tryDumping();
            }
        }

        private void tryMining()
        {
            if ((CurrentState == State.Working) && !minedProducts.IsFull)
            {
                mineCurrentTile();
            }
            void localSendProduct(ref ProductQuantity sendProduct, IoPortData portData)
            {
                Quantity initialQuantity = sendProduct.Quantity;
                if (sendProduct.Quantity == Quantity.Zero)
                {
                    sendProduct = ProductQuantity.None;
                }
                else
                {
                    portData.SendAsMuchAs(ref sendProduct);
                    if (sendProduct.Quantity == initialQuantity)
                    {
                        return;
                    }
                    minedProducts.removeProductQuantity(new ProductQuantity(sendProduct.Product, initialQuantity - sendProduct.Quantity));
                    Context.ProductsManager.ProductCreated(sendProduct.Product, initialQuantity - sendProduct.Quantity, CreateReason.MinedFromTerrain);
                    if (sendProduct.Quantity == Quantity.Zero)
                    {
                        sendProduct = ProductQuantity.None;
                    }
                }
            }
            if (((CurrentState == State.Working) && !minedProducts.IsEmpty) || (CurrentState == State.BufferIssue))
            {
                if (ConnectedOutputPorts.Length > 0)
                {
                    for (int cp = 0;cp < ConnectedOutputPorts.Length;cp++)
                    {
                        IoPortData ioPortData = ConnectedOutputPorts[cp];
                        if (sendInProgress != ProductQuantity.None)
                        {
                            localSendProduct(ref sendInProgress, ioPortData);
                        }

                        if ((sendInProgress == ProductQuantity.None)) // to prevent the enumerator construction at each simupdate when we know we don't need products.
                        {
                            LystStruct<LooseProductQuantity>.Enumerator enumerator = minedProducts.FinalProductsReadonly().GetEnumerator();
                            while ((sendInProgress == ProductQuantity.None) && enumerator.MoveNext())
                            {
                                LooseProductQuantity lq = enumerator.Current;
                                sendInProgress = new ProductQuantity(lq.Product, lq.Quantity);
                                localSendProduct(ref sendInProgress, ioPortData);
                            }
                        }

                    }
                    

                }
            }
        }

        private int currentDesignationIndex = -1;
        private int currentDesignationTileIndex = -1;
        private Tile2i getNextDumptile()
        {
            if (currentDesignationIndex == -1)
            {
                HeightTilesF maxHeight = HeightTilesF.MinValue;
                for (int i = 0;i < dumpDesignations.Count; i++)
                {
                    if (!dumpDesignations[i].IsDumpingFulfilled)
                    {
                        HeightTilesF thisHeight = (Context.TerrainManager.GetHeight(dumpDesignations[i].CenterTileCoord));
                        if (thisHeight > maxHeight)
                        {
                            maxHeight = thisHeight;
                            currentDesignationIndex = i;
                        }
                    }
                }
                if (currentDesignationIndex == -1)
                {
                    return new Tile2i();
                }
                else
                {
                    currentDesignationTileIndex = 0;
                    return getNextDumptile();
                }
            }
            else
            {
                if (dumpDesignations[currentDesignationIndex].IsDumpingFulfilled)
                {
                    currentDesignationIndex = -1;
                    return getNextDumptile();
                }
                else
                {   do
                    {
                        currentDesignationTileIndex = (currentDesignationTileIndex + 1) % 25;
                    }
                    while (dumpDesignations[currentDesignationIndex].IsDumpingFulfilledAt(dumpDesignations[currentDesignationIndex].OriginTileCoord + new RelTile2i().Rel4Index(currentDesignationTileIndex)));
                    
                    return (dumpDesignations[currentDesignationIndex].OriginTileCoord + new RelTile2i().Rel4Index(currentDesignationTileIndex));
                }
                
            }
            
        }
       

        public void dumpCurrentTile()
        {
            if ((tobeDumpedProducts.IsEmpty) || (dumpDesignations.IsEmpty)) //|| currentDumpTile == new Tile2i())
            {
                return;
            }

            Tile2i nextTile = getNextDumptile();

            if (nextTile != new Tile2i())
            {
                dumpTile(nextTile);
            }

        }

        private void dumpTile(Tile2i txi)
        {
            ProductQuantity pq = tobeDumpedProducts.getSomeProduct(4.Quantity());
            if (pq == ProductQuantity.None)
            {
                return;
            }
            Tile2iAndIndex txia = txi.ExtendIndex(Context.TerrainManager);
            ThicknessTilesF thickness = pq.Product.DumpableProduct.Value.TerrainMaterial.Value.QuantityToThickness(pq.Quantity);
#if false
            Context.TerrainManager.DumpMaterial(txia,
                new TerrainMaterialThicknessSlim(pq.Product.DumpableProduct.Value.TerrainMaterial.Value.SlimId, thickness));
            Context.ProductsManager.ClearProduct(pq);
            totalDumped += pq.Quantity.Value;
#endif      
            HeightTilesF requestedHeight = dumpDesignations[currentDesignationIndex].GetTargetHeightAt(txi);
            ThicknessTilesF notUsedThickness = Context.TerrainManager.DumpMaterialUpToHeight(txia,
                new TerrainMaterialThicknessSlim(pq.Product.DumpableProduct.Value.TerrainMaterial.Value.SlimId, thickness), requestedHeight);
            PartialProductQuantity notUsedPartialQuantity = (new TerrainMaterialThicknessSlim(pq.Product.DumpableProduct.Value.TerrainMaterial.Value.SlimId, notUsedThickness)).ToPartialProductQuantity(Context.TerrainManager);
            tobeDumpedProducts.AddProduct(notUsedPartialQuantity);
        }

        private void tryDumping()
        {
            if ((CurrentState == State.Working) || (CurrentState == State.BufferIssue))
            {
                dumpCurrentTile();
//                dumpCurrentTile();
//                dumpCurrentTile();
//                dumpCurrentTile();
            }
        }

        private State updateState()
        {
            if ((!base.IsEnabled) || _maintenance.Status.IsBroken)
            {
                return State.Paused;
            }
            if (Entity.IsMissingWorkers(this))
            {
                return State.NotEnoughWorkers;
            }
            if ((!isMining) && (!isDumping))
            {
                return State.Waiting;
            }
            if  (((minedProducts.IsFull) && (isMining)) || (((tobeDumpedProducts.IsEmpty) && (isDumping))))
            {
                return State.BufferIssue;
            }
            return State.Working;
        }

        private void designationAdded(TerrainDesignation designation)
        {
            if (!designation.Prototype.IsTerraforming || !this.isWithinArea(designation))
                return;
            designation.AddManagingTower((IAreaManagingTower)this);
            this.managedDesignations.AddAndAssertNew(designation);
            recreateManagedDestinationsLysts();
        }

        private void designationRemoved(TerrainDesignation designation)
        {
            if (!designation.Prototype.IsTerraforming || !this.managedDesignations.Remove(designation))
                return;
            Assert.That<bool>(this.isWithinArea(designation)).IsTrue();
            designation.RemoveManagingTower((IAreaManagingTower)this);
            recreateManagedDestinationsLysts();
        }

        private bool isWithinArea(TerrainDesignation designation)
        {
            return this.Area.ContainsTile(designation.OriginTileCoord);
        }

        Fix32 totalMined = 0;
        Fix32 totalDumped = 0;
        public int simStepCount { get; private set; } = 10;
        private int mineIndex = 0;
        public int simStepCounter = 0;
        public bool isMining { get; private set; } = false;
        public bool isDumping { get; private set; } = false;

        public Fix32 minedTotal
        {
            get { return totalMined; }
        }
        public Fix32 dumpedTotal
        {
            get { return totalDumped; }
        }

        public string bufferInfo()
        {
            string s = "Mined "+ (minedProducts.ToString() + "\n" + "toBeD " + tobeDumpedProducts.ToString());
            return s;
        }

        public void setMining(bool mining)
        {
            isMining = mining;
            if (isMining)
            {
                isDumping = false;
            }
        }

        public void setDumping(bool dumping)
        {
            isDumping = dumping;
            if (isDumping)
            {
                isMining = false;
            }
        }

        public override bool CanBePaused => true;

        int IEntityWithWorkers.WorkersNeeded => Prototype.Costs.Workers;
        [DoNotSave(0, null)]
        bool IEntityWithWorkers.HasWorkersCached { get; set; }

        MaintenanceCosts IMaintainedEntity.MaintenanceCosts => Prototype.Costs.Maintenance;
        public IEntityMaintenanceProvider Maintenance => _maintenance;
        bool IMaintainedEntity.IsIdleForMaintenance => (CurrentState != State.Working);

        TerrainMaterialThicknessSlim tts = new TerrainMaterialThicknessSlim();

        public string getLabelTxt()
        {

            PartialProductQuantity ppq = tts.ToPartialProductQuantity(Context.TerrainManager);
            LystStruct<LooseProductQuantity> bp = minedProducts.FinalProductsReadonly();
            LystStruct<LooseProductQuantity>.Enumerator enumerator = minedProducts.FinalProductsReadonly().GetEnumerator();
            string s = "";
            while (enumerator.MoveNext())
            {
                LooseProductQuantity lpq = enumerator.Current;
                s = s + lpq.Product.ToString() + " " + lpq.Quantity.ToString() + "\n ";
            }

            return $"area = {minableArea.ToString()}, mine = {mineIndex} \n" + s;
         }

        public void buttonAction()
        {
            mineCurrentTile();
        }


        private int mineDesignationIndex = -1;
        private int mineDesignationTileIndex = -1;

        Tile2i getNextMineTile()
        {
            if (mineDesignationIndex == -1)
            {
     
                if (mineDesignations.Count == 0)
                {
                    return new Tile2i();
                }
            
                for (int i = 0; i < mineDesignations.Count; i++)
                {
                    mineDesignationIndex = (mineDesignationIndex + 1) % mineDesignations.Count;
                    if (!mineDesignations[mineDesignationIndex].IsMiningFulfilled)
                    {
                        return getNextMineTile();
                    }
                    
                }
                mineDesignationIndex = -1;
                return new Tile2i();
            }
            else
            {
                if (mineDesignations[mineDesignationIndex].IsMiningFulfilled)
                {
                    mineDesignationIndex = -1;
                    return getNextMineTile();
                }
                else
                {
                    do
                    {
                        mineDesignationTileIndex.NextModulo(25);
                    }
                    while (mineDesignations[mineDesignationIndex].IsMiningFulfilledAt(mineDesignations[mineDesignationIndex].OriginTileCoord + new RelTile2i().Rel4Index(mineDesignationTileIndex)));
                    
                    return (mineDesignations[mineDesignationIndex].OriginTileCoord + new RelTile2i().Rel4Index(mineDesignationTileIndex));
                }
            }
        }

        public void mineCurrentTile()
        {
            if (mineDesignations.IsEmpty)
            {
                return;
            }
            PartialQuantity mined = PartialQuantity.Zero;
            
            Tile2i t = getNextMineTile();
            while ((t != new Tile2i()) && (mined.Value < 3.ToFix32()))
            {
                mined += mineTile(t);
                t = getNextMineTile();
            }
        }

        public PartialQuantity mineTile(Tile2i txi)
        {
            HeightTilesF requestedHeight = mineDesignations[mineDesignationIndex].GetTargetHeightAt(txi);
            Tile2iAndIndex txia = txi.ExtendIndex(Context.TerrainManager);
            HeightTilesF currentHeight = Context.TerrainManager.GetHeight(txi);
            
            tts = Context.TerrainManager.MineMaterial(txi.ExtendIndex(Context.TerrainManager), currentHeight - requestedHeight);
            Context.TerrainManager.DisruptExactly(txia , currentHeight - requestedHeight);
            totalMined += tts.ToPartialProductQuantity(Context.TerrainManager).Quantity.Value;
            minedProducts.AddProduct(tts.ToPartialProductQuantity(Context.TerrainManager));
            return tts.ToPartialProductQuantity(Context.TerrainManager).Quantity;
        }

        private void cleanManagedDesignations()
        {
            foreach (TerrainDesignation designation in this.managedDesignations.ToArray())
            {
                designationRemoved(designation);
            }
            mineDesignations.Clear();
            dumpDesignations.Clear();
         }

        public void editMinableArea(RectangleTerrainArea2i newArea)
        {
            RectangleTerrainArea2i oldArea = minableArea;
            cleanManagedDesignations();
            minableArea = newArea;
            mineDesignationIndex = -1;
            mineDesignationTileIndex = -1;
            currentDesignationIndex = -1;
            currentDesignationTileIndex = -1;
            foreach (TerrainDesignation designation in (IEnumerable<TerrainDesignation>)this._designationManager.Designations)
            {
                this.designationAdded(designation);
            }
            _mdManager.InvokeOnAreaChanged(this, oldArea);
         }

        public string statusInfo()
        {
            string s = (isMining) ? "Mining" : (isDumping) ? "Dumping" : "Idle";
            string m = "MineInfo : " + "Total " + totalMined + " Buffer : " + minedProducts.ToString() + $" mCnt = {mineDesignations.Count} ";
            string d = "DumpInfo : " + "Total " + totalDumped + " Buffer : " + tobeDumpedProducts.ToString() + $" dCnt = {dumpDesignations.Count} ";
            return s + "\n" + m + "\n" + d; 
        } 
        
        public void simStepChanged(string s)
        {
            if (Int32.TryParse(s, out int j))
            {
                simStepCount = j.Max(1);
            }
        }

        public void simStepChanged(int newStep)
        {
            simStepCount = newStep.Max(1);
        }

        public Quantity ReceiveAsMuchAsFromPort(ProductQuantity pq, IoPortToken sourcePort)
        {
            if ((pq.Product.DumpableProduct.IsNone) || (tobeDumpedProducts.IsFull))
            {
                return pq.Quantity;
            }
            tobeDumpedProducts.AddProduct(pq);
            Context.ProductsManager.ClearProduct(pq);
            return Quantity.Zero;
        }

        protected override void OnDestroy()
        {
            _designationManager.DesignationAdded.Remove<MDTower>(this, new Action<TerrainDesignation>(this.designationAdded));
            _designationManager.DesignationRemoved.Remove<MDTower>(this, new Action<TerrainDesignation>(this.designationRemoved));
            base.OnDestroy();
        }

        private static readonly Action<object, BlobWriter> s_serializeDataDelayedAction;

        private static readonly Action<object, BlobReader> s_deserializeDataDelayedAction;

        public static void Serialize(MDTower value, BlobWriter writer)
        {
            if (writer.TryStartClassSerialization(value))
            {
                writer.EnqueueDataSerialization(value, s_serializeDataDelayedAction);
            }
        }
        
        protected override void SerializeData(BlobWriter writer)
        {
            base.SerializeData(writer);
            writer.WriteInt(serializeVersion);
            writer.WriteGeneric(_proto);
            writer.WriteInt(mineIndex);
            writer.WriteBool(isMining);
            writer.WriteBool(isDumping);
            writer.WriteInt(simStepCount);
            RectangleTerrainArea2i.Serialize(minableArea, writer);
            PartialProductsBuffer.Serialize(minedProducts, writer);
            ProductQuantity.Serialize(sendInProgress, writer);
            PartialProductsBuffer.Serialize(tobeDumpedProducts, writer);
            Fix32.Serialize(totalDumped, writer);
            Fix32.Serialize(totalMined, writer);
            writer.WriteGeneric<ITerrainDesignationsManager>(_designationManager);
            Set<TerrainDesignation>.Serialize(this.managedDesignations, writer);
            LystStruct<TerrainDesignation>.Serialize(this.mineDesignations, writer);
            LystStruct<TerrainDesignation>.Serialize(this.dumpDesignations, writer);
            MDManager.Serialize(this._mdManager, writer);
            writer.WriteGeneric<IEntityMaintenanceProvider>(_maintenance);
            writer.WriteInt(mineDesignationIndex);
            writer.WriteInt(currentDesignationIndex);
        }

        public static MDTower Deserialize(BlobReader reader)
        {
            if (reader.TryStartClassDeserialization(out MDTower obj, (Func<BlobReader, Type, MDTower>)null))
            {
                reader.EnqueueDataDeserialization(obj, s_deserializeDataDelayedAction);
            }
            return obj;
        }

        private void recreateManagedDestinationsLysts()
        {
            mineDesignationIndex = -1;
            mineDesignationTileIndex = -1;
            currentDesignationIndex = -1;
            currentDesignationTileIndex = -1;

            mineDesignations.Clear();
            dumpDesignations.Clear();
            foreach (TerrainDesignation designation in managedDesignations)
            {
                if ((designation.Prototype.Id.ToString() == "MiningDesignator") && designation.IsNotFulfilled)
                {
                    mineDesignations.Add(designation);
                } 
                else
                {
                    if ((designation.Prototype.Id.ToString() == "DumpingDesignator") && designation.IsNotFulfilled)
                    {
                        dumpDesignations.Add(designation);
                    }
                }
            }
        }


        protected override void DeserializeData(BlobReader reader)
        {
            LogWrite.Info("Deserializing");
            base.DeserializeData(reader);
            int sVersion = reader.ReadInt();
            reader.SetField(this, "_proto", reader.ReadGenericAs<MDPrototype>());
            reader.SetField(this, "mineIndex", reader.ReadInt());
            reader.SetProperty(this, "isMining", reader.ReadBool());
            reader.SetProperty(this, "isDumping", reader.ReadBool());
            reader.SetProperty(this, "simStepCount", reader.ReadInt());
            minableArea = RectangleTerrainArea2i.Deserialize(reader);
            minedProducts = PartialProductsBuffer.Deserialize(reader);
            sendInProgress = ProductQuantity.Deserialize(reader);
            tobeDumpedProducts = PartialProductsBuffer.Deserialize(reader);
            totalDumped = Fix32.Deserialize(reader);
            totalMined = Fix32.Deserialize(reader);
            reader.SetField<MDTower>(this, "_designationManager", (object)reader.ReadGenericAs<ITerrainDesignationsManager>());
            reader.SetField<MDTower>(this, "managedDesignations", (object)Set<TerrainDesignation>.Deserialize(reader));
            mineDesignations = LystStruct<TerrainDesignation>.Deserialize(reader);
            dumpDesignations = LystStruct<TerrainDesignation>.Deserialize(reader);
            reader.SetField<MDTower>(this, "_mdManager", (object)MDManager.Deserialize(reader));
            _maintenance = reader.ReadGenericAs<IEntityMaintenanceProvider>();
            reader.SetField(this, "mineDesignationIndex", reader.ReadInt());
            reader.SetField(this, "currentDesignationIndex", reader.ReadInt());
            maxAreaSize = _proto.maxAreaSize;
        }

        static MDTower()
        {
            s_serializeDataDelayedAction = delegate (object obj, BlobWriter writer)
            {
                ((MDTower)obj).SerializeData(writer);
            };
            s_deserializeDataDelayedAction = delegate (object obj, BlobReader reader)
            {
                ((MDTower)obj).DeserializeData(reader);
            };
        }
    }
}
