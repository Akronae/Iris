using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Chresimos.Core;
using Proteus.Core;

namespace Iris.Core
{
    public class MessageDispatcher
    {
        private readonly List<PacketHandler> _handlers = new List<PacketHandler>();
        private readonly MessageDispatcherStorage _storage = new MessageDispatcherStorage();

        private readonly Serializer _serializer;

        /// <summary>
        ///     Used to execute the packet handler's method invocation in another thread (ie. Unity main thread).
        /// </summary>
        public Action<Action> CallWrapper = a => { a(); };

        public MessageDispatcher (Serializer serializer, IEnumerable<Assembly> protocolAssemblies, Action<Action> callWrapper = null)
        {
            _serializer = serializer;
            CallWrapper = callWrapper ?? CallWrapper;

            foreach (var assembly in protocolAssemblies) RegisterAssembly(assembly);
        }

        public void HandlePacket (Packet packet, IEndPointConnection endPointConnection, object argument)
        {
            var handlers = new List<PacketHandler>();

            foreach (var handler in _handlers.ToArray())
            {
                var packetType = packet.GetType();
                if (handler.HandledType == packetType) handlers.Add(handler);

                if (handler.HandledType.IsGenericType && packetType.IsGenericType)
                {
                    var areSameBaseGenericType = handler.HandledType.GetGenericTypeDefinition() ==
                                                 packetType.GetGenericTypeDefinition();
                    if (!areSameBaseGenericType) continue;

                    var handlerGenParam = handler.HandledType.GetGenericArguments().First();
                    if (!handlerGenParam.IsGenericParameter) continue;

                    var constraint = handlerGenParam.GetGenericParameterConstraints().FirstOrDefault();
                    var packetGenParam = packetType.GetGenericArguments().First();

                    if (constraint != null)
                    {
                        if (packetGenParam.IsSubclassOf(constraint) || packetGenParam == constraint)
                            handlers.Add(handler);
                    }
                }
            }

            foreach (var handler in handlers)
            {
                if (endPointConnection != null)
                {
                    var playerState = endPointConnection.ConnectionState;

                    if (!playerState.IsFlagSet(handler.ConnectionState) && handler.ConnectionState != ConnectionState.All) continue;
                    if (!handler.Predicate(endPointConnection)) continue;
                }

                var args = new List<object> {packet};
                if (handler.AcceptsArgument) args.Add(argument);

                var method = handler.Method;
                if (method.IsGenericMethod)
                    method = method.MakeGenericMethod(packet.GetType().GetGenericArguments().First());

                CallWrapper(() => method.Invoke(handler.Target, args.ToArray()));
            }
        }

        public Packet DeserializePacket (byte[] data)
        {    
            var basePacket = _serializer.Deserialize<Packet>(data);

           
            if (!_storage.HasPacketType(basePacket))
            {
                LogUtils.Log($"Could not found packet associated with Protocol ID: {basePacket.ProtocolId} and Packet ID: {basePacket.PackedId}");
                return null;
            }

            var packetType = _storage.RetrievePacketType(basePacket);

            if (packetType.IsSubclassOf(typeof(GenericPacket)))
            {
                var genericPacket = _serializer.Deserialize<GenericPacket>(data);
                packetType = GetGenericPacketType(genericPacket);

                basePacket = genericPacket;
            }

            return (Packet) _serializer.Deserialize(packetType, data);
        }

        private Type GetGenericPacketType (GenericPacket packet)
        {
            var identifier = new MessageDispatcherStorage.PacketIdentifier(packet);
            if (!_storage.HasPacketType(identifier))
            {
                LogUtils.Log($"Could not found packet associated with Protocol ID: {packet.ProtocolId} and Packet ID: {packet.PackedId}");
                return null;
            }

            var baseGenericPacket = _storage.RetrievePacketType(identifier);
            var type = _serializer.GenericTypesProvider.GetType(packet.GenericTypeId);

            return baseGenericPacket.MakeGenericType(type);
        }

        public void RegisterPacketHandlersFrom (object holder)
        {
            RegisterPacketHandlersFrom(holder, connection => true);
        }

        public void RegisterPacketHandlersFrom (object holder, Func<IEndPointConnection, bool> predicate)
        {
            var handlers = GetHandlersFrom(holder, predicate);
            _handlers.AddRange(handlers);
        }

        private List<PacketHandler> GetHandlersFrom (object holder, Func<IEndPointConnection, bool> predicate)
        {
            var handlers = new List<PacketHandler>();

            foreach (var method in holder.GetType().GetMethods())
            {
                var attr = (PacketHandlerAttribute) method.GetCustomAttributes(typeof(PacketHandlerAttribute), true)
                    .FirstOrDefault();

                if (attr is null) continue;

                var parameters = method.GetParameters();
                var handledType = parameters.FirstOrDefault()?.ParameterType;
                if (handledType is null)
                    throw LogUtils.Throw(new Exception($"PacketHandler {method.Name} of " +
                                                       $"{holder.GetType()} has invalid signature."));

                var handler = new PacketHandler(method, holder, attr.MinimumRequiredState, handledType,
                    parameters.Length == 2, predicate);

                handlers.Add(handler);
            }

            return handlers;
        }

        public void RemovePacketHandlersFrom (object holder)
        {
            _handlers.RemoveAll(h => h.Target == holder);
        }
        
        public void RegisterPacketHandler <TPacket> (Action<TPacket> action, byte state = ConnectionState.All)
        {
            var handler = new PacketHandler(action.Method, action.Target, state, typeof(TPacket), false, conn => true);
            _handlers.Add(handler);
        }

        public void RemoveHandler <TPacket> (Action<TPacket> action)
        {
            _handlers.RemoveAll(h => h.Method == action.Method);
        }

        public void RegisterAssembly (Assembly assembly)
        {
            foreach (var packet in ScanAssemblyPackets(assembly))
            {
                var identifier = new MessageDispatcherStorage.PacketIdentifier(packet.Key.ProtocolId, packet.Key.PackedId);
                

                _storage.StorePacketType(identifier, packet.Value);
            }
        }

        public static Dictionary<PacketAttribute, Type> ScanAssemblyPackets (Assembly assembly)
        {
            var packets = new Dictionary<PacketAttribute, Type>();

            foreach (var type in assembly.GetTypes())
            {
                var attr = type.GetCustomAttributes<PacketAttribute>(false).FirstOrDefault();

                if (attr is null) continue;

                var samePacket = packets.Where(a =>
                    a.Key.PackedId == attr.PackedId && a.Key.ProtocolId == attr.ProtocolId).ToArray();

                if (samePacket.Length > 0)
                    throw LogUtils.Throw(
                        new Exception($"{samePacket.FormattedString(p => p.Value.FullName)} has the same ID as {type.FullName}"));

                packets.Add(attr, type);
            }

            return packets;
        }

        private class PacketHandler
        {
            public readonly bool AcceptsArgument;
            public readonly Type HandledType;
            public readonly MethodInfo Method;
            public readonly Func<IEndPointConnection, bool> Predicate;
            public readonly byte ConnectionState;
            public readonly object Target;

            public PacketHandler (MethodInfo method, object target, byte connectionState, Type handledType,
                bool acceptsArgument, Func<IEndPointConnection, bool> predicate)
            {
                Method = method;
                Target = target;
                ConnectionState = connectionState;
                HandledType = handledType;
                AcceptsArgument = acceptsArgument;
                Predicate = predicate;
            }
        }
    }
}