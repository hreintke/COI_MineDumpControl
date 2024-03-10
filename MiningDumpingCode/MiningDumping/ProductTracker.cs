using Mafi.Collections;
using Mafi.Core.Products;
using Mafi.Core.Vehicles;
using Mafi.Core;
using Mafi.Serialization;
using Mafi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiningDumpingMod
{
 //   [GenerateSerializer(false, null, 0)]
    internal class PartialMinedProductTracker
    {
        private static readonly Action<object, BlobWriter> s_serializeDataDelayedAction;

        private static readonly Action<object, BlobReader> s_deserializeDataDelayedAction;

        private LystStruct<LooseProductQuantity> m_finalProducts;

        private LystStruct<KeyValuePair<ProductProto, PartialQuantity>> m_minedProducts;

        private Quantity m_maxCapacity;

        private Quantity m_usedCapacity;

        public bool IsFull => m_usedCapacity >= m_maxCapacity;

        public static void Serialize(PartialMinedProductTracker value, BlobWriter writer)
        {
            if (writer.TryStartClassSerialization(value))
            {
                writer.EnqueueDataSerialization(value, s_serializeDataDelayedAction);
            }
        }

        protected virtual void SerializeData(BlobWriter writer)
        {
            LystStruct<LooseProductQuantity>.Serialize(m_finalProducts, writer);
            Quantity.Serialize(m_maxCapacity, writer);
            LystStruct<KeyValuePair<ProductProto, PartialQuantity>>.Serialize(m_minedProducts, writer);
            Quantity.Serialize(m_usedCapacity, writer);
        }

        public static PartialMinedProductTracker Deserialize(BlobReader reader)
        {
            if (reader.TryStartClassDeserialization(out PartialMinedProductTracker obj, (Func<BlobReader, Type, PartialMinedProductTracker>)null))
            {
                reader.EnqueueDataDeserialization(obj, s_deserializeDataDelayedAction);
            }

            return obj;
        }

        protected virtual void DeserializeData(BlobReader reader)
        {
            m_finalProducts = LystStruct<LooseProductQuantity>.Deserialize(reader);
            m_maxCapacity = Quantity.Deserialize(reader);
            m_minedProducts = LystStruct<KeyValuePair<ProductProto, PartialQuantity>>.Deserialize(reader);
            m_usedCapacity = Quantity.Deserialize(reader);
        }

        internal void Reset(Quantity maxCapacity, VehicleCargo currentCargo)
        {
            m_minedProducts.ClearSkipZeroingMemory();
            m_maxCapacity = maxCapacity;
            m_usedCapacity = Quantity.Zero;
            Lyst<KeyValuePair<ProductProto, Quantity>>.Enumerator enumerator = currentCargo.GetEnumerator();
            while (enumerator.MoveNext())
            {
                LystStructExtensions.Add(key: enumerator.Current.Key, list: ref m_minedProducts, value: PartialQuantity.Zero);
            }
        }

        internal void AddMinedProduct(PartialProductQuantity deltaPq)
        {
            Assert.That(deltaPq.Quantity).IsPositive();
            if (!deltaPq.Quantity.IsNotPositive)
            {
                if (m_minedProducts.ContainsKey(deltaPq.Product))
                {
                    ref KeyValuePair<ProductProto, PartialQuantity> valueAsRef = ref LystStructExtensions.GetValueAsRef(ref m_minedProducts, deltaPq.Product);
                    m_usedCapacity -= valueAsRef.Value.ToQuantityRounded();
                    valueAsRef = Make.Kvp(valueAsRef.Key, valueAsRef.Value + deltaPq.Quantity);
                }
                else
                {
                    m_minedProducts.Add(deltaPq.Product, deltaPq.Quantity);
                }

                m_usedCapacity += m_minedProducts.GetValueOrDefault(deltaPq.Product).ToQuantityRounded();
                if (m_usedCapacity > m_maxCapacity)
                {
                    ref KeyValuePair<ProductProto, PartialQuantity> valueAsRef2 = ref LystStructExtensions.GetValueAsRef(ref m_minedProducts, deltaPq.Product);
                    Assert.That(valueAsRef2.Value.FractionalPart).IsNear(Fix32.Zero, Fix32.FromFraction(1L, 100L));
                    m_usedCapacity -= valueAsRef2.Value.ToQuantityRounded();
                    valueAsRef2 = Make.Kvp(valueAsRef2.Key, valueAsRef2.Value.IntegerPart.AsPartial);
                    m_usedCapacity += valueAsRef2.Value.ToQuantityRounded();
                }

                Assert.That(m_usedCapacity).IsLessOrEqual(m_maxCapacity);
            }
        }

        internal PartialQuantity MaxAllowedQuantityOf(ProductProto product)
        {
            if (m_minedProducts.ContainsKey(product))
            {
                return (m_maxCapacity - m_usedCapacity).AsPartial - m_minedProducts.GetValueOrDefault(product).FractionalPart;
            }

            if (m_minedProducts.Count >= 3)
            {
                return PartialQuantity.Zero;
            }

            return (m_maxCapacity - m_usedCapacity).AsPartial;
        }

        internal LystStruct<LooseProductQuantity> FinalProductsReadonly()
        {
            Assert.That(m_usedCapacity).IsLessOrEqual(m_maxCapacity);
            m_finalProducts.ClearSkipZeroingMemory();
            LystStruct<KeyValuePair<ProductProto, PartialQuantity>>.Enumerator enumerator = m_minedProducts.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<ProductProto, PartialQuantity> current = enumerator.Current;
                m_finalProducts.Add(new LooseProductQuantity((LooseProductProto)current.Key, current.Value.ToQuantityRounded()));
            }

            return m_finalProducts;
        }

        public PartialMinedProductTracker()
        {
        }

        static PartialMinedProductTracker()
        {
            s_serializeDataDelayedAction = delegate (object obj, BlobWriter writer)
            {
                ((PartialMinedProductTracker)obj).SerializeData(writer);
            };
            s_deserializeDataDelayedAction = delegate (object obj, BlobReader reader)
            {
                ((PartialMinedProductTracker)obj).DeserializeData(reader);
            };
        }
    }
}

