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
        public GuestHeroManager(GuestClientSession session, ILogger logger, Hero hero)
        {
            EventSystem.AddReceiver(this);

            this.session = session;
            this.logger = logger;


            if (session.Client.LobbyInfo?.Owner == session.Client.Guid)
                isOwner = true;


            this.hero = hero;
            session.Client.HeroInitDone(true);

            guid = session.Client.Guid;

            Debug.Assert(session.Client.LobbyInfo != null);


            object Info;
            if (isOwner)
                Info = session.Client.LobbyInfo;
            else
                Info = GetCurrentGuestInfo();


            var options = new JsonSerializerOptions { WriteIndented = true };
            logger.Information("GuestInfo: {Json}", JsonSerializer.Serialize(Info, options));


            logger.Information("Game Base info {F1},", JsonSerializer.Serialize(session.Client.gameSessionInfo, options));
        }

        public void update()
        {

        }

        public GuestInfo GetCurrentGuestInfo()
        {
            Debug.Assert(session.Client.LobbyInfo != null);
            Debug.Assert(guid != null);

            GuestInfo = session.Client.LobbyInfo.Guests[guid];

            return GuestInfo;
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
