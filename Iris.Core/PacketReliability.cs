using System;

namespace Iris.Core
{
    [Flags]
    public enum PacketReliability
    {
        None,
        MustBeAcknowledged,
        MustBeProcessedBeforeLaterPackets,
        RequiredToFollowingPackets = MustBeProcessedBeforeLaterPackets | MustBeAcknowledged,
        SendTwice
    }
}