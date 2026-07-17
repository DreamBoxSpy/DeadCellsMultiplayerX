using System.Diagnostics;
using System.Text.Json;
using dc.en;
using DeadCellsMultiplayerX.Client.Guest.WorldX.Entities;
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
        private readonly ILogger logger;
        public PlayerGhost? RemoteHero { get; private set; }
        public GuestInfo? GuestInfo { get; private set; }
        public Hero? hero { get; set; }
        public string? RemoteSkinId { get; private set; }
        public string? RemoteHeadSkinId { get; private set; }

        public string? guid { get; set; }
        public bool isOwner { get; }
        public static async Task<TXGuestHeroManager> CreateAsync(GuestClientSession session, ILogger logger, Hero hero)
        {
            var manager = new TXGuestHeroManager(session, logger, hero);
            await manager.InitializeAsync();
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

            // Refresh full state from host (for any other changes)
            await session.Client.RefreshLobbyInfo();
            await session.Client.RefreshGameSessionInfo();

            // Always populate GuestInfo for all players
            GuestInfo = GetCurrentGuestInfo();

            var options = new JsonSerializerOptions { WriteIndented = true };
            logger.Information("\n GuestInfo: {Json}", JsonSerializer.Serialize(GuestInfo, options));
            logger.Information("\n Game Base info {F1},", JsonSerializer.Serialize(session.Client.gameSessionInfo, options));
        }

        public void FrameUpdate()
        {

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


        public void Clear()
        {
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
