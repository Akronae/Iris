using Proteus.Core;

namespace Iris.Core
{
    [Packet(PacketEndPointConnection.ReliabilityProtocolId, 1)]
    public class PacketAcknowledgmentRequestPacket : Packet
    {
        [SerializedMember(0)]
        public int PacketNumberToAcknowledge;

        public PacketAcknowledgmentRequestPacket (int packetNumberToAcknowledge)
        {
            PacketNumberToAcknowledge = packetNumberToAcknowledge;
        }

        public PacketAcknowledgmentRequestPacket ()
        {
            
        }
    }
}