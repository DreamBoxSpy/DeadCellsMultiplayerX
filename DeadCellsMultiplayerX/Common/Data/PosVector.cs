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

        private const ulong CxMask = (1UL << 31) - 1;          // bits 0–30
        private const ulong DirMask = 1UL << 31;               // bit  31
        private const ulong CyMask = ((1UL << 31) - 1) << 32;  // bits 32–62
        private const int CyShift = 32;
        private const ulong OnGroundMask = 1UL << 63;  // bit 63


        [IgnoreMember]
        public int CX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(PackedA & CxMask);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedA = (PackedA & ~CxMask) | ((ulong)value & CxMask);
        }

        [IgnoreMember]
        public int CY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((PackedA & CyMask) >> CyShift);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedA = (PackedA & ~CyMask) | (((ulong)value << CyShift) & CyMask);
        }

        [IgnoreMember]
        public double XR
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BitConverter.Int64BitsToDouble((long)PackedB);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedB = (ulong)BitConverter.DoubleToInt64Bits(value);
        }

        [IgnoreMember]
        public double XY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BitConverter.Int64BitsToDouble((long)PackedC);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedC = (ulong)BitConverter.DoubleToInt64Bits(value);
        }

        [IgnoreMember]
        public int DIR
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
            CX = cx;
            CY = cy;
            XR = xr;
            XY = xy;
            DIR = dir;
        }
    }
}