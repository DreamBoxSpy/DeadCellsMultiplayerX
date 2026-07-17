
using System.Runtime.CompilerServices;
using MessagePack;

namespace DeadCellsMultiplayerX.Common.Data
{
    [MessagePackObject]
    public struct RectInt
    {
        public RectInt() { }
        public RectInt(int x, int y, int wid, int hei)
        {
            X = x;
            Y = y;
            Width = wid;
            Height = hei;
        }

        [Key(0)]
        public int X
        {
            get; set;
        }
        [Key(1)]
        public int Y
        {
            get; set;
        }
        [Key(2)]
        public int Width
        {
            get; set;
        }
        [Key(3)]
        public int Height
        {
            get; set;
        }
    }
}
