using System.Buffers;
using HashlinkNET.Native.Impl;
using HaxeProxy.Runtime;
using MessagePack;
using MessagePack.Formatters;

namespace DeadCellsMultiplayerX.Common.Serializers
{
    /// <summary>
    /// 使用 Haxe 原生 <see cref="dc.haxe.Serializer"/> / <see cref="dc.haxe.Unserializer"/>
    /// 对对象进行序列化，将结果以 MessagePack Binary 格式存储。
    /// <summary>
    public class HaxeFallbackFormatter<T> : IMessagePackFormatter<T>
    {
        /// <inheritdoc />
        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var haxeSerializer = new dc.haxe.Serializer();
            haxeSerializer.serialize(value);
            dc.String dcStr = haxeSerializer.buf.toString();

            if (dcStr == null)
            {
                writer.WriteNil();
                return;
            }

            int length = dcStr.length;
            int byteCount = checked(length * 2);

            unsafe
            {
                var span = new ReadOnlySpan<byte>((void*)dcStr.bytes, byteCount);
                writer.WriteBinHeader(byteCount);
                writer.WriteRaw(span);
            }
        }

        /// <inheritdoc />
        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return default!;

            ReadOnlySequence<byte>? seqNullable = reader.ReadBytes();

            if (!seqNullable.HasValue)
                throw new MessagePackSerializationException("Expected binary data for Haxe deserialization.");

            ReadOnlySequence<byte> seq = seqNullable.Value;
            int byteCount = checked((int)seq.Length);

            if ((byteCount % 2) != 0)
                throw new MessagePackSerializationException("Invalid UCS-2 byte count: must be even.");

            int length = byteCount / 2;
            IntPtr ptr = Lib_std.alloc_bytes.Invoke(byteCount + 2);

            if (seq.IsSingleSegment)
            {
                unsafe
                {
                    seq.First.Span.CopyTo(new Span<byte>((void*)ptr, byteCount));
                    *(short*)(ptr + byteCount) = 0;
                }
            }
            else
            {
                byte[] rented = ArrayPool<byte>.Shared.Rent(byteCount);
                try
                {
                    seq.CopyTo(rented);
                    unsafe
                    {
                        fixed (byte* src = rented)
                        {
                            Buffer.MemoryCopy(src, (void*)ptr, byteCount, byteCount);
                        }
                        *(short*)(ptr + byteCount) = 0;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }

            dc.String dcStr = dc.String.Class.__alloc__(ptr, length);
            var haxeUnser = new dc.haxe.Unserializer(dcStr);
            return (T)haxeUnser.unserialize();
        }
    }

    /// <summary>
    /// <see cref="IFormatterResolver"/>，将 Haxe 对象路由到 <see cref="HaxeFallbackFormatter{T}"/>，
    /// 非 Haxe 类型返回 <c>null</c> 交由标准 MessagePack 解析器链处理。
    /// </summary>
    public class HaxeFallbackResolver : IFormatterResolver
    {
        /// <summary>
        /// <see cref="HaxeFallbackResolver"/> 的单例。
        /// </summary>
        public static readonly HaxeFallbackResolver Instance = new();

        private HaxeFallbackResolver() { }

        /// <inheritdoc />
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (typeof(HaxeObject).IsAssignableFrom(typeof(T)))
            {
                return FormatterCache<T>.Formatter;
            }
            return null!;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter = new HaxeFallbackFormatter<T>();
        }
    }
}
