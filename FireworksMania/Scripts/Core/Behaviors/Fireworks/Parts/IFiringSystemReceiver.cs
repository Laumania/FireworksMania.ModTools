using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    public interface IFiringSystemReceiver
    {
        event Action OnFiringSystemReceiverDataUpdated;
        FiringSystemReceiverData FiringSystemReceiverData { get; set; }
        Vector3 GetFiringSystemReceiverWorldPosition();
    }

    [Serializable]
    public struct FiringSystemReceiverData : INetworkSerializable, System.IEquatable<FiringSystemReceiverData>
    {
        public byte CueIndex;
        public byte ModuleIndex;

        public bool HasValue => CueIndex > 0 && ModuleIndex > 0;

        public bool Equals(FiringSystemReceiverData other)
        {
            return CueIndex == other.CueIndex && ModuleIndex == other.ModuleIndex;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (CueIndex * 397) ^ ModuleIndex;
            }
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out ModuleIndex);
                reader.ReadValueSafe(out CueIndex);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(ModuleIndex);
                writer.WriteValueSafe(CueIndex);
            }
        }
    }
}
