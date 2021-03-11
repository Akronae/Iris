using System;
using System.Linq;
using Chresimos.Core.Utils;
using Proteus.Core;

namespace Iris.Core
{
    public class Packet
    {
        public const int UndefinedPacketNumber = -1;
        private int _packetId = PacketAttribute.UndefinedPacketId;
        private int _protocolId = PacketAttribute.UndefinedProtocolId;
        private PacketReliability? _reliability;

        public PacketReliability Reliability
        {
            get
            {
                if (_reliability == null)
                {
                    _reliability = GetType().GetCustomAttribute<PacketReliabilityAttribute>(true)?.Reliability;
                }

                return _reliability ?? PacketReliability.None;
            }
        }

        [SerializedMember(0)]
        public int PackedId
        {
            get
            {
                if (_packetId != PacketAttribute.UndefinedPacketId) return _packetId;

                var attrs = GetType().GetCustomAttributes(typeof(PacketAttribute), false);
                var attr = (PacketAttribute) attrs.FirstOrDefault();

                if (attr != null) return attr.PackedId;

                throw LogUtils.Throw(new Exception($"{nameof(PacketAttribute)} not set on {GetType().FullName}"));
            }
            set => _packetId = value;
        }
        
        [SerializedMember(1)]
        public int ProtocolId
        {
            get
            {
                if (_protocolId != PacketAttribute.UndefinedProtocolId) return _protocolId;

                var attrs = GetType().GetCustomAttributes(typeof(PacketAttribute), false);
                var attr = (PacketAttribute) attrs.FirstOrDefault();

                if (attr != null) return attr.ProtocolId;

                throw LogUtils.Throw(new Exception($"{nameof(PacketAttribute)} not set on {GetType().FullName}"));
            }
            set => _protocolId = value;
        }

        [SerializedMember(2)]
        public int PacketNumber = UndefinedPacketNumber;

        [SerializedMember(3)]
        public int DoNotProceedBeforePacketNumber = UndefinedPacketNumber;

        public override string ToString ()
        {
            return $"{GetType().NameWithGeneric()} ({PacketNumber})";
        }
    }
}