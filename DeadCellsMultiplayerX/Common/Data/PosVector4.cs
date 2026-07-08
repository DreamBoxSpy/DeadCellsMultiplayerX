using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc;
using MessagePack;

namespace DeadCellsMultiplayerX.Common.Data
{
    [MessagePack.MessagePackObject]
    public class PosVector4
    {
        public PosVector4() { }

        public PosVector4(int x, int y, double z, double w)
        {
            X = x; Y = y; Z = z; W = w;
        }

        [Key(0)] public int X;
        [Key(1)] public int Y;
        [Key(2)] public double Z;
        [Key(3)] public double W;

    }
}