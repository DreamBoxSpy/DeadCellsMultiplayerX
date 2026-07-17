using System.Diagnostics;
using System.Text.Json;
using dc.en;
using DeadCellsMultiplayerX.Client.Guest.WorldX.Entities;
using ModCore.Events;
using ModCore.Events.Interfaces.Game.Hero;
using Serilog;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    internal class GuestHeroManager :
    IOnHeroUpdate,
    IEventReceiver
    {
        private readonly GuestClientSession session;
        private readonly ILogger logger;
        public PlayerGhost? RemoteHero { get; private set; }
        public GuestInfo? GuestInfo { get; private set; }
        public Hero? hero { get; }
        public string? RemoteSkinId { get; private set; }
        public string? RemoteHeadSkinId { get; private set; }

        public string? guid { get; set; }
        public bool isOwner { get; }
        public static async Task<GuestHeroManager> CreateAsync(GuestClientSession session, ILogger logger, Hero hero)
        {
            var manager = new GuestHeroManager(session, logger, hero);
            await manager.InitializeAsync();
            return manager;
        }

        // Minimal constructor for field init only
        private GuestHeroManager(GuestClientSession session, ILogger logger, Hero hero)
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
            session.Client.HeroInitDone(true);

            // Optimistic local update: we know we just sent HeroInitDone, so update local state immediately
            if (session.Client.LobbyInfo.Guests.ContainsKey(guid))
            {
                session.Client.LobbyInfo.Guests[guid].HeroInitDone = true;
            }

            // Refresh full state from host (for any other changes)
            await session.Client.RefreshLobbyInfo();
            await session.Client.RefreshGameSessionInfo();

            // Always populate GuestInfo for all players
            GuestInfo = GetCurrentGuestInfo();

            var options = new JsonSerializerOptions { WriteIndented = true };
            logger.Information("GuestInfo: {Json}", JsonSerializer.Serialize(GuestInfo, options));
            logger.Information("Game Base info {F1},", JsonSerializer.Serialize(session.Client.gameSessionInfo, options));
        }

        public void update()
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
            RemoteHero = null;
            RemoteSkinId = null;
            RemoteHeadSkinId = null;
        }

        void IOnHeroUpdate.OnHeroUpdate(double dt)
        {
            // var options = new JsonSerializerOptions { WriteIndented = true };
            // logger.Information("Game Base info {F1},", JsonSerializer.Serialize(session.Client.gameSessionInfo, options));
        }

    }
}
