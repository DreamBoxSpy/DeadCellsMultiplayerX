using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc.en;

namespace DeadCellsMultiplayerX.Client
{
    public class GameSessionInfo
    {
        public Dictionary<string, PlyerGameSessionInfo> PlyerGameSession { get; set; } = [];

        public string HostLevelName { get; set; } = string.Empty;
    }

    public class PlyerGameSessionInfo
    {
        public bool HeroInitDone { get; set; }
    }
}