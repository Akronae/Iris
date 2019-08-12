namespace Iris.Core
{
    public abstract class Frame <T1, T2> where T1 : PacketUdpClient<T2> where T2 : PacketEndPointConnection 
    {
        protected readonly T1 Connection;

        protected Frame (T1 connection)
        {
            Connection = connection;
            Connection.MessageDispatcher.RegisterPacketHandlersFrom(this);
        }
    }
}