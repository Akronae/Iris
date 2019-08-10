using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Chresimos.Core;

namespace Iris.Core
{
    public abstract class NetworkUdpClient <T> where T : EndPointConnection
    {
        public readonly List<T> Clients = new List<T>();
        public bool IsConnected => Connection.Client.Connected;
        protected readonly UdpClient Connection;
        public readonly T DefaultEndPoint;
        protected readonly string ServerName;
        protected NetworkUdpClient (int port, T defaultEndPoint, string serverName) : this(port)
        {
            ServerName = serverName;
            DefaultEndPoint = defaultEndPoint;
            if (DefaultEndPoint != null) Clients.Add(DefaultEndPoint);
        }

        protected NetworkUdpClient (int port)
        {
            Connection = new UdpClient(port);

            Connection.BeginReceive(OnReceive, null);
        }
        
        private void OnReceive (IAsyncResult ar)
        {
            IPEndPoint endPoint = null;
            var data = Connection.EndReceive(ar, ref endPoint);
            OnDataReceive(data, endPoint);

            Connection.BeginReceive(OnReceive, ar);
        }

        protected abstract void OnDataReceive (byte[] data, IPEndPoint endPoint);
        
        public virtual void Send (byte[] data)
        {
            if (DefaultEndPoint is null)
            {
                LogUtils.Throw(new Exception("Cannot send data to default EndPoint as it is not defined"));
            }
            
            Send(data, DefaultEndPoint.EndPoint);
        }
        
        public virtual void Send (byte[] data, IPEndPoint endPoint)
        {
            Connection.Send(data, data.Length, endPoint);
        }

        public void Close ()
        {
            Connection.Close();
        }

        protected virtual T AddClient (IPEndPoint ipEndPoint)
        {
            var udpEndPoint = Clients.Find(c => Equals(c.EndPoint, ipEndPoint));
            if (udpEndPoint != null) return udpEndPoint;
            
            udpEndPoint = CreateEndPointConnection(ipEndPoint);
            Clients.Add(udpEndPoint);

            return udpEndPoint;
        }

        protected abstract T CreateEndPointConnection (IPEndPoint ipEndPoint);
        
        public T GetClientByConnId (string id)
        {
            return Clients.SingleOrDefault(c => c.Id == id);
        }

        protected virtual void Log (string message)
        {
            LogUtils.Log($"[{ServerName}]: {message}");
        }
        
        protected virtual void Warn (string message)
        {
            LogUtils.Warn($"[{ServerName}]: {message}");
        }
    }
}