using System;
using System.IO;
using System.Text;
using DeadCellsMultiplayerX.Common.Serializers;

namespace DeadCellsMultiplayerX.Common.Network
{
    /// <summary>
    /// 网络数据包写入器，用于构建要发送的二进制数据包。
    /// </summary>
    public sealed class PacketWriter : IDisposable
    {
        private readonly MemoryStream stream;
        private readonly BinaryWriter writer;
        private bool disposed;

        /// <summary>
        /// 获取当前已写入的字节数。
        /// </summary>
        public int Length => (int)stream.Length;

        public PacketWriter()
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        }



        /// <inheritdoc cref="BinaryWriter.Write(byte)"/>
        public void Write(byte value) => writer.Write(value);

        /// <inheritdoc cref="BinaryWriter.Write(bool)"/>
        public void Write(bool value) => writer.Write(value);

        /// <inheritdoc cref="BinaryWriter.Write(short)"/>
        public void Write(short value) => writer.Write(value);

        /// <inheritdoc cref="BinaryWriter.Write(ushort)"/>
        public void Write(ushort value) => writer.Write(value);

        /// <inheritdoc cref="BinaryWriter.Write(int)"/>
        public void Write(int value) => writer.Write(value);

        /// <inheritdoc cref="BinaryWriter.Write(uint)"/>
        public void Write(uint value) => writer.Write(value);

        /// <inheritdoc cref="BinaryWriter.Write(long)"/>
        public void Write(long value) => writer.Write(value);

        /// <inheritdoc cref="BinaryWriter.Write(ulong)"/>
        public void Write(ulong value) => writer.Write(value);

        /// <inheritdoc cref="BinaryWriter.Write(float)"/>
        public void Write(float value) => writer.Write(value);

        /// <inheritdoc cref="BinaryWriter.Write(double)"/>
        public void Write(double value) => writer.Write(value);

        /// <summary>
        /// 写入一个长度前缀的字符串（UTF-8 编码）。
        /// </summary>
        public void Write(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            writer.Write(value);
        }


        #region 字节数组

        /// <summary>
        /// 写入字节数组，先写入长度前缀（int），再写入数据。
        /// </summary>
        /// <param name="data">要写入的字节数组</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> 为 <c>null</c>。</exception>
        /// <remarks>
        /// 写入格式：<c>[int Length][byte[] Data]</c>。
        /// 对应读取时应先读取 int 获取长度，再读取等长字节。
        /// </remarks>
        public void Write(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            writer.Write(data.Length);
            writer.Write(data);
        }

        /// <summary>
        /// 直接写入字节数组数据，不写入长度前缀。
        /// </summary>
        /// <param name="data">要写入的字节数组。</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> 为 <c>null</c>。</exception>
        /// <remarks>
        /// 用于写入固定长度数据或外部已管理长度的场景。
        /// 读取方必须有其他方式确定数据边界。
        /// </remarks>
        public void WriteRaw(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            writer.Write(data);
        }

        #endregion

        #region 序列化对象

        /// <summary>
        /// 使用指定的序列化器将对象序列化并写入数据包。
        /// </summary>
        /// <typeparam name="T">要序列化的对象类型。</typeparam>
        /// <param name="serializer">要使用的二进制序列化器。</param>
        /// <param name="value">要序列化并写入的对象。</param>
        /// <exception cref="ArgumentNullException"><paramref name="serializer"/> 为 <c>null</c>。</exception>
        /// <remarks>
        /// 内部先将对象序列化为 <c>byte[]</c>，再调用 <see cref="Write(byte[])"/> 写入长度前缀和数据。
        /// </remarks>
        public void WriteObject<T>(IBinarySerializer serializer, T value)
        {
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));
            byte[] payload = serializer.Serialize(value);
            Write(payload);
        }

        #endregion

        #region 导出与控制

        /// <summary>
        /// 返回包含当前数据包全部内容的字节数组。
        /// </summary>
        /// <returns>数据包的完整字节表示。</returns>
        /// <remarks>多次调用返回相同内容的独立副本。</remarks>
        public byte[] ToArray() => stream.ToArray();

        /// <summary>
        /// 清空缓冲区，允许重复使用当前实例构建新数据包。
        /// </summary>
        /// <remarks>重置后 <see cref="Length"/> 变为 0，之前写入的数据被丢弃。</remarks>
        public void Reset()
        {
            stream.SetLength(0);
            stream.Position = 0;
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 释放 <see cref="PacketWriter"/> 使用的所有资源。
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                writer.Dispose();
                stream.Dispose();
                disposed = true;
            }
        }

        #endregion
    }
}
