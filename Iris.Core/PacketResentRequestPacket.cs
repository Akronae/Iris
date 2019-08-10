using Proteus.Core;

namespace Iris.Core
{
    [Packet(PacketEndPointConnection.ReliabilityProtocolId, 0)]
    public class PacketResentRequestPacket : Packet
    {
        [SerializedMember(0)]
        public int ResentPacketNumber;

        public PacketResentRequestPacket (int resentPacketNumber)
        {
            ResentPacketNumber = resentPacketNumber;
        }

        public PacketResentRequestPacket ()
        {
            
        }
    }
}