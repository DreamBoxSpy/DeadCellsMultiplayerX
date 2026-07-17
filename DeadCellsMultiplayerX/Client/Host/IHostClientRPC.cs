using PolyType;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Host
{
    [JsonRpcContract, GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
    internal partial interface IHostClientRPC
    {
        public Task<bool> CheckVersion(string version);
        /// <summary>
        /// 获取房间信息
        /// </summary>
        /// <returns></returns>
        public Task<LobbyInfo> GetLobbyInfo();

        /// <summary>
        /// 获取当前玩家的 GUID
        /// </summary>
        /// <returns></returns>
        public Task<string> GetGUID();

        /// <summary>
        /// 设置访客名称
        /// </summary>
        /// <param name="name"></param>
        public void SetName(string name);

        /// <summary>
        /// 退出房间
        /// </summary>
        public void Quit();

        /// <summary>
        /// 设置是否准备好开始游戏
        /// </summary>
        public void SetReady(bool ready);

        /// <summary>
        /// 设置玩家皮肤
        /// </summary>
        public Task SetSkinMould(string skinMould);

        /// <summary>
        /// 延迟
        /// </summary>
        public Task Ping();

        /// <summary>
        /// 玩家英雄以初始化
        /// </summary>
        /// <param name="InitDone"></param>
        public void HeroInitDone(bool InitDone);

        public Task<Stream> GetServerStream();
    }
}
