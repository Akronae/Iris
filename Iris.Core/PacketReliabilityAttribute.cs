using System;

namespace Iris.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PacketReliabilityAttribute : Attribute
    {
        public readonly PacketReliability Reliability;
        
        public PacketReliabilityAttribute (PacketReliability reliability)
        {
            Reliability = reliability;
        }
    }
}