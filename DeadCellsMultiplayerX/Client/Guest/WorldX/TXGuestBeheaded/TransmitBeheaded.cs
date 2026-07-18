using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using dc.en;
using DeadCellsMultiplayerX.Common.Data;
using Serilog;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.TXGuestBeheaded
{
    internal abstract class TransmitBeheaded : IDisposable
    {
        protected TXGuestHeroManager Manager { get; }

        protected Hero Hero => Manager.hero!;
        protected GuestClientSession? session { get; }
        protected ILogger Logger => Manager.logger;

        protected TransmitBeheaded(GuestClientSession Session, TXGuestHeroManager manager)
        {
            Debug.Assert(session != null);
            session = Session;
            Manager = manager;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// 每帧更新
        /// </summary>
        public abstract void Tick();

        /// <summary>
        /// 是否需要同步
        /// </summary>
        public abstract bool ShouldSync();

        /// <summary>
        /// 填充数据
        /// </summary>
        public abstract HeroInfo Fill(HeroInfo info);

        /// <summary>
        /// 释放资源
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// 重置
        /// </summary>
        public abstract void Reset();
    }
}