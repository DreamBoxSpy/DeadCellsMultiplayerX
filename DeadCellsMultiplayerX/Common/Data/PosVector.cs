using System.Runtime.CompilerServices;
using MessagePack;

namespace DeadCellsMultiplayerX.Common.Data
{
    /// <summary>
    /// Packed entity position vector for high-frequency network sync.
    ///
    /// Original five MessagePack fields (CX, CY, XR, XY, DIR = 224 bits on wire)
    /// are reduced to three ulong fields (192 bits on wire).  DIR is stored as
    /// a single bit in PackedA, reducing CY precision from 32 to 31 bits.
    /// Dead Cells tile coordinates are well under 2^31, so zero observable
    /// precision is lost.
    ///
    /// Packing layout (PackedA):
    ///   Bits  0–30 : CX (31-bit unsigned tile X)
    ///   Bit     31 : DIR (0 = right/1,  1 = left/-1)
    ///   Bits 32–62 : CY (31-bit unsigned tile Y)
    ///   Bit     63 : reserved (0)
    ///
    /// Packing layout (PackedB):
    ///   Bits 0–63 : XR raw IEEE-754 double bits
    ///
    /// Packing layout (PackedC):
    ///   Bits 0–63 : XY raw IEEE-754 double bits
    /// </summary>
    [MessagePackObject]
    public class PosVector
    {
        [Key(0)] public ulong PackedA;
        [Key(1)] public ulong PackedB;
        [Key(2)] public ulong PackedC;

        [IgnoreMember]
        private const ulong CxMask = 0x7FFFFFFFUL;          // bits 0–30
        [IgnoreMember]
        private const ulong DirMask = 0x80000000UL;          // bit  31
        [IgnoreMember]
        private const ulong CyMask = 0x7FFFFFFF00000000UL;   // bits 32–62
        [IgnoreMember]
        private const int CyShift = 32;
        [IgnoreMember]
        private const ulong OnGroundMask = 1UL << 63;  // bit 63


        [IgnoreMember]
        public int CellX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(PackedA & CxMask);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedA = (PackedA & ~CxMask) | ((ulong)value & CxMask);
        }

        [IgnoreMember]
        public int CellY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((PackedA & CyMask) >> CyShift);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedA = (PackedA & ~CyMask) | (((ulong)value << CyShift) & CyMask);
        }

        [IgnoreMember]
        public double OffsetX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BitConverter.Int64BitsToDouble((long)PackedB);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedB = (ulong)BitConverter.DoubleToInt64Bits(value);
        }

        [IgnoreMember]
        public double OffsetY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BitConverter.Int64BitsToDouble((long)PackedC);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedC = (ulong)BitConverter.DoubleToInt64Bits(value);
        }

        [IgnoreMember]
        public int Direction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ((PackedA & DirMask) != 0) ? -1 : 1;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedA = value < 0
                ? (PackedA | DirMask)
                : (PackedA & ~DirMask);
        }


        public PosVector() { }

        public PosVector(int cx, int cy, double xr, double xy, int dir)
        {
            CellX = cx;
            CellY = cy;
            OffsetX = xr;
            OffsetY = xy;
            Direction = dir;
        }


        [IgnoreMember]
        public int CX { get => CellX; set => CellX = value; }

        [IgnoreMember]
        public int CY { get => CellY; set => CellY = value; }

        [IgnoreMember]
        public double XR { get => OffsetX; set => OffsetX = value; }

        [IgnoreMember]
        public double XY { get => OffsetY; set => OffsetY = value; }

        [IgnoreMember]
        public int DIR { get => Direction; set => Direction = value; }
    }
}
