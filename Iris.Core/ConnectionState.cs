using System;

namespace Iris.Core
{
    [Flags]
    public enum ConnectionState
    {
        Unidentified,
        Handshaked,
        GameJoined,
        All
    }
}