using System;
using Unity.Netcode;

namespace FireworksMania.Core.Netcode
{
    [Serializable]
    public struct FuseNetworkIdentifier : INetworkSerializable, System.IEquatable<FuseNetworkIdentifier>
    {
        public ulong FuseNetworkObjectId;
        public ulong FuseNetworkBehaviorId;
        public int FuseIndex;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out FuseNetworkObjectId);
                reader.ReadValueSafe(out FuseNetworkBehaviorId);
                reader.ReadValueSafe(out FuseIndex);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(FuseNetworkObjectId);
                writer.WriteValueSafe(FuseNetworkBehaviorId);
                writer.WriteValueSafe(FuseIndex);
            }
        }

        public bool Equals(FuseNetworkIdentifier other)
        {
            return FuseNetworkObjectId == other.FuseNetworkObjectId &&
                   FuseNetworkBehaviorId == other.FuseNetworkBehaviorId &&
                   FuseIndex == other.FuseIndex;
        }
    }
}
