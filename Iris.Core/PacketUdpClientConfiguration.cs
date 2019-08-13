using System.Collections.Generic;
using System.Reflection;

namespace Iris.Core
{
    public class PacketUdpClientConfiguration <TEndPoint> : NetworkUdpClientConfiguration<TEndPoint>
    {
        public readonly List<Assembly> ProtocolAssemblies = new List<Assembly>()
        {
            // Reliability protocol assembly.
            typeof(PacketResentRequestPacket).Assembly
        };

        public PacketUdpClientConfiguration<TEndPoint> AddProtocolAssemblies (IEnumerable<Assembly> assemblies)
        {
            ProtocolAssemblies.AddRange(assemblies);
            
            return this;
        }
        
        public PacketUdpClientConfiguration<TEndPoint> AddProtocolAssembly (Assembly assembly)
        {
            AddProtocolAssemblies(new[] {assembly});
            
            return this;
        }
        
        public new PacketUdpClientConfiguration<TEndPoint> SetDefaultEndPoint (TEndPoint endPoint)
        {
            base.SetDefaultEndPoint(endPoint);

            return this;
        }
        
        public new PacketUdpClientConfiguration<TEndPoint> SetServerName (string serverName)
        {
            base.SetServerName(serverName);

            return this;
        }
        
        public new PacketUdpClientConfiguration<TEndPoint> SetListenPort (int listenPort)
        {
            base.SetListenPort(listenPort);

            return this;
        }
    }
}