using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc.h2d;
using dc.ui;
using dc.uicore;

namespace DeadCellsMultiplayerX.Client.UI
{

    public class ReleaseNotes
    {
        public string Version { get; }          // 版本号
        public DateTimeOffset ReleaseTime { get; }  // 发布时间
        public List<ChangeEntry> Changes { get; } = [];  // 更新条目

        public ReleaseNotes(string version, DateTimeOffset releaseTime, string? title = null)
        {
            Version = version;
            ReleaseTime = releaseTime;
        }
    }
    public class ChangeEntry
    {
        public ChangeType Type { get; }
        public string Description { get; }

        public ChangeEntry(ChangeType type, string description)
        {
            Type = type;
            Description = description;
        }
    }

    public enum ChangeType
    {
        Feature,     // 新功能
        Improvement, // 改进
        BugFix,      // 修复
        Breaking,    // 大变更
        Other
    }

    public enum PageKind
    {
        Lobby,

        Host,

        Client,

        GameMian,

        OnlineMian,

        SteamP2P,

        Settings
    }

    public enum ControllerKey
    {
        Default_UP,
        Default_DOWN,
        Default_Left,
        Default_Rigth,
        Tab_Exchange_Q,
        Tab_Exchange_E,
        Confirm,
        Cancel
    }


    public record class BtnData
    {
        public Flow? Flow { get; set; }
        public dc.ui.Text? text { get; set; }
        public Interactive? interactive { get; set; }
        public Action? OnReszie { get; set; }
        public bool LoadAuido { get; set; } = false;
    }
}