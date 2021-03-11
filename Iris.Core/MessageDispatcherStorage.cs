using System;
using System.Collections.Generic;
using Chresimos.Core.Utils;

namespace Iris.Core
{
    public class MessageDispatcherStorage
    {
        private readonly Dictionary<PacketIdentifier, Type> _packetTypes = new Dictionary<PacketIdentifier, Type>();

        public void StorePacketType (Packet packet)
        {
            StorePacketType(new PacketIdentifier(packet), packet.GetType());
        }

        public void StorePacketType (PacketIdentifier identifier, Type type)
        {
            if (HasPacketType(identifier))
            {
                if (RetrievePacketType(identifier) == type) return;

                throw LogUtils.Throw($"{RetrievePacketType(identifier).FullName} has the same ID as {type.FullName}");
            }

            if (!type.HasParameterlessConstructor())
            {
                throw LogUtils.Throw($"{type.FullName} must have a parameterless constructor in order to be deserialized.");
            }
            
            _packetTypes.Add(identifier, type);
        }

        public Type RetrievePacketType (Packet packet)
        {
            return _packetTypes[new PacketIdentifier(packet)];
        }

        public Type RetrievePacketType (PacketIdentifier identifier)
        {
            return _packetTypes[identifier];
        }

        public bool HasPacketType (Packet packet)
        {
            return _packetTypes.ContainsKey(new PacketIdentifier(packet));
        }

        public bool HasPacketType (PacketIdentifier identifier)
        {
            return _packetTypes.ContainsKey(identifier);
        }

        public struct PacketIdentifier
        {
            public readonly int ProtocolId;
            public readonly int PacketId;

            public PacketIdentifier (int protocolId, int packetId)
            {
                ProtocolId = protocolId;
                PacketId = packetId;
            }

            public PacketIdentifier (Packet packet)
            {
                ProtocolId = packet.ProtocolId;
                PacketId = packet.PackedId;
            }
        }
    }
}