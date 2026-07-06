using System;
using System.Buffers;
using MemoryPack;

namespace DeadCellsMultiplayerX.Common.Serializers
{
    public sealed class MemoryPackSerializerImpl : IBinarySerializer
    {
        public static readonly MemoryPackSerializerImpl Instance = new();
        public SerializerKind Kind => SerializerKind.MemoryPack;

        private MemoryPackSerializerImpl() { }
        public byte[] Serialize<T>(T? obj)
        {
            return MemoryPackSerializer.Serialize(obj);
        }

        public T? Deserialize<T>(byte[] data)
        {
            return MemoryPackSerializer.Deserialize<T>(data);
        }

        public byte[] Serialize(object obj, Type type)
        {
            return MemoryPackSerializer.Serialize(type, obj);
        }

        public object? Deserialize(byte[] data, Type type)
        {
            return MemoryPackSerializer.Deserialize(type, data);
        }

        public T? Deserialize<T>(ReadOnlySpan<byte> data)
        {
            return MemoryPackSerializer.Deserialize<T>(data);
        }

        public object? Deserialize(ReadOnlySpan<byte> data, Type type)
        {
            return MemoryPackSerializer.Deserialize(type, data);
        }

        public void Serialize<T>(IBufferWriter<byte> writer, T? obj)
        {
            MemoryPackSerializer.Serialize(writer, obj);
        }

        public void Serialize(IBufferWriter<byte> writer, object obj, Type type)
        {
            MemoryPackSerializer.Serialize(type, writer, obj);
        }
    }
}