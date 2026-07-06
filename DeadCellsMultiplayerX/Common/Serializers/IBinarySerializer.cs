using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeadCellsMultiplayerX.Common.Serializers
{
    public interface IBinarySerializer
    {
        SerializerKind Kind { get; }

        byte[] Serialize<T>(T? obj);

        T? Deserialize<T>(ReadOnlySpan<byte> data);

        

        object? Deserialize(ReadOnlySpan<byte> data, Type type);

        byte[] Serialize(object obj, Type type);



        void Serialize<T>(IBufferWriter<byte> writer, T? obj);

        void Serialize(
            IBufferWriter<byte> writer,
            object obj,
            Type type);

        T? Deserialize<T>(byte[] data)
            => Deserialize<T>((ReadOnlySpan<byte>)data);

        object? Deserialize(byte[] data, Type type)
            => Deserialize((ReadOnlySpan<byte>)data, type);
    }

    public static class Serializers
    {
        public static IBinarySerializer MemoryPack { get; }
            = MemoryPackSerializerImpl.Instance;

        public static IBinarySerializer MessagePack { get; }
            = MessagePackSerializerImpl.Instance;
    }

    public enum SerializerKind : byte
    {
        MemoryPack = 1,
        MessagePack = 2
    }
}
