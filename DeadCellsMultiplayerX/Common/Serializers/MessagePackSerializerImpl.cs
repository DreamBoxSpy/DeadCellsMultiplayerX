using System.Buffers;
using MessagePack;
using MessagePack.Resolvers;

namespace DeadCellsMultiplayerX.Common.Serializers
{
    public sealed class MessagePackSerializerImpl : IBinarySerializer
    {
        public static readonly MessagePackSerializerImpl Instance = new();
        public SerializerKind Kind => SerializerKind.MessagePack;

        private static readonly MessagePackSerializerOptions Options =
            MessagePackSerializerOptions.Standard
                .WithResolver(
                    CompositeResolver.Create(
                        [],
                        [
                            HaxeFallbackResolver.Instance,
                            ContractlessStandardResolver.Instance,
                        ]
                    )
                );

        private MessagePackSerializerImpl() { }
        public byte[] Serialize<T>(T? obj)
        {
            return MessagePackSerializer.Serialize(obj, Options);
        }

        public byte[] Serialize(object obj, Type type)
        {
            return MessagePackSerializer.Serialize(type, obj, Options);
        }

        public T? Deserialize<T>(byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data, Options);
        }

        public object? Deserialize(byte[] data, Type type)
        {
            return MessagePackSerializer.Deserialize(type, data, Options);
        }

        public T? Deserialize<T>(ReadOnlySpan<byte> data)
        {
            return MessagePackSerializer.Deserialize<T>(data.ToArray(), Options);
        }

        public object? Deserialize(ReadOnlySpan<byte> data, Type type)
        {
            return MessagePackSerializer.Deserialize(type, data.ToArray(), Options);
        }

        public void Serialize<T>(IBufferWriter<byte> writer, T? obj)
        {
            MessagePackSerializer.Serialize(writer, obj, Options);
        }

        public void Serialize(IBufferWriter<byte> writer, object obj, Type type)
        {
            MessagePackSerializer.Serialize(type, writer, obj, Options);
        }
    }
}