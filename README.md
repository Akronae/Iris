# Iris
<img src="https://camo.githubusercontent.com/c6c9631419b18b24346a3ad71d6390af685e0b19/68747470733a2f2f696d616765732e66696e65617274616d65726963612e636f6d2f696d616765732f617274776f726b696d616765732f6d656469756d6c617267652f322f697269732d313830302d6775792d686561642e6a7067" width="20%" />

Modern reliable UDP framework for .NET &amp; Mono.

# [Download the NuGet package](https://github.com/Akronae/Iris/tree/master/nuget)

# Documentation
## Basic
```cs
public class EchoUdpClient : NetworkUdpClient<EndPointConnection>
{
    public EchoUdpClient (NetworkUdpClientConfiguration<EndPointConnection> configuration) : base(configuration)
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
var serverConfig = new NetworkUdpClientConfiguration<EndPointConnection
{
    ServerName = "UDP Server",
    ListenPort = serverPort
};
var server = new EchoUdpClient(serverConfig);

var endPoint = new EndPointConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), serverPort));
var clientConfig = new NetworkUdpClientConfiguration<EndPointConnection>
{
    DefaultEndPoint = endPoint,
    ServerName = "UDP Client",
};
var client = new EchoUdpClient(clientConfig);

client.Send(Encoding.ASCII.GetBytes("Hello server!"));

// Prevent the thread on which the server is running from terminating.
server.WaitHandle.WaitOne();

/**
OUTPUT:
127.0.0.1:49663 sent Hello server!
127.0.0.1:8080 sent Hello server!
**/
```
## Using Packets
```cs
// Now inherits from `PacketUdpClient` which handles packets instead of `NetworkUdpClient`.
public class EchoUdpClient : PacketUdpClient<PacketEndPointConnection>
{
    public EchoUdpClient (PacketUdpClientConfiguration<PacketEndPointConnection> configuration) : base(configuration)
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
[PacketReliability(PacketReliability.SendTwice | PacketReliability.MustBeAcknowledged)]
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
var protocolAssembly = typeof(MessagePacket).Assembly;

var serverConfig = new PacketUdpClientConfiguration<PacketEndPointConnection>
{
    ServerName = "UDP Server",
    ListenPort = serverPort
}.AddProtocolAssembly(protocolAssembly);
var server = new EchoUdpClient(serverConfig);

var endPoint = new PacketEndPointConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), serverPort));
var clientConfig = new PacketUdpClientConfiguration<PacketEndPointConnection>
{
    DefaultEndPoint = endPoint,
    ServerName = "UDP Client",
}.AddProtocolAssembly(protocolAssembly);
var client = new EchoUdpClient(clientConfig);

client.Send(new MessagePacket("Hey server!"));

server.WaitHandle.WaitOne();

/**
OUTPUT:
127.0.0.1:50711 (Id 54152) sent Hey server!
127.0.0.1:8080 (Id 65611) sent Hey server!

[UPD Server]: Requested 127.0.0.1:50711 (Id 54152) to acknowledge packet MessagePacket (0)
UDP Client acknowledged packet number 0 to 127.0.0.1:8080 (Id 65611)
127.0.0.1:50711 (Id 54152) acknowledged packet number 0
[UDP Client]: Requested 127.0.0.1:8080 (Id 65611) to acknowledge packet MessagePacket (0)
UPD Server acknowledged packet number 0 to 127.0.0.1:50711 (Id 54152)
127.0.0.1:8080 (Id 65611) acknowledged packet number 0
**/
```

## Using Frames
```cs
public class MessageFrame : Frame<PacketUdpClient<PacketEndPointConnection>, PacketEndPointConnection>
{
    public MessageFrame (PacketUdpClient<PacketEndPointConnection> server) : base(server)
    {
    }
    
    // This handler will always be called because it is protected with `ConnectionState.All`.
    // The connection state is set on `sender.ConnectionState`.
    // The class `ConnectionState` can be inherited to create custom states,
    // be careful though to not define new states on values already taken by `ConnectionState`. 
    [PacketHandler(ConnectionState.All)]
    public void HandleMessagePacket (MessagePacket packet, PacketEndPointConnection sender)
    {
        LogUtils.Log($"{sender} sent {packet.Message}");
        
        if (!packet.Message.EndsWith("\n"))
        {
            Connection.Send(new MessagePacket(packet.Message + "\n"), sender);
        }
    }
}

public class EchoUdpClient : PacketUdpClient<PacketEndPointConnection>
{
    public EchoUdpClient (PacketUdpClientConfiguration<PacketEndPointConnection> configuration) : base(configuration)
    {
        // Each frame need to be instantiated by the client which is supposed to receive the packets.
        new MessageFrame(this);
    }
    
    protected override PacketEndPointConnection CreateEndPointConnection (IPEndPoint ipEndPoint)
    {
        return new PacketEndPointConnection(ipEndPoint);
    }
}

[Packet(100, 0)]
public class MessagePacket : Packet
{
    [SerializedMember(0)]
    public string Message;

    public MessagePacket (string message)
    {
        Message = message;
    }

    public MessagePacket ()
    {
    }
}

const int serverPort = 8080;
var protocolAssembly = typeof(MessagePacket).Assembly;

var serverConfig = new PacketUdpClientConfiguration<PacketEndPointConnection>
{
    ServerName = "UDP Server",
    ListenPort = serverPort
}.AddProtocolAssembly(protocolAssembly);
var server = new EchoUdpClient(serverConfig);

var endPoint = new PacketEndPointConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), serverPort));
var clientConfig = new PacketUdpClientConfiguration<PacketEndPointConnection>
{
    DefaultEndPoint = endPoint,
    ServerName = "UDP Client",
}.AddProtocolAssembly(protocolAssembly);
var client = new EchoUdpClient(clientConfig);

client.Send(new MessagePacket("Hey server!"));

server.WaitHandle.WaitOne();

/**
OUTPUT:
127.0.0.1:59899 (Id 47322) sent Hey server!
127.0.0.1:8080 (Id 90471) sent Hey server!
**/
```
