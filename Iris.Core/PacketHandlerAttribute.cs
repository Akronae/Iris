using System;

namespace Iris.Core
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PacketHandlerAttribute : Attribute
    {
        public readonly ConnectionState MinimumRequiredState;

        public PacketHandlerAttribute (ConnectionState minimumRequiredState)
        {
            MinimumRequiredState = minimumRequiredState;
        }
    }
}