using System.Collections.Generic;
using Proteus.Core;

namespace Iris.Core
{
    [Packet(PacketEndPointConnection.ReliabilityProtocolId, 3)]
    public class PacketResentPacket : Packet
    {
        [SerializedMember(0)]
        public List<byte> ResentPacket;

        public PacketResentPacket (List<byte> resentPacket)
        {
            ResentPacket = resentPacket;
        }

        public PacketResentPacket ()
        {
        }
    }
}