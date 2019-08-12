using System.Linq;
using Chresimos.Core;

namespace Iris.Core
{
    public class ReliabilityFrame <T> where T : PacketEndPointConnection
    {
        private readonly PacketUdpClient<T> _client;
        
        public ReliabilityFrame (PacketUdpClient<T> client)
        {
            _client = client;
            _client.MessageDispatcher.RegisterPacketHandlersFrom(this);
        }

        [PacketHandler(ConnectionState.All)]
        public void HandlePacketResentRequestPacket (PacketResentRequestPacket packet, T client)
        {
            var resentPacket = client.GetSentPacket(packet.ResentPacketNumber);
            var resent = _client.Serializer.Serialize(resentPacket).ToList();
            
            // We must not go through the reliability process etc. of _client.Send().
            _client.SendRawPacket(new PacketResentPacket(resent), client);
            
            LogUtils.Log($"{_client} resent {resentPacket} to {client}");
        }
        
        [PacketHandler(ConnectionState.All)]
        public void HandlePacketResentPacket (PacketResentPacket packet, T client)
        {
            _client.HandleResentPacket(packet.ResentPacket.ToArray(), client);
        }
        
        [PacketHandler(ConnectionState.All)]
        public void HandlePacketAcknowledgmentRequestPacket (PacketAcknowledgmentRequestPacket packet, T client)
        {
            if (client.HasReceivedPacket(packet.PacketNumberToAcknowledge))
            {
                _client.Send(new PacketAcknowledgedPacket(packet.PacketNumberToAcknowledge), client);
                LogUtils.Log($"{_client} acknowledged packet number {packet.PacketNumberToAcknowledge} to {client}");
            }
            else
            {
                _client.Send(new PacketResentRequestPacket(packet.PacketNumberToAcknowledge), client);
                LogUtils.Log($"{_client} asked {client} to resend packet number {packet.PacketNumberToAcknowledge}");
            }
        }
        
        [PacketHandler(ConnectionState.All)]
        public void HandlePacketAcknowledgedPacket (PacketAcknowledgedPacket packet, T client)
        {
            LogUtils.Log($"{client} acknowledged packet number {packet.PacketNumberAcknowledged}");
            client.AcknowledgePacket(packet.PacketNumberAcknowledged);
        }
    }
}