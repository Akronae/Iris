using System;

namespace Iris.Core
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PacketHandlerAttribute : Attribute
    {
        public readonly byte MinimumRequiredState;

        public PacketHandlerAttribute (byte minimumRequiredState)
        {
            MinimumRequiredState = minimumRequiredState;
        }
    }
}