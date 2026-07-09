using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Common.Data
{
    public class SpriteInfo
    {
        public string GUID { get; set; } = Guid.NewGuid().ToString();
        public string? Parent { get; set; }
        public long TimeStamp { get; set; } = 0;
        public string AtlasName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public SimpleObjData PivotData { get; set; } = new();
        public List<SpriteInfo> Children { get; set; } = [];
    }
}
