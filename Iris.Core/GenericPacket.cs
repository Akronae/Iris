using System;
using Proteus.Core;

namespace Iris.Core
{
    public class GenericPacket : Packet
    {
        public Type GenericType;

        [SerializedMember(0)]
        public int GenericTypeId = GenericTypesConsts.UndefinedType;

        public GenericPacket ()
        {
        }

        public GenericPacket (Type genericType) : this()
        {
            GenericType = genericType;
        }
    }
}