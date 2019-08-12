using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using Chresimos.Core;
using Proteus.Core;

namespace Iris.Core
{
    public abstract class PacketUdpClient <T> :  NetworkUdpClient<T> where T : PacketEndPointConnection
    {
        private const float ClientRoutineIntervalSeconds = 2f;
        
        public readonly Serializer Serializer = new Serializer(new LoadedAssembliesGenericTypesProvider());
        public readonly MessageDispatcher MessageDispatcher;

        private readonly ReliabilityFrame<T> _reliabilityFrame;
        private readonly Timer _routineTimer;
        
        protected PacketUdpClient (T defaultEndPoint, IEnumerable<Assembly> protocolAssemblies, string serverName,
            int listenPort = 0) : base(defaultEndPoint, serverName, listenPort)
        {
            if (protocolAssemblies == null)
            {
                protocolAssemblies = Enumerable.Empty<Assembly>();
            }
            
            var assemblies = protocolAssemblies.ToArray().ToList();
            assemblies.Add(typeof(PacketResentRequestPacket).Assembly);
                
            MessageDispatcher = new MessageDispatcher(Serializer, assemblies);
            _reliabilityFrame = new ReliabilityFrame<T>(this);

            _routineTimer = new Timer(_ => LaunchClientsRoutine(), null, 0, (int) (ClientRoutineIntervalSeconds * 1000));
        }

        protected PacketUdpClient (IEnumerable<Assembly> protocolAssemblies, string serverName, int listenPort = 0) : this(null, protocolAssemblies, serverName, listenPort)
        {
        }

        public void HandleResentPacket (byte[] packet, T client)
        {
            OnDataReceive(packet, client.EndPoint);
        }

        protected virtual void OnPacketReceived (byte[] rawPacket, Packet packet, IPEndPoint endPoint)
        {
            var udpEndPoint = AddClient(endPoint);

            var unprocessedPackets = udpEndPoint.RemovePacketsWaitingFor(packet.PacketNumber).OrderBy(p => p.PacketNumber);
            
            var receiveResult = udpEndPoint.AddToReceivedPackets(packet);
            switch (receiveResult)
            {
                case PacketEndPointConnection.ReceivePacketResult.Received:
                    break;
                case PacketEndPointConnection.ReceivePacketResult.PacketNumberAlreadyReceived:
                    return;
                case PacketEndPointConnection.ReceivePacketResult.MustWaitForAnotherPacket:
                    LogUtils.Warn($"Cannot process {packet} need to wait for {packet.DoNotProceedBeforePacketNumber}");
                    return;
                case PacketEndPointConnection.ReceivePacketResult.PacketNumberAlreadyProcessed:
                    Log($"{endPoint} sent {packet} while it as already been processed.");
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(receiveResult));
            }
            
            MessageDispatcher.HandlePacket(packet, udpEndPoint, udpEndPoint);
            
            foreach (var unprocessedPacket in unprocessedPackets)
            {
                Log($"Processing {unprocessedPacket} which was waiting for {packet}");
                udpEndPoint.ProcessPacket(unprocessedPacket);
                OnPacketReceived(ListUtils.EmptyByteArray, unprocessedPacket, endPoint);
            }
        }
        
        protected override void OnDataReceive (byte[] data, IPEndPoint endPoint)
        {
            Packet packet;
            try
            {
                packet = MessageDispatcher.DeserializePacket(data);
            }
            catch (Exception e)
            {
                Error(
                    $"Could not deserialize {BitConverter.ToString(data)} sent by {(object) GetClientByIpEndPointOrDefault(endPoint) ?? endPoint}" +
                    $"\n{e}");
                
                return;
            }

            OnPacketReceived(data, packet, endPoint);
        }

        public virtual byte[] Send (Packet packet)
        {
            if (DefaultEndPoint == null)
            {
                LogUtils.Throw(new Exception(
                    $"Called {nameof(Send)} method without end point argument and no default end point is set."));
            }
            
            return Send(packet, DefaultEndPoint);
        }

        public virtual byte[] Send (Packet packet, T client)
        {
            client.AddToSentPackets(packet);

            var data = SendRawPacket(packet, client);
            
            if (packet.Reliability.IsFlagSet(PacketReliability.SendTwice))
            {
                Send(data, client.EndPoint);
            }

            return data;
        }

        public virtual byte[] SendRawPacket (Packet packet, T client)
        {
            if (packet is GenericPacket gp) gp.GenericTypeId = Serializer.GenericTypesProvider.GetTypeId(gp.GenericType);
            
            var data = Serializer.Serialize(packet);
            
            Send(data, client.EndPoint);

            return data;
        }
        
        private void LaunchClientsRoutine ()
        {
            foreach (var client in Clients.ToArray())
            {
                ClientRoutine(client);
            }
        }

        public virtual void ClientRoutine (T client)
        {
            var unacknowledgedPackets = client.GetUnacknowledgedPackets().OrderBy(p => p.PacketNumber);
            
            foreach (var unacknowledgedPacket in unacknowledgedPackets)
            {
                Log($"Requested {client} to acknowledge packet {unacknowledgedPacket}");
                Send(new PacketAcknowledgmentRequestPacket(unacknowledgedPacket.PacketNumber), client);
            }
        }

        public override void Dispose ()
        {
            Disposed = true;
            
            _routineTimer?.Dispose();
            base.Dispose();
        }
    }
}