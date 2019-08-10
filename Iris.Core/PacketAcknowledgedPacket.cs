using Proteus.Core;

namespace Iris.Core
{
    [Packet(PacketEndPointConnection.ReliabilityProtocolId, 2)]
    public class PacketAcknowledgedPacket : Packet
    {
        [SerializedMember(0)]
        public int PacketNumberAcknowledged;

        public PacketAcknowledgedPacket (int packetNumberAcknowledged)
        {
            PacketNumberAcknowledged = packetNumberAcknowledged;
        }

        public PacketAcknowledgedPacket ()
        {
            
        }
    }
}