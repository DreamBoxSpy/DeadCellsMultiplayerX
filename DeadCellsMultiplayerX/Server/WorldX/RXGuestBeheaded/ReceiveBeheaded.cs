using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeadCellsMultiplayerX.Server.WorldX.RXGuestBeheaded
{
    public abstract class ReceiveBeheaded: IDisposable
    {
        /// <summary>
        /// 初始化
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// 接收玩家数据
        /// </summary>
        public abstract void Receive();

        /// <summary>
        /// 每帧更新
        /// </summary>
        public abstract void Tick();

        /// <summary>
        /// 应用目标
        /// </summary>
        public abstract void Apply();

        /// <summary>
        /// 释放资源
        /// </summary>
        public abstract void Dispose();
    }
}