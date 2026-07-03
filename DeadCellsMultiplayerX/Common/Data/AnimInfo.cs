using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc.hl.types;

namespace DeadCellsMultiplayerX.Common.Data
{
    public class AnimInfo
    {
        public int cursor { get; set; }    // 当前帧序号
        public double speed { get; set; }      // 播放速度
        public bool paused { get; set; }      // 是否暂停
        public string GroupName { get; set; } = ""; //动画名
        public int Frame { get; set; } //帧索引
        public int plays { get; set; }
    }
}