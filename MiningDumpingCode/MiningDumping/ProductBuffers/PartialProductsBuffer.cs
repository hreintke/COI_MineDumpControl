using Mafi;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Products;
using Mafi.Core.Terrain;
using Mafi.Serialization;
using Mafi.Unity.UiFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mafi.Base.Assets.Base.Tutorials;

namespace MiningDumpingMod
{
    [GenerateSerializer(false, null, 0)]
    internal class PartialProductsBuffer
    {
        public PartialQuantity maxQuantity;
        public PartialQuantity inUseQuantity;

        private LystStruct<KeyValuePair<ProductProto, PartialQuantity>> bufferedProducts;
        private LystStruct<LooseProductQuantity> finalProducts;

        public bool IsFull => inUseQuantity >= maxQuantity;

        public bool IsEmpty => inUseQuantity == Quantity.Zero;

        public PartialProductsBuffer(PartialQuantity maxQ)
        {
            this.maxQuantity = maxQ;
        }

        public PartialQuantity availabeQuantity()
        {
            return maxQuantity - inUseQuantity;
        }

        public PartialQuantity getPartialQuantity(ProductProto product)
        {
            return bufferedProducts.GetValueOrDefault(product);
        }

        public Quantity GetQuantity(ProductProto product)
        {
            return getPartialQuantity(product).IntegerPart;
        }

        public Quantity GetQuantity()
        {
            return inUseQuantity.IntegerPart;
        }

        public void AddProduct(PartialProductQuantity deltaPq)
        {
            if (!deltaPq.Quantity.IsNotPositive)
            {
                if (bufferedProducts.ContainsKey(deltaPq.Product))
                {
                    ref KeyValuePair<ProductProto, PartialQuantity> valueAsRef = ref LystStructExtensions.GetValueAsRef(ref bufferedProducts, deltaPq.Product);
                    valueAsRef = Make.Kvp(valueAsRef.Key, valueAsRef.Value + deltaPq.Quantity);
                }
                else
                {
                    bufferedProducts.Add(deltaPq.Product, deltaPq.Quantity);
                }

                inUseQuantity += deltaPq.Quantity;
            }
        }

        public void AddProduct(ProductQuantity deltaQ)
        {
            AddProduct(new PartialProductQuantity( deltaQ.Product, new PartialQuantity(deltaQ.Quantity)));
        }


        internal void removeProduct(ProductProto p)
        {
            if (bufferedProducts.ContainsKey(p))
            {
                inUseQuantity -= bufferedProducts.GetValueOrDefault(p);
                bufferedProducts.Remove(p);
            }
        }

        internal void removeProductQuantity(ProductQuantity pq)
        {
            if (bufferedProducts.ContainsKey(pq.Product))
            {
                ref KeyValuePair<ProductProto, PartialQuantity> valueAsRef = ref LystStructExtensions.GetValueAsRef(ref bufferedProducts, pq.Product);
                valueAsRef = Make.Kvp(valueAsRef.Key, (valueAsRef.Value - new PartialQuantity(Fix32.FromInt(pq.Quantity.Value))).Max(PartialQuantity.Zero)) ;
                inUseQuantity -= new PartialQuantity(Fix32.FromInt(pq.Quantity.Value)).Max(PartialQuantity.Zero);
            }
        }

        public LystStruct<LooseProductQuantity> FinalProductsReadonly()
        {
            finalProducts.ClearSkipZeroingMemory();
            LystStruct<KeyValuePair<ProductProto, PartialQuantity>>.Enumerator enumerator = bufferedProducts.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<ProductProto, PartialQuantity> current = enumerator.Current;
                finalProducts.Add(new LooseProductQuantity((LooseProductProto)current.Key, current.Value.IntegerPart));
            }

            return finalProducts;
        }

        public ProductQuantity getSomeProduct(Quantity q)
        {
            ProductQuantity returnQuantity = ProductQuantity.None;
            LystStruct<KeyValuePair<ProductProto, PartialQuantity>>.Enumerator enumerator = bufferedProducts.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<ProductProto, PartialQuantity> current = enumerator.Current;
         
                if (current.Value.IntegerPart > Quantity.Zero )
                {
                    returnQuantity = (new ProductQuantity(current.Key, current.Value.IntegerPart.Min(q)));
                    break;
                }
            }
            removeProductQuantity(returnQuantity);
            return returnQuantity;
        }

        public override string ToString()
        {
            return (IsFull ? "Full" : "Not full") + " " + inUseQuantity.ToString() + $" pc = {bufferedProducts.Count}"; 
        }

        private static readonly Action<object, BlobWriter> s_serializeDataDelayedAction;

        private static readonly Action<object, BlobReader> s_deserializeDataDelayedAction;

        public static void Serialize(PartialProductsBuffer value, BlobWriter writer)
        {
            if (writer.TryStartClassSerialization(value))
            {
                writer.EnqueueDataSerialization(value, s_serializeDataDelayedAction);
            }
        }

        protected void SerializeData(BlobWriter writer)
        {
            LystStruct<KeyValuePair<ProductProto, PartialQuantity>>.Serialize(bufferedProducts, writer);
            LystStruct<LooseProductQuantity>.Serialize(finalProducts, writer);
            PartialQuantity.Serialize(maxQuantity, writer);
            PartialQuantity.Serialize(inUseQuantity, writer);
    }

        public static PartialProductsBuffer Deserialize(BlobReader reader)
        {
            if (reader.TryStartClassDeserialization(out PartialProductsBuffer obj, (Func<BlobReader, Type, PartialProductsBuffer>)null))
            {
                reader.EnqueueDataDeserialization(obj, s_deserializeDataDelayedAction);
            }
            return obj;
        }

        protected void DeserializeData(BlobReader reader)
        {
            bufferedProducts = LystStruct<KeyValuePair<ProductProto, PartialQuantity>>.Deserialize(reader);
            finalProducts = LystStruct<LooseProductQuantity>.Deserialize(reader);
            maxQuantity = PartialQuantity.Deserialize(reader);
            inUseQuantity = PartialQuantity.Deserialize(reader);
        }

        static PartialProductsBuffer()
        {
            s_serializeDataDelayedAction = delegate (object obj, BlobWriter writer)
            {
                ((PartialProductsBuffer)obj).SerializeData(writer);
            };
            s_deserializeDataDelayedAction = delegate (object obj, BlobReader reader)
            {
                ((PartialProductsBuffer)obj).DeserializeData(reader);
            };
        }
    }
}
