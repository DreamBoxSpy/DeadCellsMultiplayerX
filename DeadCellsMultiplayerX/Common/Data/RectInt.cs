
using System.Runtime.CompilerServices;
using MessagePack;

namespace DeadCellsMultiplayerX.Common.Data
{
    [MessagePackObject]
    public struct RectInt
    {
        [Key(0)] public ulong PackedA;
        const ulong X_MASK = (1UL << 24) - 1;
        const ulong Y_MASK = ((1UL << 24) - 1) << 24;
        const ulong W_MASK = ((1UL << 8) - 1) << 48;
        const ulong H_MASK = ((1UL << 8) - 1) << 56;

        public RectInt() { }
        public RectInt(int x, int y, int wid, int hei)
        {
            X = x;
            Y = y;
            Width = wid;
            Height = hei;
        }

        [IgnoreMember]
        public int X
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((PackedA & X_MASK) >> 0);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedA = (PackedA & ~X_MASK) | ((ulong)value & X_MASK);
        }
        [IgnoreMember]
        public int Y
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((PackedA & Y_MASK) >> 24);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedA = (PackedA & ~Y_MASK) | ((ulong)value << 24) & Y_MASK;
        }
        [IgnoreMember]
        public int Width
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((PackedA & W_MASK) >> 48);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedA = (PackedA & ~W_MASK) | ((ulong)value << 48) & W_MASK;
        }
        [IgnoreMember]
        public int Height
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((PackedA & H_MASK) >> 56);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedA = (PackedA & ~H_MASK) | ((ulong)value << 56) & H_MASK;
        }
    }
}
