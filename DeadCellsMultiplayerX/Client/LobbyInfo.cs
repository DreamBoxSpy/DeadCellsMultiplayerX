using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Client
{
    /// <summary>
    /// 房间的信息
    /// </summary>
    public class LobbyInfo
    {
        /// <summary>
        /// 房间里的玩家
        /// </summary>
        public Dictionary<string, GuestInfo> Guests { get; set; } = [];

        /// <summary>
        /// 房间是否已开始游戏
        /// </summary>
        public bool IsStarted { get; set; } = false;

        /// <summary>
        /// 房间已开始游戏且Guest可以连接服务器
        /// </summary>
        public bool CanConnectServer { get; set; } = false;

        /// <summary>
        /// 房间 Host
        /// </summary>
        public string Owner { get; set; } = "";
    }

    /// <summary>
    /// 房间中每位玩家的信息
    /// </summary>
    public class GuestInfo
    {
        /// <summary>
        /// 玩家名称
        /// </summary>
        public string Name { get; set; } = "Guest";

        /// <summary>
        /// 玩家 Id
        /// </summary>
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();

        /// <summary>
        /// 是否准备好游戏
        /// </summary>
        public bool IsReady { get; set; } = false;

        /// <summary>
        /// 是否是房主
        /// </summary>
        public bool IsHost { get; set; } = false;

        /// <summary>
        /// 是否已连接服务器
        /// </summary>
        public bool IsConnectedServer { get; set; } = false;

        /// <summary>
        /// 玩家皮肤
        /// </summary>
        public string SkinMould { get; set; } = string.Empty;

        public bool HeroInitDone { get; set; } = false;

        public GuestInfo Clone() => (GuestInfo) MemberwiseClone();
    }
}
