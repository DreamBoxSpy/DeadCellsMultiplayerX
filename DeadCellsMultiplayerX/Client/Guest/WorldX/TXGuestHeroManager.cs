using System.Diagnostics;
using System.Text.Json;
using dc.en;
using DeadCellsMultiplayerX.Client.Guest.WorldX.Entities;
using DeadCellsMultiplayerX.Client.Guest.WorldX.GuestHero;
using DeadCellsMultiplayerX.Client.Guest.WorldX.TXGuestBeheaded;
using DeadCellsMultiplayerX.Common.Data;
using Microsoft.VisualStudio.Threading;
using ModCore.Events;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Game.Hero;
using Serilog;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    internal class TXGuestHeroManager :
    IOnHeroUpdate,
    IEventReceiver,
    IOnFrameUpdate
    {
        private readonly GuestClientSession session;
        public readonly ILogger logger;
        public PlayerGhost? RemoteHero { get; private set; }
        public GuestInfo? GuestInfo { get; private set; }
        public PlyerGameSessionInfo? plyerGameInfo { get; private set; }
        public Hero? hero { get; set; }
        public CancellationTokenSource loopCts = default!;
        public string? RemoteSkinId { get; private set; }
        public string? RemoteHeadSkinId { get; private set; }

        public string? guid { get; set; }
        public bool isOwner { get; }
        public bool heroReady { get; private set; } = false;

        public HeroInfo Baseinfo = new();

        private readonly List<TransmitBeheaded> modules = new();
        public static async Task<TXGuestHeroManager> CreateAsync(GuestClientSession session, ILogger logger, Hero hero)
        {
            var manager = new TXGuestHeroManager(session, logger, hero);
            await manager.InitializeAsync();
            manager.loopCts = new CancellationTokenSource();
            manager.StartSyncLoop(manager.loopCts.Token).Forget();
            return manager;
        }

        // Minimal constructor for field init only
        private TXGuestHeroManager(GuestClientSession session, ILogger logger, Hero hero)
        {
            EventSystem.AddReceiver(this);
            this.session = session;
            this.logger = logger;
            this.hero = hero;
            guid = session.Client.Guid;

            if (session.Client.LobbyInfo?.Owner == session.Client.Guid)
                isOwner = true;
        }

        private async Task InitializeAsync()
        {
            Debug.Assert(session.Client.LobbyInfo != null);
            Debug.Assert(guid != null);

            // Tell host hero init is done
            await session.Client.HeroInitDone(true);
            heroReady = true;

            // Refresh full state from host (for any other changes)
            await session.Client.RefreshLobbyInfo();
            await session.Client.RefreshGameSessionInfo();

            // Always populate GuestInfo for all players
            GuestInfo = GetCurrentGuestInfo();
            plyerGameInfo = GetCurrnetGameSessionInfo();


            var options = new JsonSerializerOptions { WriteIndented = true };
            logger.Information("\n GuestInfo: {Json}", JsonSerializer.Serialize(GuestInfo, options));
            logger.Information("\n Game Base info {F1},", JsonSerializer.Serialize(plyerGameInfo, options));


            Register(new TxBhAnimation(session, this));
            Register(new TxBhMainHSprite(session, this));
            Register(new TxBhMovement(session, this));
        }


        /// <summary>
        /// 用于固定更新 Hero 与 game 所关联的基本信息
        /// </summary>
        /// <returns></returns>
        public async Task StartSyncLoop(CancellationToken ct)
        {
            SynchronizationContext.SetSynchronizationContext(
                ModCore.Modules.Game.SynchronizationContext);

            while (ct.IsCancellationRequested)
            {
                session.DisposeToken.ThrowIfCancellationRequested();

                if (hero == null || hero.destroyed || !hero.initDone)
                {
                    heroReady = false;
                    await session.Client.HeroInitDone(heroReady);
                }

                await Task.Delay(1000);


                await session.Client.RefreshGameSessionInfo();
            }
        }

        public HeroInfo ReplicatingHeroInfo()
        {
            Baseinfo.Ready = heroReady;
            foreach (var module in modules.Where(m => m.ShouldSync()))
            {
                module.Fill(Baseinfo);
            }

            return Baseinfo;
        }

        public T Register<T>(T module) where T : TransmitBeheaded
        {
            module.Initialize();
            modules.Add(module);
            return module;
        }

        public void Unregister(TransmitBeheaded module)
        {
            module.Dispose();
            modules.Remove(module);
        }

        public void FrameUpdate()
        {
            if (hero == null || hero.spr == null)
                return;


            foreach (var module in modules)
            {
                module.Tick();
            }

        }

        public void HeroUpdate(Hero hero)
        {

        }

        public GuestInfo? GetCurrentGuestInfo()
        {
            Debug.Assert(session.Client.LobbyInfo != null);
            Debug.Assert(guid != null);

            if (session.Client.LobbyInfo.Guests.TryGetValue(guid, out var info))
            {
                GuestInfo = info;
                return GuestInfo;
            }

            logger.Warning("Guest {guid} not found in LobbyInfo.Guests", guid);
            return null;
        }

        public PlyerGameSessionInfo? GetCurrnetGameSessionInfo()
        {
            Debug.Assert(session.Client.gameSessionInfo != null);
            Debug.Assert(guid != null);

            if (session.Client.gameSessionInfo.PlyerGameSession.TryGetValue(guid, out var info))
            {
                plyerGameInfo = info;
                return plyerGameInfo;
            }

            logger.Warning("Guest {guid} not found in GameSessionInfo.PlyerGameSession", guid);
            return null;
        }


        public void Clear()
        {
            loopCts.Cancel();
            loopCts.Dispose();
            loopCts = null!;
            RemoteHero?.destroy();
            hero = null;
            RemoteHero = null;
            RemoteSkinId = null;
            RemoteHeadSkinId = null;
            GuestInfo = null;
        }

        void IOnHeroUpdate.OnHeroUpdate(double dt)
        {
            if (hero == null) return;
            HeroUpdate(hero);
        }

        void IOnFrameUpdate.OnFrameUpdate(double dt) => FrameUpdate();
    }
}
