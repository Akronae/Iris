namespace Iris.Core
{
    public class NetworkUdpClientConfiguration <TEndPoint>
    {
        public TEndPoint DefaultEndPoint;
        public string ServerName;
        public int ListenPort;

        public NetworkUdpClientConfiguration<TEndPoint> SetDefaultEndPoint (TEndPoint endPoint)
        {
            DefaultEndPoint = endPoint;

            return this;
        }
        
        public NetworkUdpClientConfiguration<TEndPoint> SetServerName (string serverName)
        {
            ServerName = serverName;

            return this;
        }
        
        public NetworkUdpClientConfiguration<TEndPoint> SetListenPort (int listenPort)
        {
            ListenPort = listenPort;

            return this;
        }
    }
}