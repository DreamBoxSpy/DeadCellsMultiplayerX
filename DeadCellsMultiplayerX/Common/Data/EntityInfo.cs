using dc;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace DeadCellsMultiplayerX.Common.Data
{
    public class EntityInfo
    {
        public string? TypeName { get; set; } = "";
        public string GUID { get; set; } = Guid.NewGuid().ToString();
        public long TimeStamp { get; set; } = 0;
        public string? ColorMapModel { get; set; }
        public string? ColorMapSkin { get; set; }
        public int SubLevelId { get; set; }
        public SimpleObjData EntityData { get; set; } = new();
        public Dictionary<int, SimpleObjData> GlowData { get; set; } = [];
        public SpriteInfo? MainSprite { get; set; }
        public AnimInfo animInfo { get; set; } = new();
    }
}
