using System.Net;
using Chresimos.Core.Utils;

namespace Iris.Core
{
    public class EndPointConnection : IEndPointConnection
    {
        public string Id { get; } = RandUtils.RandomId();
        public byte ConnectionState { get; set; } = Core.ConnectionState.Unidentified;
        
        public readonly IPEndPoint EndPoint;

        public EndPointConnection (IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }

        public override string ToString ()
        {
            return $"{EndPoint} (Id {Id})";
        }
    }
}