using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Chresimos.Core;

namespace Iris.Core
{
    public abstract class NetworkUdpClient <T> : IDisposable where T : EndPointConnection
    {
            
        public readonly List<T> Clients = new List<T>();
        public bool IsConnected => Connection.Client.Connected;
        protected readonly UdpClient Connection;
        public readonly T DefaultEndPoint;
        protected readonly string ServerName;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public WaitHandle WaitHandle => _cancellationTokenSource.Token.WaitHandle;
        protected bool Disposed;
        
        protected NetworkUdpClient (T defaultEndPoint, string serverName, int listenPort = 0) : this(listenPort)
        {
            ServerName = serverName;
            DefaultEndPoint = defaultEndPoint;
            if (DefaultEndPoint != null) Clients.Add(DefaultEndPoint);
        }

        protected NetworkUdpClient (int listenPort)
        {
            Connection = new UdpClient(listenPort);

            Connection.BeginReceive(OnReceive, null);
        }
        
        private void OnReceive (IAsyncResult ar)
        {
            if (Disposed) return;
            
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

        public virtual void Dispose ()
        {
            Disposed = true;
            
            Close();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            Connection?.Dispose();
            
            GC.SuppressFinalize(this);
        }

        public void Close ()
        {
            Connection.Close();
        }

        protected virtual T AddClient (IPEndPoint ipEndPoint)
        {
            var udpEndPoint = GetClientByIpEndPointOrDefault(ipEndPoint);
            if (udpEndPoint != null) return udpEndPoint;
            
            udpEndPoint = CreateEndPointConnection(ipEndPoint);
            Clients.Add(udpEndPoint);

            return udpEndPoint;
        }

        protected abstract T CreateEndPointConnection (IPEndPoint ipEndPoint);
        
        public T GetClientByConnIdOrDefault (string id)
        {
            return Clients.SingleOrDefault(c => c.Id == id);
        }
        
        public T GetClientByIpEndPointOrDefault (IPEndPoint endPoint)
        {
            return Clients.SingleOrDefault(c => Equals(c.EndPoint, endPoint));
        }

        protected virtual void Log (string message)
        {
            LogUtils.Log($"[{ServerName}]: {message}");
        }
        
        protected virtual void Warn (string message)
        {
            LogUtils.Warn($"[{ServerName}]: {message}");
        }
        
        protected virtual void Error (string message)
        {
            LogUtils.Error($"[{ServerName}]: {message}");
        }

        public override string ToString ()
        {
            return ServerName;
        }
    }
}