using System.Collections.Generic;
using System.Linq;
using System.Net;
using Chresimos.Core.Utils;

namespace Iris.Core
{
    public class PacketEndPointConnection : EndPointConnection
    {
        public const int ReliabilityProtocolId = 0;

        public PacketEndPointConnection (IPEndPoint endPoint) : base(endPoint)
        {
        }

        private readonly Dictionary<int, Packet> _sentPackets = new Dictionary<int, Packet>();
        private readonly Dictionary<int, Packet> _receivedPackets = new Dictionary<int, Packet>();
        private readonly Dictionary<int, Packet> _unacknowledgedPackets = new Dictionary<int, Packet>();
        private readonly Dictionary<int, Packet> _processedPackets = new Dictionary<int, Packet>();
        private readonly Dictionary<int, Packet> _unprocessedPackets = new Dictionary<int, Packet>();
        private int _lastSentPacketNumber;
        private int _lastBlockingStreamSentPacketNumber = Packet.UndefinedPacketNumber;
        private readonly object SendLock = new object();

        public void AddToSentPackets (Packet packet)
        {
            lock (SendLock)
            {
                packet.PacketNumber = _lastSentPacketNumber++;
                packet.DoNotProceedBeforePacketNumber = _lastBlockingStreamSentPacketNumber;
            }

            if (packet.Reliability.IsFlagSet(PacketReliability.MustBeProcessedBeforeLaterPackets))
            {
                _lastBlockingStreamSentPacketNumber = packet.PacketNumber;
            }

            if (packet.Reliability.IsFlagSet(PacketReliability.MustBeAcknowledged))
            {
                if (_unacknowledgedPackets.ContainsKey(packet.PacketNumber))
                {
                    throw LogUtils.Throw(
                        $"Both {_unacknowledgedPackets[packet.PacketNumber]} and {packet} have the same packet number.");
                }
                _unacknowledgedPackets.Add(packet.PacketNumber, packet);
            }

            if (packet.ProtocolId == ReliabilityProtocolId)
            {
                packet.DoNotProceedBeforePacketNumber = Packet.UndefinedPacketNumber;
            }
            
            _sentPackets.Add(packet.PacketNumber, packet);
        }

        public ReceivePacketResult AddToReceivedPackets (Packet packet)
        {
            if (_receivedPackets.ContainsKey(packet.PacketNumber))
            {
                return ReceivePacketResult.PacketNumberAlreadyReceived;
            }
            
            _receivedPackets.Add(packet.PacketNumber, packet);

            if (HasReceivedPacket(packet.DoNotProceedBeforePacketNumber) == false)
            {
                _unprocessedPackets.Add(packet.PacketNumber, packet);
                
                return ReceivePacketResult.MustWaitForAnotherPacket;
            }

            if (_processedPackets.ContainsKey(packet.PacketNumber))
            {
                return ReceivePacketResult.PacketNumberAlreadyProcessed;
            }

            return ReceivePacketResult.Received;
        }

        public void ProcessPacket (Packet packet)
        {
            if (_processedPackets.ContainsKey(packet.PacketNumber))
            {
                return;
            }
            
            _processedPackets.Add(packet.PacketNumber, packet);
        }

        public Packet GetSentPacket (int packetNumber)
        {
            if (!_sentPackets.ContainsKey(packetNumber))
            {
                throw LogUtils.Throw(
                    new KeyNotFoundException(
                        $"Key {packetNumber} could not been found in {nameof(_sentPackets)} of {this}"));
            }
            
            return _sentPackets[packetNumber];
        }

        public Packet[] GetUnacknowledgedPackets ()
        {
            return _unacknowledgedPackets.Values.ToArray();
        }

        public void AcknowledgePacket (int packetNumber)
        {
            if (!_unacknowledgedPackets.ContainsKey(packetNumber))
            {
                LogUtils.Warn(
                    $"Tried to acknowledge packet number {packetNumber} in {this} but wasn't waiting for acknowledgment.");
                return;
            }
            
            _unacknowledgedPackets.Remove(packetNumber);
        }

        public List<Packet> RemovePacketsWaitingFor (int packetNumber)
        {
            var packets = new List<Packet>();

            for (var i = 0; i < _unprocessedPackets.Count; i++)
            {
                var packet = _unprocessedPackets.ElementAt(i).Value;
                if (packet.DoNotProceedBeforePacketNumber != packetNumber) continue;
                
                packets.Add(packet);
                _unprocessedPackets.Remove(packet.PacketNumber);
            }

            return packets;
        }

        public bool HasReceivedPacket (int packetNumber)
        {
            if (packetNumber == Packet.UndefinedPacketNumber) return true;
            return _receivedPackets.ContainsKey(packetNumber);
        }

        public enum ReceivePacketResult
        {
            PacketNumberAlreadyReceived,
            PacketNumberAlreadyProcessed,
            MustWaitForAnotherPacket,
            Received,
        }
    }
}