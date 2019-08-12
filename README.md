# Iris
<img src="https://camo.githubusercontent.com/c6c9631419b18b24346a3ad71d6390af685e0b19/68747470733a2f2f696d616765732e66696e65617274616d65726963612e636f6d2f696d616765732f617274776f726b696d616765732f6d656469756d6c617267652f322f697269732d313830302d6775792d686561642e6a7067" width="20%" />

Modern reliable UDP framework for .NET &amp; Mono.

# [Download the NuGet package](https://github.com/Akronae/Iris/tree/master/nuget)

# Documentation
## Basic
```cs
public class EchoUdpClient : NetworkUdpClient<EndPointConnection>
{
    public EchoUdpClient (EndPointConnection defaultEndPoint, string serverName, int listenPort = 0)
    : base(defaultEndPoint, serverName, listenPort)
    {
    }
    
    protected override EndPointConnection CreateEndPointConnection (IPEndPoint ipEndPoint)
    {
        return new EndPointConnection(ipEndPoint);
    }
    
    protected override void OnDataReceive (byte[] data, IPEndPoint endPoint)
    {
        var received = Encoding.ASCII.GetString(data);
        Console.WriteLine($"{endPoint} sent {received}");

        if (received.EndsWith("\n")) return;
        
        Send(Encoding.ASCII.GetBytes(received + "\n"), endPoint);
    }
}

const int serverPort = 8080;

// The first argument is the default endpoint which is used as receiver if `server.Send` is called
// without endpoint argument. Being a server, there is no default endpoint.
// Second is the client name, which is used for logging.
var server = new EchoUdpClient(null, "UPD Server", serverPort);

var endPoint = new EndPointConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), serverPort));
var client = new EchoUdpClient(endPoint, "UDP Client");
client.Send(Encoding.ASCII.GetBytes("Hello server!"));

// Prevent the thread on which the server is running from terminating.
server.WaitHandle.WaitOne();

/**
OUTPUT:
127.0.0.1:49663 sent Hello server!
127.0.0.1:8080 sent Hello server!
**/
```

```cs
// Now inherits from `PacketUdpClient` which handles packets instead of `NetworkUdpClient`.
public class EchoUdpClient : PacketUdpClient<PacketEndPointConnection>
{
    public EchoUdpClient (PacketEndPointConnection defaultEndPoint, IEnumerable<Assembly> protocolAssemblies, string serverName, int listenPort = 0) : base(defaultEndPoint, protocolAssemblies, serverName, listenPort)
    {
    }
    
    protected override PacketEndPointConnection CreateEndPointConnection (IPEndPoint ipEndPoint)
    {
        return new PacketEndPointConnection(ipEndPoint);
    }

    protected override void OnPacketReceived (byte[] rawPacket, Packet packet, IPEndPoint endPoint)
    {
        base.OnPacketReceived(rawPacket, packet, endPoint);

        if (!(packet is MessagePacket mp)) return;

        var sender = GetClientByIpEndPointOrDefault(endPoint);
        Console.WriteLine($"{endPoint} sent {mp.Message}");

        if (!mp.Message.EndsWith("\n"))
        {
            Send(new MessagePacket(mp.Message + "\n"), sender);
        }
    }
}

// The first argument is a unique packet ID inside a protocol ID (second argument).
[Packet(100, 0)]
public class MessagePacket : Packet
{
    [SerializedMember(0)]
    public string Message;

    public MessagePacket (string message)
    {
        Message = message;
    }

    // As long as there constructors in packets, there must be an empty constructor for the packet to be
    // instantiated by reflection during deserialization.
    public MessagePacket ()
    {
    }
}

const int serverPort = 8080;
// A `PacketUdpClient` needs to know which packets are referenced for deserializing.
var protocolAssembly = new [] {typeof(MessagePacket).Assembly};

var server = new EchoUdpClient(null, protocolAssembly, "UPD Server", serverPort);

var endPoint = new PacketEndPointConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080));
var client = new EchoUdpClient(endPoint, protocolAssembly, "UDP Client");

client.Send(new MessagePacket("Hey server!"));

server.WaitHandle.WaitOne();

/**
OUTPUT:
127.0.0.1:50976 sent Hey server!
127.0.0.1:8080 sent Hey server!
**/
```
