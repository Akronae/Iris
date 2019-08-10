using System;

namespace Iris.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PacketAttribute : Attribute
    {
        public const int UndefinedPacketId = -1;
        public const int UndefinedProtocolId = -1;
        public readonly int PackedId;
        public readonly int ProtocolId;

        public PacketAttribute (int protocolId, int packedId)
        {
            PackedId = packedId;
            ProtocolId = protocolId;
        }
    }
}