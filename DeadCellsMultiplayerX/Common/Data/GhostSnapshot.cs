using System;
using DeadCellsMultiplayerX.Common.Serializers;
using Mirror;

namespace DeadCellsMultiplayerX.Common.Data
{
    public class GhostSnapshot : Snapshot
    {
        public EntityInfo State = null!;

        public double remoteTime { get; set; }
        public double localTime { get; set; }
    }
}