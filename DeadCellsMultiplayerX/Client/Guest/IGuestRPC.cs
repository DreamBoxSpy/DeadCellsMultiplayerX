using DeadCellsMultiplayerX.Common.Data;
using PolyType;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Guest
{
    [JsonRpcContract, GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
    internal partial interface IGuestRPC
    {
        /// <summary>
        /// 告诉客户端进入新level
        /// </summary>
        /// <param name="saveData"></param>
        /// <returns></returns>
        public Task EnterNewLevel(byte[] saveData);

        /// <summary>
        /// 通知客户端更新 Entity
        /// </summary>
        /// <param name="info"></param>
        public void UpdateEntity(EntityInfo info);


        public Task<HeroInfo> RequestHeroInfo();
    }
}
