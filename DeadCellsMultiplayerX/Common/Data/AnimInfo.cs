using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using MessagePack;

namespace DeadCellsMultiplayerX.Common.Data
{
    /// <summary>
    /// Packed animation state for high-frequency network sync.
    ///
    /// Original animation fields (Speed double, Frame int, Paused bool, Plays int)
    /// are reduced to two ulong fields (128 bits on wire).  Plays precision is reduced
    /// from 32 to 31 bits, still well within practical limits.
    ///
    /// Packing layout (PackedA):
    ///   Bits 0–63 : raw IEEE-754 double bits of <see cref="AnimSpeed"/>.
    ///
    /// Packing layout (PackedB):
    ///   Bits  0–30 : AnimFrame (31-bit frame index)
    ///   Bit     31 : AnimPaused (1 = paused, 0 = playing)
    ///   Bits 32–62 : AnimPlays (31-bit play count)
    ///   Bit     63 : reserved (0)
    /// </summary>
    [MessagePackObject]
    public class AnimInfo
    {
        [Key(0)] public List<AnimTransitions> AnimTransitions { get; set; } = new();
        [Key(1)] public ulong PackedA;
        [Key(2)] public ulong PackedB;
        [Key(3)] public ulong PackedC;
        [Key(4)] public ulong PackedD;

        public const ulong FrameMask = (1UL << 31) - 1;
        public const ulong PausedMask = 1UL << 31;
        private const ulong PlaysMask = ((1UL << 31) - 1) << 32;
        private const ulong CursorMask = (1UL << 32) - 1;

        [IgnoreMember]
        public double Speed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BitConverter.Int64BitsToDouble((long)PackedA);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedA = (ulong)BitConverter.DoubleToInt64Bits(value);
        }

        [IgnoreMember]
        public int Frame
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(PackedB & FrameMask);
            set => PackedB = (PackedB & ~FrameMask) | ((ulong)value & FrameMask);
        }

        [IgnoreDataMember]
        public bool Paused
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (PackedB & PausedMask) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedB = value
                ? (PackedB | PausedMask)
                : (PackedB & ~PausedMask);
        }

        [IgnoreDataMember]
        public int Plays
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((PackedB & PlaysMask) >> 32);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedB = (PackedB & ~PlaysMask) | (((ulong)value << 32) & PlaysMask);
        }

        [IgnoreDataMember]
        public double playDuration
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BitConverter.Int64BitsToDouble((long)PackedC);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedC = (ulong)BitConverter.DoubleToInt64Bits(value);
        }

        [IgnoreDataMember]
        public int animCursor
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(PackedD & CursorMask);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedD = (PackedD & ~CursorMask) | ((uint)value & CursorMask);
        }
        public AnimInfo() { }
    }
}